using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

[Table("tb_PlanServicio")]
public class PlanServicio
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Column("nombre")]
    public string Nombre { get; set; } = null!;

    [StringLength(500)]
    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("tiempo_respuesta_base_h", TypeName = "decimal(10,2)")]
    public decimal TiempoRespuestaBaseH { get; set; }
}
