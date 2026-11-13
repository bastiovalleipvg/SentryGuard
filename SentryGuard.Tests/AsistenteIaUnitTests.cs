using SentryGuard.Services;
using System.Threading.Tasks;
using Xunit;

namespace SentryGuard.Tests
{
    /// <summary>
    /// Pruebas unitarias para el módulo de Inteligencia Artificial (RF-23 / RF-24 / ADR-007).
    /// </summary>
    public class AsistenteIaUnitTests
    {
        private readonly IAsistenteIA _asistente = new AsistenteSimulado();

        [Fact]
        public async Task ClasificarIncidente_RansomwareOC2_SugiereSeveridadCritica()
        {
            // Arrange
            var titulo = "Detección de artefacto LockBit y conexión a C2";
            var descripcion = "Se detectó ejecutable anómalo intentando cifrar volumen y balizaje a servidor de comando y control.";

            // Act
            var result = await _asistente.ClasificarIncidenteAsync(titulo, descripcion);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(4, result.Data.SeveridadSugeridaId); // Crítica
            Assert.Equal("Crítica", result.Data.SeveridadNombre);
            Assert.NotEmpty(result.Data.AccionesRecomendadas);
            Assert.Contains(result.Data.AccionesRecomendadas, a => a.Contains("Aislar"));
        }

        [Fact]
        public async Task ClasificarIncidente_InyeccionSqlOWebshell_SugiereSeveridadAlta()
        {
            // Arrange
            var titulo = "Intento de Inyección SQL detectado en WAF";
            var descripcion = "Múltiples peticiones HTTP con payloads de inyección SQL y sospecha de webshell en portal web.";

            // Act
            var result = await _asistente.ClasificarIncidenteAsync(titulo, descripcion);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(3, result.Data.SeveridadSugeridaId); // Alta
            Assert.Equal("Alta", result.Data.SeveridadNombre);
            Assert.NotEmpty(result.Data.AccionesRecomendadas);
        }

        [Fact]
        public async Task ClasificarIncidente_CampanaPhishing_SugiereSeveridadMedia()
        {
            // Arrange
            var titulo = "Campaña de phishing masivo a empleados";
            var descripcion = "Correos sospechosos solicitando cambio de credenciales de Office 365.";

            // Act
            var result = await _asistente.ClasificarIncidenteAsync(titulo, descripcion);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.SeveridadSugeridaId); // Media
            Assert.Equal("Media", result.Data.SeveridadNombre);
        }

        [Theory]
        [InlineData("", "Descripción válida")]
        [InlineData("Título válido", "")]
        [InlineData("   ", "   ")]
        public async Task ClasificarIncidente_DatosVacios_RetornaBadRequest(string titulo, string descripcion)
        {
            // Act
            var result = await _asistente.ClasificarIncidenteAsync(titulo, descripcion);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task Resumir_ContextoValido_GeneraResumenEjecutivo()
        {
            // Arrange
            var contexto = "Total incidentes: 45. Críticos: 2. Resueltos en SLA: 42. Activos afectados: Servidor Web, Firewall.";

            // Act
            var result = await _asistente.ResumirAsync(contexto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data.Resumen);
            Assert.Contains("CyberOps", result.Data.Resumen);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Resumir_ContextoVacio_RetornaBadRequest(string contexto)
        {
            // Act
            var result = await _asistente.ResumirAsync(contexto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
        }
    }
}
