using SentryGuard.Common;
using SentryGuard.DTOs;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SentryGuard.Services
{
    /// <summary>
    /// Implementación simulada / heurística del asistente de IA (ADR-007).
    /// Garantiza reproducibilidad en pruebas, ejecución en CI/CD sin consumo de tokens y fallback cuando no hay API Key.
    /// </summary>
    public class AsistenteSimulado : IAsistenteIA
    {
        public Task<ServiceResult<ClasificacionIaDTO>> ClasificarIncidenteAsync(
            string titulo,
            string descripcion,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(descripcion))
            {
                return Task.FromResult(ServiceResult<ClasificacionIaDTO>.BadRequest(
                    "El título y la descripción son requeridos para la clasificación con IA."));
            }

            var texto = $"{titulo} {descripcion}".ToLowerInvariant();

            int severidadId;
            string severidadNombre;
            string categoria;
            string justificacion;
            List<string> acciones;

            if (Regex.IsMatch(texto, @"\b(ransomware|lockbit|c2|command and control|fuga masiva|ddos|exfiltraci[oó]n|dc01|controlador de dominio)\b"))
            {
                severidadId = 4; // Crítica (1h SLA)
                severidadNombre = "Crítica";
                categoria = "Malware";
                justificacion = "Se detectaron indicadores de compromiso críticos (amenaza de ransomware, comando y control o impacto directo a la infraestructura núcleo). Requiere aislamiento inmediato.";
                acciones = new List<string>
                {
                    "Aislar de inmediato el endpoint o segmento de red afectado para frenar el movimiento lateral.",
                    "Preservar evidencia forense en caliente (volcado de memoria RAM y captura de tráfico PCAP).",
                    "Notificar al CISO y activar el protocolo de respuesta ante incidentes críticos (CSIRT)."
                };
            }
            else if (Regex.IsMatch(texto, @"\b(inyecci[oó]n sql|sqli|webshell|elevaci[oó]n de privilegios|mimikatz|pass-the-cookie|acceso no autorizado)\b"))
            {
                severidadId = 3; // Alta (4h SLA)
                severidadNombre = "Alta";
                categoria = "Acceso no autorizado";
                justificacion = "Evidencia de explotación de vulnerabilidades de aplicación o intento de escalamiento de privilegios en el perímetro.";
                acciones = new List<string>
                {
                    "Revocar tokens de sesión y forzar reseteo de credenciales de las cuentas comprometidas.",
                    "Bloquear direcciones IP de origen sospechosas en el Firewall / WAF.",
                    "Revisar y parchar el vector de entrada vulnerable en el código o servicio expuesto."
                };
            }
            else if (Regex.IsMatch(texto, @"\b(phishing|correo sospechoso|fuerza bruta|vishing|credenciales)\b"))
            {
                severidadId = 2; // Media (12h SLA)
                severidadNombre = "Media";
                categoria = "Phishing";
                justificacion = "Intento de recolección de credenciales o ataque distribuido de autenticación que no ha comprometido infraestructura central.";
                acciones = new List<string>
                {
                    "Purgar los correos maliciosos de los buzones de los usuarios afectados en el gateway de correo.",
                    "Verificar si algún colaborador ingresó credenciales y activar segundo factor de autenticación (MFA).",
                    "Bloquear el dominio y URL maliciosa en el DNS sinkhole corporativo."
                };
            }
            else
            {
                severidadId = 1; // Baja (24h SLA)
                severidadNombre = "Baja";
                categoria = "Otro";
                justificacion = "Evento de telemetría de bajo riesgo o escaneo rutinario sin afectación de servicios ni compromiso confirmado.";
                acciones = new List<string>
                {
                    "Registrar las IPs escaneadoras en lista de monitoreo pasivo del SIEM.",
                    "Verificar que las políticas de filtrado perimetral descarten el tráfico anómalo sin saturar recursos."
                };
            }

            var resultado = new ClasificacionIaDTO(
                SeveridadSugeridaId: severidadId,
                SeveridadNombre: severidadNombre,
                CategoriaSugerida: categoria,
                Justificacion: justificacion,
                AccionesRecomendadas: acciones,
                Origen: "simulado"
            );

            return Task.FromResult(ServiceResult<ClasificacionIaDTO>.Ok(resultado));
        }

        public Task<ServiceResult<ResumenIaDTO>> ResumirAsync(
            string contexto,
            string? tipo = "general",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(contexto))
            {
                return Task.FromResult(ServiceResult<ResumenIaDTO>.BadRequest(
                    "El contexto a resumir no puede estar vacío."));
            }

            var resumen = $"[Resumen Ejecutivo CyberOps - Asistente IA]\n\n" +
                          $"Se analizó la telemetría e información operacional provista ({contexto.Length} caracteres analizados).\n" +
                          $"• Estado general: El volumen de alertas se mantiene dentro de los rangos de contención previstos.\n" +
                          $"• Principales focos: Es prioritario mantener el aislamiento de activos clasificados con severidad Alta o Crítica y verificar el cumplimiento de SLA del 85%.\n" +
                          $"• Recomendación de gestión: Continuar la rotación de credenciales y la auditoría de reglas de firewall perimetral.";

            var resultado = new ResumenIaDTO(
                Resumen: resumen,
                GeneradoEn: DateTime.UtcNow,
                Modelo: "AsistenteSimulado-Heuristico-v1"
            );

            return Task.FromResult(ServiceResult<ResumenIaDTO>.Ok(resultado));
        }
    }
}
