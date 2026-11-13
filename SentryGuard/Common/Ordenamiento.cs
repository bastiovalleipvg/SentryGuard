using System.Linq.Expressions;

namespace SentryGuard.Common;

/// <summary>
/// Orden por columna de los listados (RF-06). El cliente envía orden=campo (ascendente) u
/// orden=-campo (descendente). Solo se aceptan las columnas de una lista blanca: nunca se arma
/// un ORDER BY con texto del cliente (OWASP API8, inyección).
/// </summary>
public static class Ordenamiento
{
    public static (string Campo, bool Descendente)? Interpretar(string? orden)
    {
        if (string.IsNullOrWhiteSpace(orden)) return null;
        var texto = orden.Trim();
        var descendente = texto.StartsWith('-');
        return (texto.TrimStart('-', '+').ToLowerInvariant(), descendente);
    }

    /// <summary>Errores de validación (formato de la API) si la columna pedida no está permitida; null si es válida.</summary>
    public static Dictionary<string, string[]>? Validar<T>(string? orden, IReadOnlyDictionary<string, Expression<Func<T, object>>> columnas)
    {
        var pedido = Interpretar(orden);
        if (pedido is null || columnas.ContainsKey(pedido.Value.Campo)) return null;
        return new Dictionary<string, string[]>
        {
            ["orden"] = [$"Columna de orden no válida. Usa una de: {string.Join(", ", columnas.Keys)} (con prefijo '-' para descendente)."]
        };
    }

    /// <summary>Aplica el orden pedido (con desempate estable) o el orden por defecto.</summary>
    public static IOrderedQueryable<T> Ordenar<T>(
        this IQueryable<T> consulta,
        string? orden,
        IReadOnlyDictionary<string, Expression<Func<T, object>>> columnas,
        Func<IQueryable<T>, IOrderedQueryable<T>> porDefecto,
        Expression<Func<T, object>> desempate)
    {
        var pedido = Interpretar(orden);
        if (pedido is null || !columnas.TryGetValue(pedido.Value.Campo, out var columna))
            return porDefecto(consulta);

        var ordenada = pedido.Value.Descendente ? consulta.OrderByDescending(columna) : consulta.OrderBy(columna);
        return ordenada.ThenBy(desempate);
    }
}
