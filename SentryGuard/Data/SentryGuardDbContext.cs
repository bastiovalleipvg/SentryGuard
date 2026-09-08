using Microsoft.EntityFrameworkCore;
using SentryGuard.Models;

namespace SentryGuard.Data;

public class SentryGuardDbContext : DbContext
{
    public SentryGuardDbContext(DbContextOptions<SentryGuardDbContext> options)
        : base(options)
    {
    }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<PlanServicio> PlanesServicio => Set<PlanServicio>();
    public DbSet<CategoriaActivo> CategoriasActivo => Set<CategoriaActivo>();
    public DbSet<Severidad> Severidades => Set<Severidad>();
    public DbSet<EstadoIncidente> EstadosIncidente => Set<EstadoIncidente>();
    public DbSet<Organizacion> Organizaciones => Set<Organizacion>();
    public DbSet<ContratoSLA> ContratosSla => Set<ContratoSLA>();
    public DbSet<ContratoSLADetalle> ContratosSlaDetalle => Set<ContratoSLADetalle>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Activo> Activos => Set<Activo>();
    public DbSet<Incidente> Incidentes => Set<Incidente>();
    public DbSet<BitacoraIncidente> BitacorasIncidente => Set<BitacoraIncidente>();
    public DbSet<EvidenciaIncidente> EvidenciasIncidente => Set<EvidenciaIncidente>();
    public DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<PlanServicio>(entity =>
        {
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.Property(e => e.TiempoRespuestaBaseH).HasPrecision(10, 2);
        });

        modelBuilder.Entity<CategoriaActivo>(entity =>
        {
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<Severidad>(entity =>
        {
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.Property(e => e.HorasSla).HasPrecision(10, 2);
        });

        modelBuilder.Entity<EstadoIncidente>(entity =>
        {
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<Organizacion>(entity =>
        {
            entity.HasIndex(e => e.Rut).IsUnique();
            entity.Property(e => e.FechaAlta)
                .HasPrecision(0)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Organizacion.Plan -> PlanServicio (sin colección de vuelta)
            entity.HasOne(e => e.Plan)
                .WithMany()
                .HasForeignKey(e => e.PlanContratadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContratoSLA>(entity =>
        {
            entity.Property(e => e.VigenteDesde).HasPrecision(0);
            entity.Property(e => e.VigenteHasta).HasPrecision(0);
            entity.HasIndex(e => e.OrganizacionId).IsUnique().HasFilter("[vigente_hasta] IS NULL");

            // ContratoSLA.Organizacion <-> Organizacion.ContratosSla
            entity.HasOne(e => e.Organizacion)
                .WithMany(o => o.ContratosSla)
                .HasForeignKey(e => e.OrganizacionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContratoSLADetalle>(entity =>
        {
            entity.HasIndex(e => new { e.ContratoId, e.SeveridadId }).IsUnique();
            entity.Property(e => e.HorasSla).HasPrecision(10, 2);

            // Sin navegación declarada en ContratoSLADetalle.cs -> se deja genérico
            entity.HasOne<ContratoSLA>()
                .WithMany()
                .HasForeignKey(e => e.ContratoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Severidad>()
                .WithMany()
                .HasForeignKey(e => e.SeveridadId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.IntentosFallidos).HasDefaultValue(0);
            entity.Property(e => e.BloqueadoHasta).HasPrecision(0);

            // Usuario.Rol -> Rol (sin colección de vuelta)
            entity.HasOne(e => e.Rol)
                .WithMany()
                .HasForeignKey(e => e.RolId)
                .OnDelete(DeleteBehavior.Restrict);

            // Usuario.Organizacion <-> Organizacion.Usuarios
            entity.HasOne(e => e.Organizacion)
                .WithMany(o => o.Usuarios)
                .HasForeignKey(e => e.OrganizacionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(e => e.FechaEmision)
                .HasPrecision(0)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Property(e => e.FechaExpira).HasPrecision(0);
            entity.Property(e => e.Revocado).HasDefaultValue(false);
            entity.HasIndex(e => e.UsuarioId);
            entity.HasIndex(e => e.FechaExpira);

            // Sin navegación declarada en RefreshToken.cs -> se deja genérico
            entity.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Activo>(entity =>
        {
            entity.HasIndex(e => e.OrganizacionId);
            entity.HasIndex(e => e.CategoriaId);

            // Activo.Organizacion <-> Organizacion.Activos
            entity.HasOne(e => e.Organizacion)
                .WithMany(o => o.Activos)
                .HasForeignKey(e => e.OrganizacionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Activo.Categoria -> CategoriaActivo (sin colección de vuelta)
            entity.HasOne(e => e.Categoria)
                .WithMany()
                .HasForeignKey(e => e.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Incidente>(entity =>
        {
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Property(e => e.FechaCierre).HasPrecision(0);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.OrganizacionId);
            entity.HasIndex(e => e.ActivoId);
            entity.HasIndex(e => e.SeveridadId);
            entity.HasIndex(e => e.EstadoId);
            entity.HasIndex(e => e.AnalistaAsignadoId);
            entity.HasIndex(e => e.FechaCreacion);

            // Incidente.Organizacion <-> Organizacion.Incidentes
            entity.HasOne(e => e.Organizacion)
                .WithMany(o => o.Incidentes)
                .HasForeignKey(e => e.OrganizacionId)
                .OnDelete(DeleteBehavior.Restrict);

            // El resto son de un solo sentido (sin colección de vuelta)
            entity.HasOne(e => e.Activo)
                .WithMany()
                .HasForeignKey(e => e.ActivoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Severidad)
                .WithMany()
                .HasForeignKey(e => e.SeveridadId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Estado)
                .WithMany()
                .HasForeignKey(e => e.EstadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AnalistaAsignado)
                .WithMany()
                .HasForeignKey(e => e.AnalistaAsignadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BitacoraIncidente>(entity =>
        {
            entity.Property(e => e.Timestamp)
                .HasPrecision(0)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasIndex(e => e.IncidenteId);
            entity.HasIndex(e => e.UsuarioId);
            entity.HasIndex(e => e.Timestamp);

            entity.HasOne(e => e.Incidente)
                .WithMany()
                .HasForeignKey(e => e.IncidenteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvidenciaIncidente>(entity =>
        {
            entity.Property(e => e.Fecha)
                .HasPrecision(0)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasIndex(e => e.IncidenteId);
            entity.HasIndex(e => e.SubidoPorId);

            entity.HasOne(e => e.Incidente)
                .WithMany()
                .HasForeignKey(e => e.IncidenteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SubidoPor)
                .WithMany()
                .HasForeignKey(e => e.SubidoPorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LogAuditoria>(entity =>
        {
            entity.Property(e => e.Timestamp)
                .HasPrecision(0)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasIndex(e => e.UsuarioId);
            entity.HasIndex(e => e.Timestamp);

            
            entity.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}