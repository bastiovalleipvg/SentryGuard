using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

[Table("tb_Severidad")]
public class Severidad
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Column("nombre")]
    public string Nombre { get; set; } = null!;

    [Column("horas_sla", TypeName = "decimal(10,2)")]
    public decimal HorasSla { get; set; }
}
