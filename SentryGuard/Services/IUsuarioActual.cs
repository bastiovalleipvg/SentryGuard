using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SentryGuard.Services;

/// <summary>
/// Usuario autenticado de la petición en curso y contexto de red para sellar la auditoría al guardar (ADR-010 / RNF-11).
/// </summary>
public interface IUsuarioActual
{
    int? UsuarioId { get; }
    string? Ip { get; }
    string? IpOrigen => Ip;
}

public class HttpUsuarioActual : IUsuarioActual
{
    private readonly IHttpContextAccessor _accessor;

    public HttpUsuarioActual(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public int? UsuarioId =>
        int.TryParse(_accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    public string? Ip => _accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
}

public class UsuarioActualHttp : HttpUsuarioActual
{
    public UsuarioActualHttp(IHttpContextAccessor accessor) : base(accessor) { }
}
