using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

/// <summary>
/// Catálogo de categorías de incidente de ciberseguridad (RF-15, RF-23).
/// </summary>
[Table("tb_CategoriaIncidente")]
public class CategoriaIncidente
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    [Column("nombre")]
    public string Nombre { get; set; } = null!;

    [StringLength(250)]
    [Column("descripcion")]
    public string? Descripcion { get; set; }
}
