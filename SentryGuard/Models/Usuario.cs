using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

[Table("tb_Usuario")]
public class Usuario
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    [Column("nombre")]
    public string Nombre { get; set; } = null!;

    [Required]
    [StringLength(320)]
    [Column("email")]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(500)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = null!;

    [Column("rol_id")]
    public int RolId { get; set; }

    [Column("organizacion_id")]
    public int? OrganizacionId { get; set; }

    [Column("activo")]
    public bool Activo { get; set; }

    [Column("intentos_fallidos")]
    public int IntentosFallidos { get; set; }

    [Column("bloqueado_hasta")]
    public DateTime? BloqueadoHasta { get; set; }

    // Navigation properties
    [ForeignKey("RolId")]
    public virtual Rol? Rol { get; set; }

    [ForeignKey("OrganizacionId")]
    public virtual Organizacion? Organizacion { get; set; }
}
