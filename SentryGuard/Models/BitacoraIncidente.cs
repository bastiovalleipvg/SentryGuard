using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

[Table("tb_BitacoraIncidente")]
public class BitacoraIncidente
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("incidente_id")]
    public int IncidenteId { get; set; }

    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Required]
    [StringLength(100)]
    [Column("accion")]
    public string Accion { get; set; } = null!;

    [Column("comentario")]
    public string? Comentario { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; }

    // Navigation properties
    [ForeignKey("IncidenteId")]
    public virtual Incidente? Incidente { get; set; }

    [ForeignKey("UsuarioId")]
    public virtual Usuario? Usuario { get; set; }
}
