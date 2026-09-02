using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

[Table("tb_EvidenciaIncidente")]
public class EvidenciaIncidente
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("incidente_id")]
    public int IncidenteId { get; set; }

    [Required]
    [StringLength(255)]
    [Column("nombre_archivo")]
    public string NombreArchivo { get; set; } = null!;

    [Required]
    [StringLength(1000)]
    [Column("ruta_almacenamiento")]
    public string RutaAlmacenamiento { get; set; } = null!;

    [Column("subido_por_id")]
    public int SubidoPorId { get; set; }

    [Column("fecha")]
    public DateTime Fecha { get; set; }
}
