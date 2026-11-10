using Microsoft.EntityFrameworkCore;
using SentryGuard.Common;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Models;
using SentryGuard.Repositories;
using System.Linq.Expressions;

namespace SentryGuard.Services
{
    /// <summary>
    /// Implementación del servicio de lógica de negocio para Incidentes (ADR-002).
    /// Centraliza todas las reglas de negocio, validaciones de permisos, cálculo de SLA y bitácoras.
    /// </summary>
    public class IncidenteService : IIncidenteService
    {
        private readonly IIncidenteRepository _incidenteRepo;
        private readonly IActivoRepository _activoRepo;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly AuthService _authService;
        private readonly SentryGuardDbContext _db;

        private const int TituloMin = 5;
        private const int TituloMax = 250;
        private const int DescripcionMin = 10;
        private const int DescripcionMax = 4000;
        private const int BusquedaMax = 100;
        private const int ComentarioMax = 2000;

        public static readonly Expression<Func<Incidente, IncidenteDTO>> ADto = i => new IncidenteDTO
        {
            Id = i.Id,
            Titulo = i.Titulo,
            Descripcion = i.Descripcion,
            SeveridadId = i.SeveridadId,
            Severidad = i.Severidad!.Nombre,
            EstadoId = i.EstadoId,
            Estado = i.Estado!.Nombre,
            ActivoId = i.ActivoId,
            Activo = i.Activo!.Nombre,
            OrganizacionId = i.OrganizacionId,
            AnalistaAsignadoId = i.AnalistaAsignadoId,
            AnalistaAsignado = i.AnalistaAsignado != null ? i.AnalistaAsignado.Nombre : null,
            FechaCreacion = i.FechaCreacion,
            FechaCierre = i.FechaCierre
        };

        public IncidenteService(
            IIncidenteRepository incidenteRepo,
            IActivoRepository activoRepo,
            IUsuarioRepository usuarioRepo,
            AuthService authService,
            SentryGuardDbContext db)
        {
            _incidenteRepo = incidenteRepo;
            _activoRepo = activoRepo;
            _usuarioRepo = usuarioRepo;
            _authService = authService;
            _db = db;
        }

        public async Task<ServiceResult<IncidenteDTO>> CrearIncidenteAsync(CrearIncidenteDto dto, Usuario usuario)
        {
            var errores = new Dictionary<string, string[]>();
            ValidarTexto(errores, "titulo", dto.Titulo, TituloMin, TituloMax, requerido: true);
            ValidarTexto(errores, "descripcion", dto.Descripcion, DescripcionMin, DescripcionMax, requerido: true);

            if (!await _db.Severidades.AnyAsync(s => s.Id == dto.SeveridadId))
                errores["severidadId"] = ["La severidad seleccionada no existe."];

            var activo = await _activoRepo.GetByIdAsync(dto.ActivoId);
            if (activo == null)
                errores["activoId"] = ["El activo seleccionado no existe."];

            if (errores.Count > 0)
                return ServiceResult<IncidenteDTO>.Validation(errores);

            // OWASP API1: Cliente solo puede asociar incidentes a activos de su propia organización
            if (usuario.Rol?.Nombre == "Cliente" && usuario.OrganizacionId != activo!.OrganizacionId)
                return ServiceResult<IncidenteDTO>.Forbid();

            var estadoAbierto = await _db.EstadosIncidente.FirstOrDefaultAsync(e => e.Nombre == "Abierto");
            var incidente = new Incidente
            {
                OrganizacionId = activo!.OrganizacionId,
                ActivoId = dto.ActivoId,
                Titulo = dto.Titulo!.Trim(),
                Descripcion = dto.Descripcion!.Trim(),
                SeveridadId = dto.SeveridadId,
                EstadoId = estadoAbierto?.Id ?? 1,
                FechaCreacion = DateTime.UtcNow
            };

            await _incidenteRepo.AddAsync(incidente);
            RegistrarEnBitacora(incidente.Id, usuario.Id, "Incidente creado");
            await _db.SaveChangesAsync();

            var dtoResult = await _db.Incidentes.Where(i => i.Id == incidente.Id).Select(ADto).FirstAsync();
            return ServiceResult<IncidenteDTO>.Created(dtoResult);
        }

