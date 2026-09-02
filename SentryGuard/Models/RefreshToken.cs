using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

[Table("tb_RefreshToken")]
public class RefreshToken
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Required]
    [StringLength(500)]
    [Column("token_hash")]
    public string TokenHash { get; set; } = null!;

    [Column("fecha_emision")]
    public DateTime FechaEmision { get; set; }

    [Column("fecha_expira")]
    public DateTime FechaExpira { get; set; }

    [Column("revocado")]
    public bool Revocado { get; set; }
}
