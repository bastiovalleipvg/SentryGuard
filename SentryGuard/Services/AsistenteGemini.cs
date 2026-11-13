using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SentryGuard.Common;
using SentryGuard.DTOs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SentryGuard.Services
{
    /// <summary>
    /// Adaptador para integración real con APIs de Inteligencia Artificial (ADR-007 / RF-23 / RF-24).
    /// Implementa timeout de 15 segundos (RNF-10) y fallback transparente a simulación heurística si no hay API key configurada.
    /// </summary>
    public class AsistenteGemini : IAsistenteIA
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<AsistenteGemini> _logger;
        private readonly AsistenteSimulado _simulador = new();

        public AsistenteGemini(HttpClient httpClient, IConfiguration config, ILogger<AsistenteGemini> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;

            // Timeout estricto de 15 segundos según RNF-10
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        /// <summary>
        /// La clave viaja en el encabezado x-goog-api-key y no en la URL: asi no queda en los logs del
        /// HttpClient ni en los mensajes de las excepciones.
        /// </summary>
        private Task<HttpResponseMessage> EnviarAsync(string modelo, string apiKey, object cuerpo, CancellationToken cancellationToken)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelo}:generateContent";
            var peticion = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(cuerpo) };
            peticion.Headers.Add("x-goog-api-key", apiKey);
            return _httpClient.SendAsync(peticion, cancellationToken);
        }

        public async Task<ServiceResult<ClasificacionIaDTO>> ClasificarIncidenteAsync(
            string titulo,
            string descripcion,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(descripcion))
            {
                return ServiceResult<ClasificacionIaDTO>.BadRequest(
                    "El título y la descripción son obligatorios para obtener sugerencias de la IA.");
            }

            var apiKey = _config["IA:ApiKey"] ?? Environment.GetEnvironmentVariable("IA__ApiKey");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogInformation("AsistenteGemini: No se encontró IA:ApiKey configurada. Utilizando adaptador simulado heurístico.");
                return await _simulador.ClasificarIncidenteAsync(titulo, descripcion, cancellationToken);
            }

            try
            {
                var modelo = _config["IA:Modelo"] ?? "gemini-2.5-flash";
                
                var prompt = $@"Eres un experto senior en ciberseguridad para un SOC (Centro de Operaciones de Seguridad) CyberOps.
Analiza el siguiente incidente de seguridad y clasifícalo.
Título: {titulo}
Descripción: {descripcion}

Debes responder ÚNICAMENTE un objeto JSON válido con este formato exacto, sin markdown ni explicaciones adicionales:
{{
  ""severidadSugeridaId"": 1_para_baja_2_para_media_3_para_alta_o_4_para_critica,
  ""severidadNombre"": ""Baja|Media|Alta|Crítica"",
  ""categoriaSugerida"": ""Una categoría del catálogo (usa exactamente una de: Phishing, Malware, Acceso no autorizado, Denegación de servicio, Fuga de datos, Otro)"",
  ""justificacion"": ""Explicación técnica concisa de 1 a 2 oraciones del por qué de la severidad asignada"",
  ""accionesRecomendadas"": [""Acción inmediata 1"", ""Acción 2"", ""Acción 3""]
}}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new
                    {
                        responseMimeType = "application/json",
                        temperature = 0.2
                    }
                };

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var response = await EnviarAsync(modelo, apiKey, requestBody, linkedCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AsistenteGemini: Error HTTP {Status} desde el proveedor de IA. Aplicando fallback heurístico.", response.StatusCode);
                    return await _simulador.ClasificarIncidenteAsync(titulo, descripcion, cancellationToken);
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: linkedCts.Token);
                var rawText = jsonResponse
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(rawText))
                    return await _simulador.ClasificarIncidenteAsync(titulo, descripcion, cancellationToken);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var parsed = JsonSerializer.Deserialize<ClasificacionIaDTO>(rawText, options);

                return parsed != null
                    ? ServiceResult<ClasificacionIaDTO>.Ok(parsed)
                    : await _simulador.ClasificarIncidenteAsync(titulo, descripcion, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("AsistenteGemini: Tiempo de espera de 15 segundos agotado al contactar con la IA.");
                return ServiceResult<ClasificacionIaDTO>.GatewayTimeout(
                    "El servicio de Inteligencia Artificial excedió el tiempo máximo de respuesta de 15 segundos. Puedes ingresar los datos manualmente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AsistenteGemini: Excepción al comunicarse con la API de IA.");
                return await _simulador.ClasificarIncidenteAsync(titulo, descripcion, cancellationToken);
            }
        }

        public async Task<ServiceResult<ResumenIaDTO>> ResumirAsync(
            string contexto,
            string? tipo = "general",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(contexto))
            {
                return ServiceResult<ResumenIaDTO>.BadRequest("El contexto a resumir es requerido.");
            }

            var apiKey = _config["IA:ApiKey"] ?? Environment.GetEnvironmentVariable("IA__ApiKey");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogInformation("AsistenteGemini: No se encontró IA:ApiKey configurada. Utilizando adaptador simulado heurístico.");
                return await _simulador.ResumirAsync(contexto, tipo, cancellationToken);
            }

            try
            {
                var modelo = _config["IA:Modelo"] ?? "gemini-2.5-flash";
                
                var prompt = $@"Eres un analista de ciberseguridad senior de CyberOps.
Genera un resumen ejecutivo formal y conciso en español de los siguientes datos o métricas de incidentes:
{contexto}

Enfócate en: hallazgos clave, activos en riesgo, estado de resolución y recomendaciones para la toma de decisiones.";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.3
                    }
                };

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var response = await EnviarAsync(modelo, apiKey, requestBody, linkedCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AsistenteGemini: Error HTTP {Status} al resumir. Aplicando fallback heurístico.", response.StatusCode);
                    return await _simulador.ResumirAsync(contexto, tipo, cancellationToken);
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: linkedCts.Token);
                var rawText = jsonResponse
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(rawText))
                    return await _simulador.ResumirAsync(contexto, tipo, cancellationToken);

                var resultado = new ResumenIaDTO(
                    Resumen: rawText.Trim(),
                    GeneradoEn: DateTime.UtcNow,
                    Modelo: modelo
                );

                return ServiceResult<ResumenIaDTO>.Ok(resultado);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("AsistenteGemini: Tiempo de espera de 15 segundos agotado al generar resumen con la IA.");
                return ServiceResult<ResumenIaDTO>.GatewayTimeout(
                    "El servicio de Inteligencia Artificial excedió el tiempo límite de 15 segundos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AsistenteGemini: Error al comunicarse con la IA para resumir.");
                return await _simulador.ResumirAsync(contexto, tipo, cancellationToken);
            }
        }
    }
}
