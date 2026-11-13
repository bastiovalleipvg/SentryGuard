using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentryGuard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SentryGuard.Data
{
    /// <summary>
    /// Generador de datos sintéticos con semilla fija e idempotente (ADR-009 / RF-25 / RNF-01).
    /// Genera al menos 500 incidentes coherentes distribuidos a lo largo de 12 meses históricos,
    /// incluyendo bitácoras, evidencias, asignaciones de analistas y cumplimiento de SLA.
    /// </summary>
    public class GeneradorDatosSinteticos
    {
        private readonly SentryGuardDbContext _db;
        private readonly ILogger<GeneradorDatosSinteticos> _logger;

        private const int TotalIncidentesMinimos = 500;
        private const int TotalIncidentesObjetivo = 520;
        private const int SemillaFija = 42; // Semilla determinística para reproducibilidad de indicadores

        public GeneradorDatosSinteticos(SentryGuardDbContext db, ILogger<GeneradorDatosSinteticos> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<int> GenerarAsync()
        {
            var totalActual = await _db.Incidentes.CountAsync();
            if (totalActual >= TotalIncidentesMinimos)
            {
                _logger.LogInformation("GeneradorDatosSinteticos: La base de datos ya cuenta con {Total} incidentes (>= {Minimo}). Omitiendo ejecución (Idempotente).", totalActual, TotalIncidentesMinimos);
                return totalActual;
            }

            _logger.LogInformation("GeneradorDatosSinteticos: Iniciando generación de {Objetivo} incidentes sintéticos...", TotalIncidentesObjetivo);

            // 1. Asegurar organizaciones adicionales si no existen
            await AsegurarOrganizacionesYActivosAsync();

            // 2. Asegurar analistas adicionales si no existen
            await AsegurarAnalistasAdicionalesAsync();

            // 3. Cargar datos maestros en memoria
            var organizaciones = await _db.Organizaciones.Include(o => o.Activos).ToListAsync();
            var analistas = await _db.Usuarios.Where(u => u.RolId == 3 && u.Activo).ToListAsync(); // RolId 3 = Analista
            var severidades = await _db.Severidades.ToListAsync();
            var estados = await _db.EstadosIncidente.ToListAsync();
            var adminUser = await _db.Usuarios.FirstOrDefaultAsync(u => u.RolId == 1) ?? analistas.First();

            var rnd = new Random(SemillaFija);
            var fechaBaseFin = DateTime.UtcNow;
            var fechaBaseInicio = fechaBaseFin.AddDays(-365); // 12 meses hacia atrás

            var bancoTitulos = new[]
            {
                // Phishing / Credenciales
                ("Intento masivo de phishing dirigido a ejecutivos", "Campaña de correos con enlaces maliciosos simulando portal corporativo de Microsoft 365."),
                ("Compromiso de credenciales corporativas en leak público", "Credenciales de colaboradores detectadas en base de datos filtrada en la Dark Web."),
                ("Ataque de fuerza bruta contra portal VPN corporativo", "Múltiples intentos fallidos de autenticación desde direcciones IP geográficamente anómalas."),
                ("Detección de robo de token de sesión OAuth", "Token de acceso emitido con uso anómalo sin desafío de MFA."),
                ("Phishing telefónico (Vishing) a mesa de ayuda", "Llamadas de ingeniería social intentando resetear contraseñas de cuentas con privilegios."),

                // Malware / Ransomware
                ("Detección de artefacto malicioso LockBit en endpoint", "Alerta del EDR por ejecutable sospechoso intentando deshabilitar Volume Shadow Copies."),
                ("Inyección de script malicioso en servidor web", "Detección de webshell PHP en directorio público tras explotación de vulnerabilidad web."),
                ("Ejecución de PowerShell con comandos ofuscados", "Proceso de PowerShell invocado con parámetros en base64 y descarga remota de payload."),
                ("Intento de movimiento lateral mediante Mimikatz", "Alerta de inyección en proceso LSASS detectada en controlador de dominio."),
                ("Detección de troyano de acceso remoto (RAT)", "Conexión HTTPS saliente persistente hacia infraestructura clasificada como Cobalt Strike C2."),

                // Red / Perímetro / DDoS
                ("Ataque de denegación de servicio distribuido (DDoS SYN Flood)", "Saturación volumétrica en interfaces perimetrales superando los 10 Gbps."),
                ("Escaneo masivo de puertos desde IP en lista negra", "Tráfico TCP SYN anómalo buscando puertos 22, 3389 y 445 en la subred pública."),
                ("Exfiltración potencial de datos por túnel DNS", "Consultas DNS anómalas con payloads binarios hacia dominio dinámico."),
                ("Tráfico no autorizado entre zonas de seguridad (VLAN Hopping)", "Paquetes etiquetados intentando cruzar desde segmento DMZ a LAN interna."),
                ("Detección de comunicación con botnet Mirai", "Dispositivo IoT perimetral emitiendo paquetes hacia nodos de comando conocidos."),

                // Aplicación / Datos
                ("Intento de inyección SQL en endpoint de autenticación", "Cargas útiles con payloads ' OR 1=1-- detectadas en WAF en formulario de login."),
                ("Acceso anómalo y extracción de registros de base de datos", "Consulta masiva de tablas de clientes ejecutada fuera de los umbrales normales."),
                ("Manipulación de parámetros IDOR en portal de clientes", "Peticiones HTTP consecutivas variando el identificador numérico de cliente."),
                ("Intento de Cross-Site Scripting (XSS) almacenado", "Script inyectado en campos de comentarios de tickets de soporte."),
                ("Modificación no autorizada de configuración en Active Directory", "Adición de cuenta de servicio al grupo Domain Admins sin ticket de cambio aprobado.")
            };

            // Categoría de incidente (tb_CategoriaIncidente) de cada plantilla, en el mismo orden que bancoTitulos:
            // 1 Phishing, 2 Malware, 3 Acceso no autorizado, 4 Denegación de servicio, 5 Fuga de datos, 6 Otro
            var categoriaPorPlantilla = new[] { 1, 3, 3, 3, 1, 2, 2, 2, 3, 2, 4, 3, 5, 3, 2, 3, 5, 3, 3, 3 };

            var incidentesParaInsertar = new List<Incidente>();
            var bitacorasParaInsertar = new List<BitacoraIncidente>();
            var evidenciasParaInsertar = new List<EvidenciaIncidente>();

            var totalIncidentes = TotalIncidentesObjetivo - totalActual;

            for (int i = 0; i < totalIncidentes; i++)
            {
                // Distribución temporal en los últimos 365 días (12 meses)
                var progreso = (double)i / totalIncidentes;
                var offsetDias = progreso * 365.0;
                var fechaCreacion = fechaBaseInicio.AddDays(offsetDias)
                    .AddHours(rnd.Next(0, 24))
                    .AddMinutes(rnd.Next(0, 60))
                    .AddSeconds(rnd.Next(0, 60));

                if (fechaCreacion > fechaBaseFin)
                    fechaCreacion = fechaBaseFin.AddMinutes(-rnd.Next(5, 120));

                // Seleccionar organización y activo correspondiente
                var org = organizaciones[rnd.Next(organizaciones.Count)];
                var activosOrg = org.Activos?.ToList() ?? new List<Activo>();
                var activo = activosOrg.Count > 0 ? activosOrg[rnd.Next(activosOrg.Count)] : (await _db.Activos.FirstAsync());

                // Plantilla de título y descripción
                var indicePlantilla = rnd.Next(bancoTitulos.Length);
                var (tituloBase, descBase) = bancoTitulos[indicePlantilla];
                var titulo = $"{tituloBase} #{totalActual + i + 1}";
                var descripcion = $"{descBase} Afecta a {activo.Nombre} ({activo.HostnameIp ?? "Sin IP"}). Registro generado en auditoría CyberOps.";

                // Severidad (distribución realista: Media 40%, Baja 30%, Alta 20%, Crítica 10%)
                int severidadId;
                var randSev = rnd.Next(100);
                if (randSev < 30) severidadId = 1;      // Baja (24h)
                else if (randSev < 70) severidadId = 2; // Media (12h)
                else if (randSev < 90) severidadId = 3; // Alta (4h)
                else severidadId = 4;                   // Crítica (1h)

                var severidad = severidades.First(s => s.Id == severidadId);
                var horasSla = (double)severidad.HorasSla;

                // Estado según antigüedad
                int estadoId;
                DateTime? fechaCierre = null;
                var diasAntiguedad = (fechaBaseFin - fechaCreacion).TotalDays;

                if (diasAntiguedad > 45)
                {
                    // Incidentes antiguos: 85% Cerrados, 15% Resueltos
                    estadoId = rnd.Next(100) < 85 ? 5 : 4;
                }
                else if (diasAntiguedad > 15)
                {
                    // Último mes y medio: mezcla de Cerrado (50%), Resuelto (25%), En progreso (15%), Escalado (10%)
                    var roll = rnd.Next(100);
                    if (roll < 50) estadoId = 5;
                    else if (roll < 75) estadoId = 4;
                    else if (roll < 90) estadoId = 2;
                    else estadoId = 3;
                }
                else
                {
                    // Últimos 15 días: Abiertos (25%), En progreso (35%), Escalado (15%), Resueltos (15%), Cerrados (10%)
                    var roll = rnd.Next(100);
                    if (roll < 25) estadoId = 1;
                    else if (roll < 60) estadoId = 2;
                    else if (roll < 75) estadoId = 3;
                    else if (roll < 90) estadoId = 4;
                    else estadoId = 5;
                }

                // Analista asignado (todos excepto algunos Abiertos)
                int? analistaAsignadoId = null;
                Usuario? analistaAsignado = null;
                if (estadoId != 1 || rnd.Next(100) < 50)
                {
                    analistaAsignado = analistas[rnd.Next(analistas.Count)];
                    analistaAsignadoId = analistaAsignado.Id;
                }

                // Cálculo de FechaCierre y cumplimiento SLA si está Resuelto o Cerrado
                if (estadoId is 4 or 5)
                {
                    // 85% cumple SLA (duración < horasSla), 15% excede SLA
                    double duracionHoras;
                    if (rnd.Next(100) < 85)
                    {
                        duracionHoras = Math.Max(0.2, horasSla * (0.2 + rnd.NextDouble() * 0.75));
                    }
                    else
                    {
                        duracionHoras = horasSla * (1.1 + rnd.NextDouble() * 1.5);
                    }

                    fechaCierre = fechaCreacion.AddHours(duracionHoras);
                    if (fechaCierre > fechaBaseFin) fechaCierre = fechaBaseFin.AddMinutes(-5);
                    if (fechaCierre < fechaCreacion) fechaCierre = fechaCreacion.AddMinutes(1); // CK_tb_Incidente_FechaCierre
                }

                var incidente = new Incidente
                {
                    OrganizacionId = org.Id,
                    ActivoId = activo.Id,
                    Titulo = titulo,
                    Descripcion = descripcion,
                    SeveridadId = severidadId,
                    EstadoId = estadoId,
                    AnalistaAsignadoId = analistaAsignadoId,
                    FechaCreacion = fechaCreacion,
                    FechaCierre = fechaCierre,
                    CategoriaId = categoriaPorPlantilla[indicePlantilla]
                };

                incidentesParaInsertar.Add(incidente);
            }

            // Insertamos los incidentes primero para obtener sus IDs autogenerados
            _logger.LogInformation("GeneradorDatosSinteticos: Guardando {Count} incidentes en la base de datos...", incidentesParaInsertar.Count);
            await _db.Incidentes.AddRangeAsync(incidentesParaInsertar);
            await _db.SaveChangesAsync();

            // Construir bitácoras y evidencias coherentes con los IDs reales
            foreach (var inc in incidentesParaInsertar)
            {
                var analistaId = inc.AnalistaAsignadoId ?? adminUser.Id;

                // 1. Bitácora de creación
                bitacorasParaInsertar.Add(new BitacoraIncidente
                {
                    IncidenteId = inc.Id,
                    UsuarioId = adminUser.Id,
                    Accion = "Incidente registrado",
                    Comentario = "Evento detectado e ingestador en plataforma SOC SentryGuard.",
                    Timestamp = inc.FechaCreacion
                });

                // 2. Bitácora de asignación
                if (inc.AnalistaAsignadoId.HasValue)
                {
                    bitacorasParaInsertar.Add(new BitacoraIncidente
                    {
                        IncidenteId = inc.Id,
                        UsuarioId = adminUser.Id,
                        Accion = "Analista asignado",
                        Comentario = "Asignado para triaje, contención y análisis de causa raíz.",
                        Timestamp = inc.FechaCreacion.AddMinutes(rnd.Next(5, 25))
                    });
                }

                // 3. Bitácoras según estado
                if (inc.EstadoId is 2 or 3 or 4 or 5)
                {
                    bitacorasParaInsertar.Add(new BitacoraIncidente
                    {
                        IncidenteId = inc.Id,
                        UsuarioId = analistaId,
                        Accion = "Contención y mitigación",
                        Comentario = "Aislamiento preventivo del host y recolección de artefactos forenses de memoria y red.",
                        Timestamp = inc.FechaCreacion.AddMinutes(rnd.Next(30, 90))
                    });
                }

                if (inc.EstadoId is 3)
                {
                    bitacorasParaInsertar.Add(new BitacoraIncidente
                    {
                        IncidenteId = inc.Id,
                        UsuarioId = analistaId,
                        Accion = "Escalamiento a SOC L3",
                        Comentario = "Complejidad técnica elevada o persistencia detectada. Se escala a ingeniería de seguridad.",
                        Timestamp = inc.FechaCreacion.AddHours(rnd.Next(1, 3))
                    });
                }

                if (inc.FechaCierre.HasValue)
                {
                    bitacorasParaInsertar.Add(new BitacoraIncidente
                    {
                        IncidenteId = inc.Id,
                        UsuarioId = analistaId,
                        Accion = inc.EstadoId == 5 ? "Incidente cerrado" : "Incidente resuelto",
                        Comentario = "Mitigación validada, amenazas erradicadas y servicio restituido a la normalidad.",
                        Timestamp = inc.FechaCierre.Value
                    });
                }

                // 4. Evidencia (~35% de los incidentes)
                if (rnd.Next(100) < 35)
                {
                    var tiposArchivos = new[]
                    {
                        ("captura_trafico_pcap.pcap", "https://evidencias.sentryguard.cl/pcaps/"),
                        ("analisis_memoria_volatility.txt", "https://evidencias.sentryguard.cl/dumps/"),
                        ("alertas_correlacionadas_siem.json", "https://evidencias.sentryguard.cl/siem/"),
                        ("hash_muestras_virustotal.csv", "https://evidencias.sentryguard.cl/iocs/")
                    };
                    var (nombreArchivo, baseUrl) = tiposArchivos[rnd.Next(tiposArchivos.Length)];

                    evidenciasParaInsertar.Add(new EvidenciaIncidente
                    {
                        IncidenteId = inc.Id,
                        NombreArchivo = nombreArchivo,
                        RutaAlmacenamiento = $"{baseUrl}inc_{inc.Id}_{nombreArchivo}",
                        SubidoPorId = analistaId,
                        Fecha = inc.FechaCreacion.AddMinutes(rnd.Next(20, 120))
                    });
                }
            }

            _logger.LogInformation("GeneradorDatosSinteticos: Guardando {Bitacoras} bitácoras y {Evidencias} evidencias...", bitacorasParaInsertar.Count, evidenciasParaInsertar.Count);
            await _db.BitacorasIncidente.AddRangeAsync(bitacorasParaInsertar);
            await _db.EvidenciasIncidente.AddRangeAsync(evidenciasParaInsertar);
            await _db.SaveChangesAsync();

            var totalFinal = await _db.Incidentes.CountAsync();
            _logger.LogInformation("GeneradorDatosSinteticos: Proceso completado exitosamente. Total incidentes en BD: {TotalFinal}.", totalFinal);
            return totalFinal;
        }

        private async Task AsegurarOrganizacionesYActivosAsync()
        {
            var orgsExistentes = await _db.Organizaciones.CountAsync();
            if (orgsExistentes >= 4) return;

            var nuevasOrgs = new List<Organizacion>();

            if (!await _db.Organizaciones.AnyAsync(o => o.Rut == "77234567-8"))
            {
                nuevasOrgs.Add(new Organizacion
                {
                    RazonSocial = "Banco del Sur SpA",
                    Rut = "77234567-8",
                    PlanContratadoId = 3, // Premium
                    FechaAlta = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc)
                });
            }

            if (!await _db.Organizaciones.AnyAsync(o => o.Rut == "78345678-9"))
            {
                nuevasOrgs.Add(new Organizacion
                {
                    RazonSocial = "Clínica RedSalud Integral",
                    Rut = "78345678-9",
                    PlanContratadoId = 2, // Estándar
                    FechaAlta = new DateTime(2025, 11, 15, 0, 0, 0, DateTimeKind.Utc)
                });
            }

            if (!await _db.Organizaciones.AnyAsync(o => o.Rut == "79456789-0"))
            {
                nuevasOrgs.Add(new Organizacion
                {
                    RazonSocial = "Retail Express Logística",
                    Rut = "79456789-0",
                    PlanContratadoId = 2, // Estándar
                    FechaAlta = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc)
                });
            }

            if (nuevasOrgs.Count > 0)
            {
                await _db.Organizaciones.AddRangeAsync(nuevasOrgs);
                await _db.SaveChangesAsync();
            }

            // Activos para cada organización nueva
            var todosOrgs = await _db.Organizaciones.Include(o => o.Activos).ToListAsync();
            var nuevosActivos = new List<Activo>();

            foreach (var org in todosOrgs)
            {
                if (org.Activos != null && org.Activos.Count >= 2) continue;

                if (org.RazonSocial.Contains("Banco"))
                {
                    nuevosActivos.Add(new Activo { OrganizacionId = org.Id, Nombre = "Core Bancario Transaccional", HostnameIp = "10.20.0.5", CategoriaId = 1 });
                    nuevosActivos.Add(new Activo { OrganizacionId = org.Id, Nombre = "API Gateway OpenBanking", HostnameIp = "api.bancodelsur.cl", CategoriaId = 4 });
                    nuevosActivos.Add(new Activo { OrganizacionId = org.Id, Nombre = "Active Directory DC01", HostnameIp = "10.20.0.10", CategoriaId = 1 });
                }
                else if (org.RazonSocial.Contains("Salud"))
                {
                    nuevosActivos.Add(new Activo { OrganizacionId = org.Id, Nombre = "Servidor PACS Imagenología", HostnameIp = "10.30.0.15", CategoriaId = 1 });
                    nuevosActivos.Add(new Activo { OrganizacionId = org.Id, Nombre = "Ficha Clínica Electrónica", HostnameIp = "ficha.redsalud.cl", CategoriaId = 4 });
                    nuevosActivos.Add(new Activo { OrganizacionId = org.Id, Nombre = "Firewall Perimetral Hospitalario", HostnameIp = "10.30.0.1", CategoriaId = 3 });
                }
                else if (org.RazonSocial.Contains("Retail"))
                {
                    nuevosActivos.Add(new Activo { OrganizacionId = org.Id, Nombre = "Cluster eCommerce Magento", HostnameIp = "shop.retailexpress.cl", CategoriaId = 4 });
                    nuevosActivos.Add(new Activo { OrganizacionId = org.Id, Nombre = "Controlador POS Sucursales", HostnameIp = "10.40.1.2", CategoriaId = 2 });
                    nuevosActivos.Add(new Activo { OrganizacionId = org.Id, Nombre = "Concentrador VPN Bodegas", HostnameIp = "vpn.retailexpress.cl", CategoriaId = 3 });
                }
            }

            if (nuevosActivos.Count > 0)
            {
                await _db.Activos.AddRangeAsync(nuevosActivos);
                await _db.SaveChangesAsync();
            }
        }

        private async Task AsegurarAnalistasAdicionalesAsync()
        {
            var analistasCount = await _db.Usuarios.CountAsync(u => u.RolId == 3);
            if (analistasCount >= 3) return;

            // Password prehasheado para "Admin123!"
            const string passwordHash = "AQAAAAIAAYagAAAAEA7tnp5gva8xrGxWDTthC+lZOlvP37Z6w0rYJiGbxYzLWfHz5fvYXt7Usi6skqdweQ==";

            var nuevosAnalistas = new List<Usuario>();

            if (!await _db.Usuarios.AnyAsync(u => u.Email == "carlos.mendoza@sentryguard.cl"))
            {
                nuevosAnalistas.Add(new Usuario
                {
                    Nombre = "Carlos Mendoza (SOC L1)",
                    Email = "carlos.mendoza@sentryguard.cl",
                    PasswordHash = passwordHash,
                    RolId = 3,
                    Activo = true
                });
            }

            if (!await _db.Usuarios.AnyAsync(u => u.Email == "valeria.lagos@sentryguard.cl"))
            {
                nuevosAnalistas.Add(new Usuario
                {
                    Nombre = "Valeria Lagos (SOC L2)",
                    Email = "valeria.lagos@sentryguard.cl",
                    PasswordHash = passwordHash,
                    RolId = 3,
                    Activo = true
                });
            }

            if (nuevosAnalistas.Count > 0)
            {
                await _db.Usuarios.AddRangeAsync(nuevosAnalistas);
                await _db.SaveChangesAsync();
            }
        }
    }
}

