using System.Security.Claims;

namespace SentryGuard.Services;

/// <summary>Usuario autenticado de la petición en curso, para sellar la auditoría al guardar.</summary>
public interface IUsuarioActual
{
    int? UsuarioId { get; }
    string? Ip { get; }
}

public class HttpUsuarioActual : IUsuarioActual
{
    private readonly IHttpContextAccessor _accessor;
    public HttpUsuarioActual(IHttpContextAccessor accessor) => _accessor = accessor;

    public int? UsuarioId =>
        int.TryParse(_accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    public string? Ip => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
