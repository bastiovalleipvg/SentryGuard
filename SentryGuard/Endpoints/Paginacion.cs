namespace SentryGuard.Endpoints
{
    // Pagina acotada: un pageSize arbitrario permitiria descargar una tabla completa en una sola request (OWASP API4).
    internal static class Paginacion
    {
        public const int PageSizeMax = 50;

        public static (int Page, int PageSize) Normalizar(int page, int pageSize) =>
            (Math.Max(page, 1), Math.Clamp(pageSize, 1, PageSizeMax));
    }
}
