namespace SentryGuard.DTOs
{
    /// <summary>
    /// DTO de usuario para login response (OWASP API3: no exponer password_hash).
    /// </summary>
    public record UsuarioLoginDTO
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string Rol { get; init; } = null!;
        public int? OrganizacionId { get; init; }
    }

    /// <summary>
    /// DTO para GET /me (usuario actual).
    /// </summary>
    public record UsuarioMeDTO
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string Rol { get; init; } = null!;
        public bool Activo { get; init; }
    }

    /// <summary>
    /// DTO para listados de usuarios (sin datos sensibles).
    /// </summary>
    public record UsuarioListDTO
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string Rol { get; init; } = null!;
        public bool Activo { get; init; }
    }

    /// <summary>
    /// DTO de incidente para salida (OWASP API3).
    /// </summary>
    public record IncidenteDTO
    {
        public int Id { get; init; }
        public string Titulo { get; init; } = null!;
        public string Descripcion { get; init; } = null!;
        public string Severidad { get; init; } = null!;
        public string Estado { get; init; } = null!;
        public string Activo { get; init; } = null!;
        public string? AnalistaAsignado { get; init; }
        public DateTime FechaCreacion { get; init; }
        public DateTime? FechaCierre { get; init; }
    }

    /// <summary>
    /// DTO de activo para salida.
    /// </summary>
    public record ActivoDTO
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = null!;
        public string? HostnameIp { get; init; }
        public string Categoria { get; init; } = null!;
    }

    /// <summary>
    /// DTO de organización para salida.
    /// </summary>
    public record OrganizacionDTO
    {
        public int Id { get; init; }
        public string RazonSocial { get; init; } = null!;
        public string Rut { get; init; } = null!;
        public string Plan { get; init; } = null!;
        public DateTime FechaAlta { get; init; }
    }

    /// <summary>
    /// DTO de BitacoraIncidente para salida (nunca exponer IpOrigen ni datos del request).
    /// </summary>
    public record BitacoraDTO
    {
        public long Id { get; init; }
        public string Accion { get; init; } = null!;
        public string? Comentario { get; init; }
        public string Usuario { get; init; } = null!;
        public DateTime Timestamp { get; init; }
    }

    /// <summary>
    /// DTO de EvidenciaIncidente para salida.
    /// </summary>
    public record EvidenciaDTO
    {
        public long Id { get; init; }
        public string NombreArchivo { get; init; } = null!;
        public string RutaAlmacenamiento { get; init; } = null!;
        public string SubidoPor { get; init; } = null!;
        public DateTime Fecha { get; init; }
    }

    /// <summary>
    /// DTO de cumplimiento SLA.
    /// </summary>
    public record CumplimientoSLADTO
    {
        public int IncidenteId { get; init; }
        public string Titulo { get; init; } = null!;
        public string Estado { get; init; } = null!;
        public double HorasTranscurridas { get; init; }
        public double HorasSLAEstablecidas { get; init; }
        public bool CumpleSLA { get; init; }
        public double PorcentajeUtilizacion { get; init; }
    }
}
