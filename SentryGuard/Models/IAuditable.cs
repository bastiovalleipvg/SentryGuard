namespace SentryGuard.Models;

/// <summary>
/// Entidad principal con campos de auditoría. SentryGuardDbContext los completa al guardar
/// (RF-16): quién y cuándo creó o modificó el registro.
/// </summary>
public interface IAuditable
{
    DateTime FechaCreacion { get; set; }
    DateTime FechaModificacion { get; set; }
    int? CreadoPorId { get; set; }
    int? ModificadoPorId { get; set; }
}
