using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

/// <summary>
/// Registro inmutable de auditoría para trazabilidad de cambios y acciones del sistema (RNF-11 / RF-16).
/// </summary>
[Table("tb_LogAuditoria")]
public class LogAuditoria
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("usuario_id")]
    public int? UsuarioId { get; set; }

    [Required]
    [StringLength(150)]
    [Column("accion")]
    public string Accion { get; set; } = null!;

    [StringLength(50)]
    [Column("entidad")]
    public string? Entidad { get; set; }

    [Column("entidad_id")]
    public int? EntidadId { get; set; }

    [Column("valores_anteriores")]
    public string? ValoresAnteriores { get; set; }

    [Column("valores_nuevos")]
    public string? ValoresNuevos { get; set; }

    [StringLength(45)]
    [Column("ip_origen")]
    public string? IpOrigen { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; }
}
