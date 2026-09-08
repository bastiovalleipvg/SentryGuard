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

    // Hash pre-calculado con el mismo PasswordHasher<Usuario> que usa AuthService,
    // para la contraseña de prueba "Admin123!" (no se puede generar dentro de HasData
    // porque el hasher usa salt aleatorio y HasData exige valores estáticos).
    private const string PASSWORD_HASH_PRUEBA =
        "AQAAAAIAAYagAAAAEA7tnp5gva8xrGxWDTthC+lZOlvP37Z6w0rYJiGbxYzLWfHz5fvYXt7Usi6skqdweQ==";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasIndex(e => e.Nombre).IsUnique();

            entity.HasData(
                new Rol { Id = 1, Nombre = "Administrador" },
                new Rol { Id = 2, Nombre = "Supervisor" },
                new Rol { Id = 3, Nombre = "Analista" },
                new Rol { Id = 4, Nombre = "Cliente" }
            );
        });

        modelBuilder.Entity<PlanServicio>(entity =>
        {
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.Property(e => e.TiempoRespuestaBaseH).HasPrecision(10, 2);

            entity.HasData(
                new PlanServicio { Id = 1, Nombre = "Básico", Descripcion = "Plan de servicio básico", TiempoRespuestaBaseH = 24m },
                new PlanServicio { Id = 2, Nombre = "Estándar", Descripcion = "Plan de servicio estándar", TiempoRespuestaBaseH = 12m },
                new PlanServicio { Id = 3, Nombre = "Premium", Descripcion = "Plan de servicio premium", TiempoRespuestaBaseH = 4m }
            );
        });

        modelBuilder.Entity<CategoriaActivo>(entity =>
        {
            entity.HasIndex(e => e.Nombre).IsUnique();

            entity.HasData(
                new CategoriaActivo { Id = 1, Nombre = "Servidor" },
                new CategoriaActivo { Id = 2, Nombre = "Endpoint" },
                new CategoriaActivo { Id = 3, Nombre = "Red" },
                new CategoriaActivo { Id = 4, Nombre = "Aplicación" },
                new CategoriaActivo { Id = 5, Nombre = "Nube" }
            );
        });

        modelBuilder.Entity<Severidad>(entity =>
        {
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.Property(e => e.HorasSla).HasPrecision(10, 2);

            entity.HasData(
                new Severidad { Id = 1, Nombre = "Baja", HorasSla = 24m },
                new Severidad { Id = 2, Nombre = "Media", HorasSla = 12m },
                new Severidad { Id = 3, Nombre = "Alta", HorasSla = 4m },
                new Severidad { Id = 4, Nombre = "Crítica", HorasSla = 1m }
            );
        });

        modelBuilder.Entity<EstadoIncidente>(entity =>
        {
            entity.HasIndex(e => e.Nombre).IsUnique();

            entity.HasData(
                new EstadoIncidente { Id = 1, Nombre = "Abierto" },
                new EstadoIncidente { Id = 2, Nombre = "En progreso" },
                new EstadoIncidente { Id = 3, Nombre = "Escalado" },
                new EstadoIncidente { Id = 4, Nombre = "Resuelto" },
                new EstadoIncidente { Id = 5, Nombre = "Cerrado" }
            );
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

            entity.HasData(
                new
                {
                    Id = 1,
                    RazonSocial = "Empresa Demo SpA",
                    Rut = "76123456-7",
                    PlanContratadoId = 2,
                    FechaAlta = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
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

            // Usuarios de prueba (misma contraseña de prueba "Admin123!" para los 4, ver PASSWORD_HASH_PRUEBA)
            entity.HasData(
                new
                {
                    Id = 1,
                    Nombre = "Administrador SentryGuard",
                    Email = "admin@sentryguard.cl",
                    PasswordHash = PASSWORD_HASH_PRUEBA,
                    RolId = 1,
                    OrganizacionId = (int?)null,
                    Activo = true,
                    IntentosFallidos = 0
                },
                new
                {
                    Id = 2,
                    Nombre = "Supervisor Demo",
                    Email = "supervisor@sentryguard.cl",
                    PasswordHash = PASSWORD_HASH_PRUEBA,
                    RolId = 2,
                    OrganizacionId = (int?)1,
                    Activo = true,
                    IntentosFallidos = 0
                },
                new
                {
                    Id = 3,
                    Nombre = "Analista Demo",
                    Email = "analista@sentryguard.cl",
                    PasswordHash = PASSWORD_HASH_PRUEBA,
                    RolId = 3,
                    OrganizacionId = (int?)1,
                    Activo = true,
                    IntentosFallidos = 0
                },
                new
                {
                    Id = 4,
                    Nombre = "Cliente Demo",
                    Email = "cliente@sentryguard.cl",
                    PasswordHash = PASSWORD_HASH_PRUEBA,
                    RolId = 4,
                    OrganizacionId = (int?)1,
                    Activo = true,
                    IntentosFallidos = 0
                }
            );
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