using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SentryGuard.Common;
using SentryGuard.DTOs;
using SentryGuard.Services;
using System.Security.Claims;
using System.Threading;

namespace SentryGuard.Endpoints
{
    /// <summary>
    /// Endpoints para el módulo de Inteligencia Artificial (RF-23 / RF-24 / Hito 3).
    /// </summary>
    public static class IaAPI
    {
        private const int TituloMax = 250;
        private const int DescripcionMax = 5000;
        private const int ContextoMax = 10000;

        public static void MapIaApi(this IEndpointRouteBuilder app)
        {
            var grupo = app.MapGroup("/api/v1/ia")
                .WithTags("Inteligencia Artificial")
                .RequireAuthorization();

            // RF-23: Sugerir severidad, categoría y primeras acciones para un nuevo incidente
            grupo.MapPost("/clasificar-incidente", async (
                ClasificarIncidenteRequest request,
                IAsistenteIA asistenteIa,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Titulo) || request.Titulo.Trim().Length < 5)
                {
                    return ApiResults.Validacion(new System.Collections.Generic.Dictionary<string, string[]>
                    {
                        ["titulo"] = ["El título debe tener al menos 5 caracteres para ser analizado."]
                    });
                }

                if (request.Titulo.Length > TituloMax)
                {
                    return ApiResults.Validacion(new System.Collections.Generic.Dictionary<string, string[]>
                    {
                        ["titulo"] = [$"El título no puede exceder {TituloMax} caracteres."]
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Descripcion) || request.Descripcion.Trim().Length < 10)
                {
                    return ApiResults.Validacion(new System.Collections.Generic.Dictionary<string, string[]>
                    {
                        ["descripcion"] = ["La descripción debe tener al menos 10 caracteres para que la IA comprenda el contexto."]
                    });
                }

                if (request.Descripcion.Length > DescripcionMax)
                {
                    return ApiResults.Validacion(new System.Collections.Generic.Dictionary<string, string[]>
                    {
                        ["descripcion"] = [$"La descripción no puede exceder {DescripcionMax} caracteres."]
                    });
                }

                var resultado = await asistenteIa.ClasificarIncidenteAsync(
                    request.Titulo.Trim(),
                    request.Descripcion.Trim(),
                    cancellationToken);

                return resultado.ToHttpResult();
            })
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Supervisor", "Cliente"))
            .WithName("ClasificarIncidenteIa")
            .WithSummary("Sugiere severidad, categoría y acciones de contención para un incidente (RF-23)");

            // RF-24: Resumen en lenguaje natural de métricas o contexto
            grupo.MapPost("/resumir", async (
                ResumirIaRequest request,
                IAsistenteIA asistenteIa,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Contexto) || request.Contexto.Trim().Length < 10)
                {
                    return ApiResults.Validacion(new System.Collections.Generic.Dictionary<string, string[]>
                    {
                        ["contexto"] = ["El contexto a resumir debe tener al menos 10 caracteres."]
                    });
                }

                if (request.Contexto.Length > ContextoMax)
                {
                    return ApiResults.Validacion(new System.Collections.Generic.Dictionary<string, string[]>
                    {
                        ["contexto"] = [$"El contexto no puede superar los {ContextoMax} caracteres."]
                    });
                }

                var resultado = await asistenteIa.ResumirAsync(
                    request.Contexto.Trim(),
                    request.Tipo,
                    cancellationToken);

                return resultado.ToHttpResult();
            })
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Supervisor"))
            .WithName("ResumirIa")
            .WithSummary("Genera un resumen ejecutivo en lenguaje natural (RF-24)");
        }
    }
}
