using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

[Table("tb_ContratoSLA")]
public class ContratoSLA
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organizacion_id")]
    public int OrganizacionId { get; set; }

    [Column("vigente_desde")]
    public DateTime VigenteDesde { get; set; }

    [Column("vigente_hasta")]
    public DateTime? VigenteHasta { get; set; }

    [ForeignKey("OrganizacionId")]
    public virtual Organizacion? Organizacion { get; set; }
}