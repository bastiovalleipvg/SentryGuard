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
    /// Implementación del servicio de lógica de negocio para Organizaciones y Contratos SLA (ADR-002).
    /// </summary>
    public class OrganizacionService : IOrganizacionService
    {
        private readonly IOrganizacionRepository _orgRepo;
        private readonly AuthService _authService;
        private readonly SentryGuardDbContext _db;

        private const int RazonSocialMin = 3;
        private const int RazonSocialMax = 200;
        private const int BusquedaMax = 100;

        public static readonly Expression<Func<Organizacion, OrganizacionDTO>> ADto = o => new OrganizacionDTO
        {
            Id = o.Id,
            RazonSocial = o.RazonSocial,
            Rut = o.Rut,
            PlanId = o.PlanContratadoId,
            Plan = o.Plan!.Nombre,
            FechaAlta = o.FechaAlta
        };

        public OrganizacionService(IOrganizacionRepository orgRepo, AuthService authService, SentryGuardDbContext db)
        {
            _orgRepo = orgRepo;
            _authService = authService;
            _db = db;
        }

        public async Task<ServiceResult<PagedResult<OrganizacionDTO>>> ListarAsync(int page, int pageSize, string? q, int? planId)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            IQueryable<Organizacion> query = _db.Organizaciones;
            if (!string.IsNullOrWhiteSpace(q))
            {
                var termino = q.Trim();
                if (termino.Length > BusquedaMax) termino = termino[..BusquedaMax];
                query = query.Where(o => o.RazonSocial.Contains(termino) || o.Rut.Contains(termino));
            }
            if (planId.HasValue) query = query.Where(o => o.PlanContratadoId == planId.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(o => o.RazonSocial)
                .ThenBy(o => o.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ADto)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)total / pageSize);
            return ServiceResult<PagedResult<OrganizacionDTO>>.Ok(new PagedResult<OrganizacionDTO>(total, page, pageSize, totalPages, items));
        }

        public async Task<ServiceResult<OrganizacionDTO>> ObtenerAsync(int id, Usuario usuario)
        {
            if (!_authService.ValidaAccesoOrganizacion(usuario, id))
                return ServiceResult<OrganizacionDTO>.Forbid();

            var dto = await _db.Organizaciones.Where(o => o.Id == id).Select(ADto).FirstOrDefaultAsync();
            if (dto == null)
                return ServiceResult<OrganizacionDTO>.NotFound();

            return ServiceResult<OrganizacionDTO>.Ok(dto);
        }

        public async Task<ServiceResult<OrganizacionDTO>> CrearAsync(OrganizacionDto dto)
        {
            var (errores, rut) = await ValidarAsync(dto, null, null);
            if (errores.Count > 0)
                return ServiceResult<OrganizacionDTO>.Validation(errores);

            var org = new Organizacion
            {
                RazonSocial = dto.RazonSocial!.Trim(),
                Rut = rut!,
                PlanContratadoId = dto.PlanId,
                FechaAlta = DateTime.UtcNow
            };
            await _orgRepo.AddAsync(org);

            var dtoResult = await _db.Organizaciones.Where(o => o.Id == org.Id).Select(ADto).FirstAsync();
            return ServiceResult<OrganizacionDTO>.Created(dtoResult);
        }

        public async Task<ServiceResult<bool>> ActualizarAsync(int id, OrganizacionDto dto)
        {
            var org = await _orgRepo.GetByIdAsync(id);
            if (org == null)
                return ServiceResult<bool>.NotFound();

            var (errores, rut) = await ValidarAsync(dto, id, org.Rut);
            if (errores.Count > 0)
                return ServiceResult<bool>.Validation(errores);

            org.RazonSocial = dto.RazonSocial!.Trim();
            org.Rut = rut!;
            org.PlanContratadoId = dto.PlanId;
            await _orgRepo.UpdateAsync(org);
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> EliminarAsync(int id)
        {
            var org = await _orgRepo.GetByIdAsync(id);
            if (org == null)
                return ServiceResult<bool>.NotFound();

            // Validación de integridad previa
            if (await _db.Activos.AnyAsync(a => a.OrganizacionId == id) ||
                await _db.Incidentes.AnyAsync(i => i.OrganizacionId == id) ||
                await _db.Usuarios.AnyAsync(u => u.OrganizacionId == id) ||
                await _db.ContratosSla.AnyAsync(c => c.OrganizacionId == id))
            {
                return ServiceResult<bool>.Conflict(
                    "No se puede eliminar la organización porque tiene activos, incidentes, usuarios o contratos SLA asociados.",
                    "ORGANIZACION_CON_DATOS");
            }

            await _orgRepo.DeleteAsync(id);
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<IEnumerable<ContratoSlaDTO>>> ListarContratosAsync(int id, Usuario usuario)
        {
            if (!_authService.ValidaAccesoOrganizacion(usuario, id))
                return ServiceResult<IEnumerable<ContratoSlaDTO>>.Forbid();

            if (!await _db.Organizaciones.AnyAsync(o => o.Id == id))
                return ServiceResult<IEnumerable<ContratoSlaDTO>>.NotFound();

            var contratos = await _db.ContratosSla
                .Where(c => c.OrganizacionId == id)
                .OrderByDescending(c => c.VigenteDesde)
                .Select(c => new ContratoSlaDTO
                {
                    Id = c.Id,
                    VigenteDesde = c.VigenteDesde,
                    VigenteHasta = c.VigenteHasta,
                    Detalles = _db.ContratosSlaDetalle
                        .Where(d => d.ContratoId == c.Id)
                        .Join(_db.Severidades, d => d.SeveridadId, s => s.Id, (d, s) => new ContratoSlaDetalleDTO
                        {
                            SeveridadId = s.Id,
                            Severidad = s.Nombre,
                            HorasSla = d.HorasSla
                        })
                        .OrderBy(d => d.SeveridadId)
                        .ToList()
                })
                .ToListAsync();

            return ServiceResult<IEnumerable<ContratoSlaDTO>>.Ok(contratos);
        }

        public async Task<ServiceResult<int>> CrearContratoAsync(int organizacionId, CrearContratoDto dto)
        {
            if (!await _db.Organizaciones.AnyAsync(o => o.Id == organizacionId))
                return ServiceResult<int>.NotFound();

            var errores = new Dictionary<string, string[]>();
            var desde = dto.VigenteDesde.Date;
            var hasta = dto.VigenteHasta?.Date;

            if (hasta.HasValue && hasta.Value <= desde)
                errores["vigenteHasta"] = ["Debe ser posterior a la fecha de inicio."];

            var detalles = dto.Detalles ?? [];
            if (detalles.Count == 0)
                errores["detalles"] = ["Indica las horas comprometidas para al menos una severidad."];
            else if (detalles.Select(d => d.SeveridadId).Distinct().Count() != detalles.Count)
                errores["detalles"] = ["Cada severidad puede aparecer una sola vez."];
            else
            {
                var idsSeveridad = detalles.Select(d => d.SeveridadId).ToList();
                if (await _db.Severidades.CountAsync(s => idsSeveridad.Contains(s.Id)) != idsSeveridad.Count)
                    errores["detalles"] = ["Alguna de las severidades indicadas no existe."];
                else if (detalles.Any(d => d.HorasSla <= 0 || d.HorasSla > 720))
                    errores["detalles"] = ["Las horas deben ser mayores que 0 y no superar 720."];
            }

            // Los periodos no pueden superponerse
            var fin = hasta ?? DateTime.MaxValue;
            if (!errores.ContainsKey("vigenteHasta") && await _db.ContratosSla.AnyAsync(c =>
                    c.OrganizacionId == organizacionId && c.VigenteDesde < fin && (c.VigenteHasta ?? DateTime.MaxValue) > desde))
                errores["vigenteDesde"] = ["El periodo se superpone con otro contrato de la organización. Finaliza el contrato vigente primero."];

            if (errores.Count > 0)
                return ServiceResult<int>.Validation(errores);

            await using var transaccion = await _db.Database.BeginTransactionAsync();
            var contrato = new ContratoSLA { OrganizacionId = organizacionId, VigenteDesde = desde, VigenteHasta = hasta };
            _db.ContratosSla.Add(contrato);
            await _db.SaveChangesAsync();

            _db.ContratosSlaDetalle.AddRange(detalles.Select(d => new ContratoSLADetalle
            {
                ContratoId = contrato.Id,
                SeveridadId = d.SeveridadId,
                HorasSla = d.HorasSla
            }));
            await _db.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ServiceResult<int>.Created(contrato.Id);
        }

        public async Task<ServiceResult<bool>> FinalizarContratoAsync(int organizacionId, int contratoId)
        {
            var contrato = await _db.ContratosSla.FirstOrDefaultAsync(c => c.Id == contratoId && c.OrganizacionId == organizacionId);
            if (contrato == null)
                return ServiceResult<bool>.NotFound();

            if (contrato.VigenteHasta.HasValue)
                return ServiceResult<bool>.Conflict("El contrato ya tiene fecha de término.", "CONTRATO_YA_FINALIZADO");

            var hoy = DateTime.UtcNow.Date;
            contrato.VigenteHasta = hoy > contrato.VigenteDesde ? hoy : contrato.VigenteDesde.AddDays(1);
            await _db.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> EliminarContratoAsync(int organizacionId, int contratoId)
        {
            var contrato = await _db.ContratosSla.FirstOrDefaultAsync(c => c.Id == contratoId && c.OrganizacionId == organizacionId);
            if (contrato == null)
                return ServiceResult<bool>.NotFound();

            _db.ContratosSla.Remove(contrato);
            await _db.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        private async Task<(Dictionary<string, string[]> Errores, string? Rut)> ValidarAsync(
            OrganizacionDto dto, int? idActual, string? rutActual)
        {
            var errores = new Dictionary<string, string[]>();
            Validacion.Texto(errores, "razonSocial", dto.RazonSocial, RazonSocialMin, RazonSocialMax, requerido: true);

            var rut = Validacion.NormalizarRut(dto.Rut);
            if (rut == null)
                errores["rut"] = ["Ingresa el RUT sin puntos y con guion, por ejemplo 76123456-0."];
            else if (rut != rutActual && !Validacion.RutTieneDvValido(rut))
                errores["rut"] = ["El dígito verificador del RUT no es válido."];
            else if (await _db.Organizaciones.AnyAsync(o => o.Rut == rut && o.Id != idActual))
                errores["rut"] = ["Ya existe una organización con ese RUT."];

            if (!await _db.PlanesServicio.AnyAsync(p => p.Id == dto.PlanId))
                errores["planId"] = ["El plan seleccionado no existe."];

            return (errores, rut);
        }
    }
}

