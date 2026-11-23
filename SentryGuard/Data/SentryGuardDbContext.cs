using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SentryGuard.Models;
using SentryGuard.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SentryGuard.Data;

public class SentryGuardDbContext : DbContext
{
    private readonly IUsuarioActual? _usuarioActual;

    public SentryGuardDbContext(DbContextOptions<SentryGuardDbContext> options, IUsuarioActual? usuarioActual = null)
        : base(options)
    {
        _usuarioActual = usuarioActual;
    }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<PlanServicio> PlanesServicio => Set<PlanServicio>();
    public DbSet<CategoriaActivo> CategoriasActivo => Set<CategoriaActivo>();
    public DbSet<CategoriaIncidente> CategoriasIncidente => Set<CategoriaIncidente>();
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
    public DbSet<TransicionEstado> TransicionesEstado => Set<TransicionEstado>();

    // Vistas SQL para reportes y dashboard (solo lectura)
    public DbSet<IncidenteDetalleVista> VwIncidenteDetalle => Set<IncidenteDetalleVista>();
    public DbSet<CumplimientoSlaVista> VwCumplimientoSla => Set<CumplimientoSlaVista>();
    public DbSet<IndicadorMensualVista> VwIndicadoresMensuales => Set<IndicadorMensualVista>();

    // Hash pre-calculado con el mismo PasswordHasher<Usuario> que usa AuthService,
    // para la contraseña de prueba "Admin123!" (no se puede generar dentro de HasData
    // porque el hasher usa salt aleatorio y HasData exige valores estáticos).
    private static readonly DateTime FechaSeed = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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
            entity.ToTable("tb_PlanServicio", t =>
                t.HasCheckConstraint("CK_tb_PlanServicio_TiempoRespuesta", "[tiempo_respuesta_base_h] > 0"));

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
            entity.ToTable("tb_Severidad", t =>
                t.HasCheckConstraint("CK_tb_Severidad_HorasSla", "[horas_sla] > 0 AND [horas_sla] <= 720"));

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
            entity.ToTable("tb_Organizacion", t =>
                t.HasCheckConstraint("CK_tb_Organizacion_RutFormato", "LEN([rut]) >= 9 AND LEN([rut]) <= 10 AND [rut] LIKE '%-%'"));

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
            entity.ToTable("tb_ContratoSLA", t =>
                t.HasCheckConstraint("CK_tb_ContratoSLA_Vigencia", "[vigente_hasta] IS NULL OR [vigente_hasta] > [vigente_desde]"));

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
            entity.ToTable("tb_ContratoSLA_Detalle", t =>
                t.HasCheckConstraint("CK_tb_ContratoSLA_Detalle_HorasSla", "[horas_sla] > 0 AND [horas_sla] <= 720"));

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
            entity.ToTable("tb_Usuario", t =>
                t.HasCheckConstraint("CK_tb_Usuario_IntentosFallidos", "[intentos_fallidos] >= 0"));

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
                    OrganizacionId = (int?)null, // staff interno de CyberOps, no pertenece a una organizacion cliente
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
                    OrganizacionId = (int?)null, // staff interno; su acceso se valida por incidente asignado
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
            entity.Property(e => e.FechaCreacion).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.FechaModificacion).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne<Usuario>().WithMany().HasForeignKey(e => e.CreadoPorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Usuario>().WithMany().HasForeignKey(e => e.ModificadoPorId).OnDelete(DeleteBehavior.Restrict);

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

            // Activos de demostracion de la organizacion demo (id=1): sin ellos un Cliente no puede
            // registrar incidentes desde el frontend, porque solo Administrador/Supervisor crean activos.
            entity.HasData(
                new Activo { Id = 1, OrganizacionId = 1, Nombre = "Servidor web de producción", HostnameIp = "10.10.0.10", CategoriaId = 1, FechaCreacion = FechaSeed, FechaModificacion = FechaSeed },
                new Activo { Id = 2, OrganizacionId = 1, Nombre = "Firewall perimetral", HostnameIp = "10.10.0.1", CategoriaId = 3, FechaCreacion = FechaSeed, FechaModificacion = FechaSeed },
                new Activo { Id = 3, OrganizacionId = 1, Nombre = "Portal de clientes", HostnameIp = "portal.demo.cl", CategoriaId = 4, FechaCreacion = FechaSeed, FechaModificacion = FechaSeed }
            );
        });

        modelBuilder.Entity<Incidente>(entity =>
        {
            entity.ToTable("tb_Incidente", t =>
            {
                t.HasCheckConstraint("CK_tb_Incidente_TituloMinimo", "LEN(LTRIM(RTRIM([titulo]))) >= 5");
                t.HasCheckConstraint("CK_tb_Incidente_FechaCierre", "[fecha_cierre] IS NULL OR [fecha_cierre] >= [fecha_creacion]");
            });

            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Property(e => e.FechaCierre).HasPrecision(0);
            entity.Property(e => e.FechaModificacion).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.CategoriaId);
            entity.HasOne<Usuario>().WithMany().HasForeignKey(e => e.CreadoPorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Usuario>().WithMany().HasForeignKey(e => e.ModificadoPorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Categoria).WithMany().HasForeignKey(e => e.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.OrganizacionId);
            entity.HasIndex(e => e.ActivoId);
            entity.HasIndex(e => e.SeveridadId);
            entity.HasIndex(e => e.CategoriaId);
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

            entity.HasOne(e => e.Categoria)
                .WithMany()
                .HasForeignKey(e => e.CategoriaId)
                .OnDelete(DeleteBehavior.SetNull);

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
            entity.HasIndex(e => new { e.Entidad, e.EntidadId });

            entity.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CategoriaIncidente>(entity =>
        {
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.HasData(
                new CategoriaIncidente { Id = 1, Nombre = "Phishing" },
                new CategoriaIncidente { Id = 2, Nombre = "Malware" },
                new CategoriaIncidente { Id = 3, Nombre = "Acceso no autorizado" },
                new CategoriaIncidente { Id = 4, Nombre = "Denegación de servicio" },
                new CategoriaIncidente { Id = 5, Nombre = "Fuga de datos" },
                new CategoriaIncidente { Id = 6, Nombre = "Otro" }
            );
        });

        // Flujo de estados como datos (ADR-008). Ids de estado: 1 Abierto, 2 En progreso, 3 Escalado, 4 Resuelto, 5 Cerrado.
        modelBuilder.Entity<TransicionEstado>(entity =>
        {
            entity.ToTable(t => t.HasCheckConstraint("CK_tb_TransicionEstado_OrigenDistintoDestino", "[estado_origen_id] <> [estado_destino_id]"));
            entity.HasIndex(e => new { e.EstadoOrigenId, e.EstadoDestinoId }).IsUnique();

            entity.HasOne<EstadoIncidente>().WithMany().HasForeignKey(e => e.EstadoOrigenId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<EstadoIncidente>().WithMany().HasForeignKey(e => e.EstadoDestinoId).OnDelete(DeleteBehavior.Restrict);

            var pares = new (int Origen, int Destino)[]
            {
                (1, 2), (1, 3), (1, 4), (1, 5),
                (2, 3), (2, 4), (2, 5),
                (3, 2), (3, 4), (3, 5),
                (4, 2), (4, 5),   // Resuelto -> En progreso (reapertura) o Cerrado
                (5, 1), (5, 2)    // Cerrado -> reapertura
            };
            entity.HasData(pares.Select((p, i) => new TransicionEstado { Id = i + 1, EstadoOrigenId = p.Origen, EstadoDestinoId = p.Destino }));
        });

        // Vistas SQL: se crean en la migración con SQL; EF solo las lee.
        modelBuilder.Entity<IncidenteDetalleVista>(e => { e.HasNoKey(); e.ToView("vw_IncidenteDetalle"); e.Property(v => v.HorasResolucion).HasPrecision(12, 2); });
        modelBuilder.Entity<CumplimientoSlaVista>(e => { e.HasNoKey(); e.ToView("vw_CumplimientoSla"); e.Property(v => v.HorasSla).HasPrecision(12, 2); e.Property(v => v.HorasTranscurridas).HasPrecision(12, 2); });
        modelBuilder.Entity<IndicadorMensualVista>(e => { e.HasNoKey(); e.ToView("vw_IndicadoresMensuales"); e.Property(v => v.PromedioHorasResolucion).HasPrecision(12, 2); });
    }

    private static readonly HashSet<string> CamposSinAuditar = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash",
        "RowVersion",
        "FechaModificacion",
        "ModificadoPorId"
    };

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(true, cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var ahora = DateTime.UtcNow;
        var usuarioId = _usuarioActual?.UsuarioId;
        var ipOrigen = _usuarioActual?.IpOrigen ?? _usuarioActual?.Ip ?? "SISTEMA";

        // 1. Sellar fechas y usuarios para entidades auditables
        foreach (var entrada in ChangeTracker.Entries<IAuditable>().ToList())
        {
            if (entrada.State == EntityState.Added)
            {
                if (entrada.Entity.FechaCreacion == default) entrada.Entity.FechaCreacion = ahora;
                if (entrada.Entity.FechaModificacion == default) entrada.Entity.FechaModificacion = entrada.Entity.FechaCreacion;
                entrada.Entity.CreadoPorId ??= usuarioId;
                entrada.Entity.ModificadoPorId ??= usuarioId;
            }
            else if (entrada.State == EntityState.Modified)
            {
                entrada.Entity.FechaModificacion = ahora;
                entrada.Entity.ModificadoPorId = usuarioId;
            }
        }

        // 2. Preparar registros de auditoría
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is not LogAuditoria &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        if (entries.Count == 0)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        var auditorias = new List<LogAuditoria>();

        foreach (var entry in entries)
        {
            var nombreEntidad = entry.Entity.GetType().Name;
            var accion = entry.State switch
            {
                EntityState.Added => "CREAR",
                EntityState.Modified => "MODIFICAR",
                EntityState.Deleted => "ELIMINAR",
                _ => entry.State.ToString()
            };

            string? valoresAnteriores = null;
            string? valoresNuevos = null;

            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                var dicAnterior = new Dictionary<string, object?>();
                foreach (var prop in entry.OriginalValues.Properties)
                {
                    if (!CamposSinAuditar.Contains(prop.Name))
                        dicAnterior[prop.Name] = entry.OriginalValues[prop];
                }
                valoresAnteriores = JsonSerializer.Serialize(dicAnterior);
            }

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                var dicNuevo = new Dictionary<string, object?>();
                foreach (var prop in entry.CurrentValues.Properties)
                {
                    if (!CamposSinAuditar.Contains(prop.Name))
                        dicNuevo[prop.Name] = entry.CurrentValues[prop];
                }
                valoresNuevos = JsonSerializer.Serialize(dicNuevo);
            }

            var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
            int? entidadId = null;
            if (idProp?.CurrentValue != null && int.TryParse(idProp.CurrentValue.ToString(), out var parsedId))
            {
                entidadId = parsedId;
            }

            auditorias.Add(new LogAuditoria
            {
                UsuarioId = usuarioId,
                Accion = $"{accion} {nombreEntidad}",
                Entidad = nombreEntidad,
                EntidadId = entidadId,
                ValoresAnteriores = valoresAnteriores,
                ValoresNuevos = valoresNuevos,
                IpOrigen = ipOrigen,
                Timestamp = ahora
            });
        }

        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        if (auditorias.Count > 0)
        {
            foreach (var log in auditorias)
            {
                LogsAuditoria.Add(log);
            }
            await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Punto de extensión para registrar acciones explícitas como exportación de reportes (RNF-11 / Hito 4).
    /// </summary>
    public async Task RegistrarAccionAsync(string entidad, string accion, int? entidadId = null, CancellationToken cancellationToken = default)
    {
        LogsAuditoria.Add(new LogAuditoria
        {
            UsuarioId = _usuarioActual?.UsuarioId,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            IpOrigen = _usuarioActual?.IpOrigen ?? _usuarioActual?.Ip ?? "SISTEMA",
            Timestamp = DateTime.UtcNow
        });
        await base.SaveChangesAsync(cancellationToken);
    }
}
