using Microsoft.EntityFrameworkCore;
using SentryGuard.Common;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Endpoints;
using SentryGuard.Models;
using SentryGuard.Repositories;
using System.Linq.Expressions;

namespace SentryGuard.Services
{
    /// <summary>
    /// Implementación del servicio de lógica de negocio para Activos (ADR-002).
    /// </summary>
    public class ActivoService : IActivoService
    {
        private readonly IActivoRepository _activoRepo;
        private readonly SentryGuardDbContext _db;

        private const int NombreMin = 3;
        private const int NombreMax = 150;
        private const int BusquedaMax = 100;

        public static readonly Expression<Func<Activo, ActivoDTO>> ADto = a => new ActivoDTO
        {
            Id = a.Id,
            Nombre = a.Nombre,
            HostnameIp = a.HostnameIp,
            CategoriaId = a.CategoriaId,
            Categoria = a.Categoria!.Nombre,
            OrganizacionId = a.OrganizacionId,
            Organizacion = a.Organizacion!.RazonSocial
        };

        public ActivoService(IActivoRepository activoRepo, SentryGuardDbContext db)
        {
            _activoRepo = activoRepo;
            _db = db;
        }

        public async Task<ServiceResult<PagedResult<ActivoDTO>>> ListarAsync(
            Usuario usuario, int page, int pageSize, string? q, int? categoriaId, int? organizacionId)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            IQueryable<Activo> query = _db.Activos;
            if (usuario.Rol?.Nombre == "Cliente")
                query = query.Where(a => a.OrganizacionId == usuario.OrganizacionId);
            else if (usuario.Rol?.Nombre == "Analista")
                query = query.Where(a => _db.Incidentes.Any(i => i.ActivoId == a.Id && i.AnalistaAsignadoId == usuario.Id));
            else if (organizacionId.HasValue)
                query = query.Where(a => a.OrganizacionId == organizacionId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var termino = q.Trim();
                if (termino.Length > BusquedaMax) termino = termino[..BusquedaMax];
                query = query.Where(a => a.Nombre.Contains(termino) || (a.HostnameIp != null && a.HostnameIp.Contains(termino)));
            }
            if (categoriaId.HasValue) query = query.Where(a => a.CategoriaId == categoriaId.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(a => a.Nombre)
                .ThenBy(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ADto)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)total / pageSize);
            return ServiceResult<PagedResult<ActivoDTO>>.Ok(new PagedResult<ActivoDTO>(total, page, pageSize, totalPages, items));
        }

        public async Task<ServiceResult<ActivoDTO>> ObtenerAsync(int id, Usuario usuario)
        {
            var activo = await _db.Activos.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (activo == null)
                return ServiceResult<ActivoDTO>.NotFound();

            if (usuario.Rol?.Nombre == "Cliente" && usuario.OrganizacionId != activo.OrganizacionId)
                return ServiceResult<ActivoDTO>.Forbid();
            if (usuario.Rol?.Nombre == "Analista" && !await _db.Incidentes.AnyAsync(i => i.ActivoId == id && i.AnalistaAsignadoId == usuario.Id))
                return ServiceResult<ActivoDTO>.Forbid();

            var dto = await _db.Activos.Where(a => a.Id == id).Select(ADto).FirstAsync();
            return ServiceResult<ActivoDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ActivoDTO>> CrearAsync(CrearActivoDto dto)
        {
            var errores = new Dictionary<string, string[]>();
            await ValidarAsync(errores, dto.Nombre, dto.HostnameIp, dto.CategoriaId);
            if (!await _db.Organizaciones.AnyAsync(o => o.Id == dto.OrganizacionId))
                errores["organizacionId"] = ["La organización seleccionada no existe."];

            if (errores.Count > 0)
                return ServiceResult<ActivoDTO>.Validation(errores);

            var activo = new Activo
            {
                OrganizacionId = dto.OrganizacionId,
                Nombre = dto.Nombre!.Trim(),
                HostnameIp = string.IsNullOrWhiteSpace(dto.HostnameIp) ? null : dto.HostnameIp.Trim(),
                CategoriaId = dto.CategoriaId
            };
            await _activoRepo.AddAsync(activo);

            var dtoResult = await _db.Activos.Where(a => a.Id == activo.Id).Select(ADto).FirstAsync();
            return ServiceResult<ActivoDTO>.Created(dtoResult);
        }

        public async Task<ServiceResult<bool>> ActualizarAsync(int id, ActualizarActivoDto dto)
        {
            var activo = await _activoRepo.GetByIdAsync(id);
            if (activo == null)
                return ServiceResult<bool>.NotFound();

            var errores = new Dictionary<string, string[]>();
            await ValidarAsync(errores, dto.Nombre, dto.HostnameIp, dto.CategoriaId);
            if (errores.Count > 0)
                return ServiceResult<bool>.Validation(errores);

            activo.Nombre = dto.Nombre!.Trim();
            activo.HostnameIp = string.IsNullOrWhiteSpace(dto.HostnameIp) ? null : dto.HostnameIp.Trim();
            activo.CategoriaId = dto.CategoriaId;
            await _activoRepo.UpdateAsync(activo);
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> EliminarAsync(int id)
        {
            var activo = await _activoRepo.GetByIdAsync(id);
            if (activo == null)
                return ServiceResult<bool>.NotFound();

            // La FK es Restrict: validación de integridad previa para no generar 500
            if (await _db.Incidentes.AnyAsync(i => i.ActivoId == id))
                return ServiceResult<bool>.Conflict("No se puede eliminar el activo porque tiene incidentes registrados.", "ACTIVO_CON_INCIDENTES");

            await _activoRepo.DeleteAsync(id);
            return ServiceResult<bool>.Ok(true);
        }

        private async Task ValidarAsync(Dictionary<string, string[]> errores, string? nombre, string? hostnameIp, int categoriaId)
        {
            Validacion.Texto(errores, "nombre", nombre, NombreMin, NombreMax, requerido: true);
            if (!string.IsNullOrWhiteSpace(hostnameIp) && !Validacion.EsHostnameOIp(hostnameIp.Trim()))
                errores["hostnameIp"] = ["Usa un nombre de host o una IP válida (letras, números, punto, guion o dos puntos)."];
            if (!await _db.CategoriasActivo.AnyAsync(c => c.Id == categoriaId))
                errores["categoriaId"] = ["La categoría seleccionada no existe."];
        }
    }
}

