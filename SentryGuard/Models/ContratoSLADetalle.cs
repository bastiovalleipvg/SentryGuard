using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

[Table("tb_ContratoSLA_Detalle")]
public class ContratoSLADetalle
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("contrato_id")]
    public int ContratoId { get; set; }

    [Column("severidad_id")]
    public int SeveridadId { get; set; }

    [Column("horas_sla", TypeName = "decimal(10,2)")]
    public decimal HorasSla { get; set; }
}
