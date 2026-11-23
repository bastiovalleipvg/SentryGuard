using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

[Table("tb_Incidente")]
public class Incidente : IAuditable
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organizacion_id")]
    public int OrganizacionId { get; set; }

    [Column("activo_id")]
    public int ActivoId { get; set; }

    [Required]
    [StringLength(250)]
    [Column("titulo")]
    public string Titulo { get; set; } = null!;

    [Required]
    [Column("descripcion")]
    public string Descripcion { get; set; } = null!;

    [Column("severidad_id")]
    public int SeveridadId { get; set; }

    [Column("categoria_id")]
    public int? CategoriaId { get; set; }

    [Column("estado_id")]
    public int EstadoId { get; set; }

    [Column("analista_asignado_id")]
    public int? AnalistaAsignadoId { get; set; }

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; }

    [Column("fecha_cierre")]
    public DateTime? FechaCierre { get; set; }

    // Auditoría (RF-16)
    [Column("fecha_modificacion")]
    public DateTime FechaModificacion { get; set; }

    [Column("creado_por_id")]
    public int? CreadoPorId { get; set; }

    [Column("modificado_por_id")]
    public int? ModificadoPorId { get; set; }

    [Timestamp]
    [Column("row_version")]
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    [ForeignKey("OrganizacionId")]
    public virtual Organizacion? Organizacion { get; set; }

    [ForeignKey("ActivoId")]
    public virtual Activo? Activo { get; set; }

    [ForeignKey("SeveridadId")]
    public virtual Severidad? Severidad { get; set; }

    [ForeignKey("CategoriaId")]
    public virtual CategoriaIncidente? Categoria { get; set; }

    [ForeignKey("EstadoId")]
    public virtual EstadoIncidente? Estado { get; set; }

    [ForeignKey("AnalistaAsignadoId")]
    public virtual Usuario? AnalistaAsignado { get; set; }
}
