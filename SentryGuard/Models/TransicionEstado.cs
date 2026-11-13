using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

/// <summary>Par (origen, destino) permitido en el flujo de estados del incidente (ADR-008).</summary>
[Table("tb_TransicionEstado")]
public class TransicionEstado
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("estado_origen_id")]
    public int EstadoOrigenId { get; set; }

    [Column("estado_destino_id")]
    public int EstadoDestinoId { get; set; }
}
