using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

/// <summary>Clasificación del incidente (Phishing, Malware, ...). La usan la IA (RF-23) y el dashboard.</summary>
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
}
