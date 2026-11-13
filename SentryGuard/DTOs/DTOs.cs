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
        public int RolId { get; init; }
        public string Rol { get; init; } = null!;
        public int? OrganizacionId { get; init; }
        public string? Organizacion { get; init; }
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
        public int SeveridadId { get; init; }
        public string Severidad { get; init; } = null!;
        public int EstadoId { get; init; }
        public string Estado { get; init; } = null!;
        public int ActivoId { get; init; }
        public string Activo { get; init; } = null!;
        public int OrganizacionId { get; init; }
        public int? AnalistaAsignadoId { get; init; }
        public string? AnalistaAsignado { get; init; }
        public int? CategoriaId { get; init; }
        public string? Categoria { get; init; }
        public DateTime FechaCreacion { get; init; }
        public DateTime? FechaCierre { get; init; }
        public DateTime FechaModificacion { get; init; }
    }

    /// <summary>Par de estados permitido en el flujo (tb_TransicionEstado).</summary>
    public record TransicionDTO
    {
        public int EstadoOrigenId { get; init; }
        public int EstadoDestinoId { get; init; }
    }

    /// <summary>
    /// DTO de activo para salida.
    /// </summary>
    public record ActivoDTO
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = null!;
        public string? HostnameIp { get; init; }
        public int CategoriaId { get; init; }
        public string Categoria { get; init; } = null!;
        public int OrganizacionId { get; init; }
        public string Organizacion { get; init; } = null!;
    }

    /// <summary>
    /// DTO de organización para salida.
    /// </summary>
    public record OrganizacionDTO
    {
        public int Id { get; init; }
        public string RazonSocial { get; init; } = null!;
        public string Rut { get; init; } = null!;
        public int PlanId { get; init; }
        public string Plan { get; init; } = null!;
        public DateTime FechaAlta { get; init; }
    }

    /// <summary>
    /// DTO de contrato SLA de una organizacion, con las horas comprometidas por severidad.
    /// </summary>
    public record ContratoSlaDTO
    {
        public int Id { get; init; }
        public DateTime VigenteDesde { get; init; }
        public DateTime? VigenteHasta { get; init; }
        public List<ContratoSlaDetalleDTO> Detalles { get; init; } = [];
    }

    public record ContratoSlaDetalleDTO
    {
        public int SeveridadId { get; init; }
        public string Severidad { get; init; } = null!;
        public decimal HorasSla { get; init; }
    }

    /// <summary>
    /// DTO del registro de auditoria (solo Administrador).
    /// </summary>
    public record AuditoriaDTO
    {
        public long Id { get; init; }
        public string? Usuario { get; init; }
        public string Accion { get; init; } = null!;
        public string? IpOrigen { get; init; }
        public string? Entidad { get; init; }
        public int? EntidadId { get; init; }
        public string? ValoresAnteriores { get; init; }
        public string? ValoresNuevos { get; init; }
        public DateTime Timestamp { get; init; }
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
        // Contrato que consume el frontend (CumplimientoSlaSchema): no cambiar los nombres.
        public double HorasTranscurridas { get; init; }
        public double HorasSLA { get; init; }
        public bool Cumple { get; init; }
        public string Fuente { get; init; } = "Severidad";
        public DateTime FechaLimite { get; init; }
        public bool Cerrado { get; init; }
        public double PorcentajeUtilizacion { get; init; }
    }

    // ==========================================
    // DTOs para el Módulo de IA (RF-23 y RF-24)
    // ==========================================

    /// <summary>
    /// Entrada para sugerencia de clasificación de incidente con IA (RF-23).
    /// </summary>
    public record ClasificarIncidenteRequest(string Titulo, string Descripcion);

    /// <summary>
    /// Respuesta con la sugerencia estructurada de la IA (RF-23).
    /// </summary>
    public record ClasificacionIaDTO(
        int SeveridadSugeridaId,
        string SeveridadNombre,
        string CategoriaSugerida,
        string Justificacion,
        List<string> AccionesRecomendadas
    );

    /// <summary>
    /// Entrada para generación de resumen ejecutivo con IA (RF-24).
    /// </summary>
    public record ResumirIaRequest(string Contexto, string? Tipo = "general");

    /// <summary>
    /// Respuesta con el resumen generado en lenguaje natural (RF-24).
    /// </summary>
    public record ResumenIaDTO(
        string Resumen,
        DateTime GeneradoEn,
        string Modelo
    );
}
