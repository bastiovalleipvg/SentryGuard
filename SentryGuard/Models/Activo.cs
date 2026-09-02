using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

[Table("tb_Activo")]
public class Activo
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organizacion_id")]
    public int OrganizacionId { get; set; }

    [Required]
    [StringLength(150)]
    [Column("nombre")]
    public string Nombre { get; set; } = null!;

    [StringLength(255)]
    [Column("hostname_ip")]
    public string? HostnameIp { get; set; }

    [Column("categoria_id")]
    public int CategoriaId { get; set; }
}
