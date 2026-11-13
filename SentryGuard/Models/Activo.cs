using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

[Table("tb_Activo")]
public class Activo : IAuditable
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

    // Auditoría (RF-16)
    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; }

    [Column("fecha_modificacion")]
    public DateTime FechaModificacion { get; set; }

    [Column("creado_por_id")]
    public int? CreadoPorId { get; set; }

    [Column("modificado_por_id")]
    public int? ModificadoPorId { get; set; }

    // Navigation properties
    [ForeignKey("OrganizacionId")]
    public virtual Organizacion? Organizacion { get; set; }

    [ForeignKey("CategoriaId")]
    public virtual CategoriaActivo? Categoria { get; set; }
}