        public async Task<ServiceResult<IncidenteDTO>> ObtenerIncidenteAsync(int id, Usuario usuario)
        {
            var incidente = await _db.Incidentes.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
            if (incidente == null)
                return ServiceResult<IncidenteDTO>.NotFound();

            if (!_authService.ValidaAccesoIncidente(usuario, incidente))
                return ServiceResult<IncidenteDTO>.Forbid();

            var dto = await _db.Incidentes.Where(i => i.Id == id).Select(ADto).FirstAsync();
            return ServiceResult<IncidenteDTO>.Ok(dto);
        }

        public async Task<ServiceResult<PagedResult<IncidenteDTO>>> ListarIncidentesAsync(
            Usuario usuario, int page, int pageSize, string? q, int? estadoId, int? severidadId, int? activoId)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            IQueryable<Incidente> query = _db.Incidentes;

            // Cliente: solo su organización. Analista: solo incidentes asignados. Administrador/Supervisor: todo.
            if (usuario.Rol?.Nombre == "Cliente")
                query = query.Where(i => i.OrganizacionId == usuario.OrganizacionId);
            else if (usuario.Rol?.Nombre == "Analista")
                query = query.Where(i => i.AnalistaAsignadoId == usuario.Id);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var termino = q.Trim();
                if (termino.Length > BusquedaMax) termino = termino[..BusquedaMax];
                query = query.Where(i => i.Titulo.Contains(termino));
            }
            if (estadoId.HasValue) query = query.Where(i => i.EstadoId == estadoId.Value);
            if (severidadId.HasValue) query = query.Where(i => i.SeveridadId == severidadId.Value);
            if (activoId.HasValue) query = query.Where(i => i.ActivoId == activoId.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(i => i.FechaCreacion)
                .ThenByDescending(i => i.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ADto)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)total / pageSize);
            return ServiceResult<PagedResult<IncidenteDTO>>.Ok(new PagedResult<IncidenteDTO>(total, page, pageSize, totalPages, items));
        }

        public async Task<ServiceResult<bool>> ActualizarIncidenteAsync(int id, ActualizarIncidenteDto dto, Usuario usuario)
        {
            var incidente = await _incidenteRepo.GetByIdAsync(id);
            if (incidente == null)
                return ServiceResult<bool>.NotFound();

            if (!_authService.ValidaAccesoIncidente(usuario, incidente))
                return ServiceResult<bool>.Forbid();

            var errores = new Dictionary<string, string[]>();
            ValidarTexto(errores, "titulo", dto.Titulo, TituloMin, TituloMax, requerido: false);
            ValidarTexto(errores, "descripcion", dto.Descripcion, DescripcionMin, DescripcionMax, requerido: false);

            if (dto.SeveridadId.HasValue && !await _db.Severidades.AnyAsync(s => s.Id == dto.SeveridadId.Value))
                errores["severidadId"] = ["La severidad seleccionada no existe."];

            if (errores.Count > 0)
                return ServiceResult<bool>.Validation(errores);

            if (dto.Titulo is not null) incidente.Titulo = dto.Titulo.Trim();
            if (dto.Descripcion is not null) incidente.Descripcion = dto.Descripcion.Trim();
            if (dto.SeveridadId.HasValue) incidente.SeveridadId = dto.SeveridadId.Value;

            RegistrarEnBitacora(incidente.Id, usuario.Id, "Datos del incidente actualizados");
            await _incidenteRepo.UpdateAsync(incidente);
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> EliminarIncidenteAsync(int id, Usuario usuario)
        {
            if (usuario.Rol?.Nombre != "Administrador")
                return ServiceResult<bool>.Forbid();

            var incidente = await _incidenteRepo.GetByIdAsync(id);
            if (incidente == null)
                return ServiceResult<bool>.NotFound();

            await _incidenteRepo.DeleteAsync(id);
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> AsignarAnalistaAsync(int id, int analistaId, Usuario usuario)
        {
            var incidente = await _incidenteRepo.GetByIdAsync(id);
            if (incidente == null)
                return ServiceResult<bool>.NotFound();

            var analista = await _usuarioRepo.GetWithRolAsync(analistaId);
            if (analista == null || analista.Rol?.Nombre != "Analista" || !analista.Activo)
                return ServiceResult<bool>.BadRequest("El usuario indicado no existe o no tiene rol Analista", "ANALISTA_INVALIDO");

            incidente.AnalistaAsignadoId = analistaId;
            RegistrarEnBitacora(incidente.Id, usuario.Id, "Analista asignado", analista.Nombre);
            await _incidenteRepo.UpdateAsync(incidente);
            return ServiceResult<bool>.Ok(true);
        }

        // Matriz de transiciones permitidas según el flujo de estados (ADR-008)
        private static readonly Dictionary<int, int[]> TransicionesValidas = new()
        {
            [1] = [2, 3, 4, 5], // Abierto -> En progreso, Escalado, Resuelto, Cerrado
            [2] = [3, 4, 5],    // En progreso -> Escalado, Resuelto, Cerrado
            [3] = [2, 4, 5],    // Escalado -> En progreso, Resuelto, Cerrado
            [4] = [2, 5],       // Resuelto -> En progreso (reapertura), Cerrado
            [5] = [1, 2]        // Cerrado -> Abierto o En progreso (reapertura)
        };

        public async Task<ServiceResult<bool>> CambiarEstadoAsync(int id, int nuevoEstadoId, Usuario usuario)
        {
            var incidente = await _incidenteRepo.GetByIdAsync(id);
            if (incidente == null)
                return ServiceResult<bool>.NotFound();

            if (!_authService.ValidaAccesoIncidente(usuario, incidente))
                return ServiceResult<bool>.Forbid();

            var nuevoEstado = await _db.EstadosIncidente.FindAsync(nuevoEstadoId);
            if (nuevoEstado == null)
                return ServiceResult<bool>.BadRequest("Estado no existe", "ESTADO_INEXISTENTE");

            if (incidente.EstadoId == nuevoEstadoId)
                return ServiceResult<bool>.Conflict("El incidente ya se encuentra en ese estado.", "ESTADO_IDENTICO");

            if (TransicionesValidas.TryGetValue(incidente.EstadoId, out var permitidos) && !permitidos.Contains(nuevoEstadoId))
            {
                var estadoOrigen = await _db.EstadosIncidente.Where(e => e.Id == incidente.EstadoId).Select(e => e.Nombre).FirstOrDefaultAsync() ?? $"Estado #{incidente.EstadoId}";
                return ServiceResult<bool>.InvalidTransition($"No se permite la transición desde '{estadoOrigen}' hacia '{nuevoEstado.Nombre}'.");
            }

            var estadoAnterior = await _db.EstadosIncidente.Where(e => e.Id == incidente.EstadoId).Select(e => e.Nombre).FirstOrDefaultAsync();

            incidente.EstadoId = nuevoEstadoId;
            RegistrarEnBitacora(incidente.Id, usuario.Id, "Cambio de estado", $"{estadoAnterior} → {nuevoEstado.Nombre}");

            if (nuevoEstado.Nombre is "Resuelto" or "Cerrado")
                incidente.FechaCierre ??= DateTime.UtcNow;
            else
                incidente.FechaCierre = null;

            await _incidenteRepo.UpdateAsync(incidente);
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<BitacoraDTO>> AgregarBitacoraAsync(int id, string accion, string? comentario, Usuario usuario)
        {
            var incidente = await _incidenteRepo.GetByIdAsync(id);
            if (incidente == null)
                return ServiceResult<BitacoraDTO>.NotFound();

            if (!_authService.ValidaAccesoIncidente(usuario, incidente))
                return ServiceResult<BitacoraDTO>.Forbid();

            if (string.IsNullOrWhiteSpace(accion))
                return ServiceResult<BitacoraDTO>.BadRequest("La acción no puede estar vacía.");

            if (comentario is { Length: > ComentarioMax })
                return ServiceResult<BitacoraDTO>.BadRequest($"El comentario no puede exceder {ComentarioMax} caracteres.");

            var bitacora = new BitacoraIncidente
            {
                IncidenteId = incidente.Id,
                UsuarioId = usuario.Id,
                Accion = accion.Trim(),
                Comentario = comentario?.Trim(),
                Timestamp = DateTime.UtcNow
            };

            _db.BitacorasIncidente.Add(bitacora);
            await _db.SaveChangesAsync();

            var dto = new BitacoraDTO
            {
                Id = bitacora.Id,
                Accion = bitacora.Accion,
                Comentario = bitacora.Comentario,
                Usuario = usuario.Nombre,
                Timestamp = bitacora.Timestamp
            };
            return ServiceResult<BitacoraDTO>.Created(dto);
        }

        public async Task<ServiceResult<IEnumerable<BitacoraDTO>>> ObtenerBitacoraAsync(int id, Usuario usuario)
        {
            var incidente = await _incidenteRepo.GetByIdAsync(id);
            if (incidente == null)
                return ServiceResult<IEnumerable<BitacoraDTO>>.NotFound();

            if (!_authService.ValidaAccesoIncidente(usuario, incidente))
                return ServiceResult<IEnumerable<BitacoraDTO>>.Forbid();

            var bitacoras = await _db.BitacorasIncidente
                .Where(b => b.IncidenteId == id)
                .OrderBy(b => b.Timestamp)
                .Select(b => new BitacoraDTO
                {
                    Id = b.Id,
                    Accion = b.Accion,
                    Comentario = b.Comentario,
                    Usuario = b.Usuario != null ? b.Usuario.Nombre : "Sistema",
                    Timestamp = b.Timestamp
                })
                .ToListAsync();

            return ServiceResult<IEnumerable<BitacoraDTO>>.Ok(bitacoras);
        }

        public async Task<ServiceResult<EvidenciaDTO>> AgregarEvidenciaAsync(int id, string nombreArchivo, string rutaAlmacenamiento, Usuario usuario)
        {
            var incidente = await _incidenteRepo.GetByIdAsync(id);
            if (incidente == null)
                return ServiceResult<EvidenciaDTO>.NotFound();

            if (!_authService.ValidaAccesoIncidente(usuario, incidente))
                return ServiceResult<EvidenciaDTO>.Forbid();

            var errores = new Dictionary<string, string[]>();
            ValidarTexto(errores, "nombreArchivo", nombreArchivo, 3, 200, requerido: true);
            ValidarTexto(errores, "rutaAlmacenamiento", rutaAlmacenamiento, 5, 500, requerido: true);

            if (!EsRutaEvidenciaValida(rutaAlmacenamiento))
                errores["rutaAlmacenamiento"] = ["La ruta debe ser una URL HTTPS absoluta o una ruta relativa válida sin '..' ni prefijo '/'."];

            if (errores.Count > 0)
                return ServiceResult<EvidenciaDTO>.Validation(errores);

            var evidencia = new EvidenciaIncidente
            {
                IncidenteId = incidente.Id,
                NombreArchivo = nombreArchivo.Trim(),
                RutaAlmacenamiento = rutaAlmacenamiento.Trim(),
                SubidoPorId = usuario.Id,
                Fecha = DateTime.UtcNow
            };

            _db.EvidenciasIncidente.Add(evidencia);
            RegistrarEnBitacora(incidente.Id, usuario.Id, "Evidencia agregada", evidencia.NombreArchivo);
            await _db.SaveChangesAsync();

            var dto = new EvidenciaDTO
            {
                Id = evidencia.Id,
                NombreArchivo = evidencia.NombreArchivo,
                RutaAlmacenamiento = evidencia.RutaAlmacenamiento,
                SubidoPor = usuario.Nombre,
                Fecha = evidencia.Fecha
            };
            return ServiceResult<EvidenciaDTO>.Created(dto);
        }

        public async Task<ServiceResult<IEnumerable<EvidenciaDTO>>> ObtenerEvidenciasAsync(int id, Usuario usuario)
        {
            var incidente = await _incidenteRepo.GetByIdAsync(id);
            if (incidente == null)
                return ServiceResult<IEnumerable<EvidenciaDTO>>.NotFound();

            if (!_authService.ValidaAccesoIncidente(usuario, incidente))
                return ServiceResult<IEnumerable<EvidenciaDTO>>.Forbid();

            var evidencias = await _db.EvidenciasIncidente
                .Where(e => e.IncidenteId == id)
                .OrderBy(e => e.Fecha)
                .Select(e => new EvidenciaDTO
                {
                    Id = e.Id,
                    NombreArchivo = e.NombreArchivo,
                    RutaAlmacenamiento = e.RutaAlmacenamiento,
                    SubidoPor = e.SubidoPor != null ? e.SubidoPor.Nombre : "Sistema",
                    Fecha = e.Fecha
                })
                .ToListAsync();

            return ServiceResult<IEnumerable<EvidenciaDTO>>.Ok(evidencias);
        }

        public async Task<ServiceResult<CumplimientoSLADTO>> ConsultarCumplimientoSlaAsync(int id, Usuario usuario)
        {
            var incidente = await _incidenteRepo.GetWithDetallesAsync(id);
            if (incidente == null)
                return ServiceResult<CumplimientoSLADTO>.NotFound();

            if (!_authService.ValidaAccesoIncidente(usuario, incidente))
                return ServiceResult<CumplimientoSLADTO>.Forbid();

            var ahora = DateTime.UtcNow;
            var contratoDetalle = await _db.ContratosSla
                .Where(c => c.OrganizacionId == incidente.OrganizacionId
                    && c.VigenteDesde <= incidente.FechaCreacion
                    && (c.VigenteHasta == null || c.VigenteHasta >= incidente.FechaCreacion))
                .Join(_db.ContratosSlaDetalle, c => c.Id, d => d.ContratoId, (c, d) => d)
                .Where(d => d.SeveridadId == incidente.SeveridadId)
                .Select(d => (decimal?)d.HorasSla)
                .FirstOrDefaultAsync();

            var horasLimite = contratoDetalle ?? incidente.Severidad?.HorasSla ?? 72m;
            var fechaTermino = incidente.FechaCierre ?? ahora;
            var horasTranscurridas = (fechaTermino - incidente.FechaCreacion).TotalHours;
            var horasLimiteDbl = (double)horasLimite;
            var cumpleSla = horasTranscurridas <= horasLimiteDbl;
            var porcentaje = horasLimiteDbl > 0 ? (horasTranscurridas / horasLimiteDbl) * 100 : 0;

            var dto = new CumplimientoSLADTO
            {
                IncidenteId = incidente.Id,
                Titulo = incidente.Titulo,
                Estado = incidente.Estado?.Nombre ?? "Desconocido",
                HorasTranscurridas = Math.Round(horasTranscurridas, 2),
                HorasSLAEstablecidas = horasLimiteDbl,
                CumpleSLA = cumpleSla,
                PorcentajeUtilizacion = Math.Round(porcentaje, 2)
            };
            return ServiceResult<CumplimientoSLADTO>.Ok(dto);
        }

        private void RegistrarEnBitacora(int incidenteId, int usuarioId, string accion, string? comentario = null)
        {
            _db.BitacorasIncidente.Add(new BitacoraIncidente
            {
                IncidenteId = incidenteId,
                UsuarioId = usuarioId,
                Accion = accion,
                Comentario = comentario,
                Timestamp = DateTime.UtcNow
            });
        }

        private static void ValidarTexto(Dictionary<string, string[]> errores, string campo, string? valor, int min, int max, bool requerido)
        {
            if (valor is null || (!requerido && min == 0 && valor.Trim().Length == 0))
            {
                if (requerido) errores[campo] = [$"El campo {campo} es obligatorio."];
                return;
            }

            var largo = valor.Trim().Length;
            if (largo < min || largo > max)
                errores[campo] = [$"Debe tener entre {min} y {max} caracteres."];
        }

        private static bool EsRutaEvidenciaValida(string ruta)
        {
            if (Uri.TryCreate(ruta, UriKind.Absolute, out var uri))
                return uri.Scheme == Uri.UriSchemeHttps;
            return !ruta.StartsWith('/') && !ruta.Contains("..") && ruta.All(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' or '/');
        }
    }
}
