using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentryGuard.Models;

[Table("tb_Organizacion")]
public class Organizacion
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Column("razon_social")]
    public string RazonSocial { get; set; } = null!;

    [Required]
    [StringLength(10)]
    [Column("rut")]
    public string Rut { get; set; } = null!;

    [Column("plan_contratado_id")]
    public int PlanContratadoId { get; set; }

    [Column("fecha_alta")]
    public DateTime FechaAlta { get; set; }

    // Navigation properties
    [ForeignKey("PlanContratadoId")]
    public virtual PlanServicio? Plan { get; set; }

    [InverseProperty("Organizacion")]
    public virtual ICollection<Activo>? Activos { get; set; }

    [InverseProperty("Organizacion")]
    public virtual ICollection<Incidente>? Incidentes { get; set; }

    [InverseProperty("Organizacion")]
    public virtual ICollection<ContratoSLA>? ContratosSla { get; set; }

    [InverseProperty("Organizacion")]
    public virtual ICollection<Usuario>? Usuarios { get; set; }
}
