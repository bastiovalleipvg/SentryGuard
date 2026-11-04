using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SentryGuard.Data;
using SentryGuard.DTOs;
using SentryGuard.Repositories;
using SentryGuard.Services;

namespace SentryGuard.Endpoints
{
    public static class OrganizacionAPI
    {
        public static void MapOrganizacionApi(this WebApplication app)
        {
            var grupo = app.MapGroup("/api/v1/organizaciones").WithTags("Organizaciones").RequireAuthorization();
            // Listar todas las organizaciones es para quien administra o coordina; el Analista trabaja por
            // incidente asignado, no por organizacion, y un Cliente no necesita ver otras.
            grupo.MapGet("", Listar).WithName("Listar organizaciones")
                .RequireAuthorization(p => p.RequireRole("Administrador", "Supervisor"));
            // Obtener por id: Administrador/Supervisor cualquiera, Cliente solo la suya, Analista ninguna (ValidaAccesoOrganizacion).
            grupo.MapGet("/{id}", Obtener).WithName("Obtener organizacion");
            grupo.MapPost("", Crear).WithName("Crear organizacion")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
            grupo.MapPut("/{id}", Actualizar).WithName("Actualizar organizacion")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
            grupo.MapDelete("/{id}", Eliminar).WithName("Eliminar organizacion")
                .RequireAuthorization(p => p.RequireRole("Administrador"));

            // Contratos SLA de la organizacion (tb_ContratoSLA + tb_ContratoSLA_Detalle).
            grupo.MapGet("/{id}/contratos-sla", ListarContratos).WithName("Listar contratos SLA");
            grupo.MapPost("/{id}/contratos-sla", CrearContrato).WithName("Crear contrato SLA")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
            grupo.MapPost("/{id}/contratos-sla/{contratoId}/finalizar", FinalizarContrato).WithName("Finalizar contrato SLA")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
            grupo.MapDelete("/{id}/contratos-sla/{contratoId}", EliminarContrato).WithName("Eliminar contrato SLA")
                .RequireAuthorization(p => p.RequireRole("Administrador"));
        }

        private const int RazonSocialMin = 3;
        private const int RazonSocialMax = 200;
        private const int BusquedaMax = 100;

        private static readonly Expression<Func<Models.Organizacion, OrganizacionDTO>> ADto = o => new OrganizacionDTO
        {
            Id = o.Id,
            RazonSocial = o.RazonSocial,
            Rut = o.Rut,
            PlanId = o.PlanContratadoId,
            Plan = o.Plan!.Nombre,
            FechaAlta = o.FechaAlta
        };

        private static async Task<IResult> Listar(SentryGuardDbContext db, int page = 1, int pageSize = 10, string? q = null, int? planId = null)
        {
            (page, pageSize) = Paginacion.Normalizar(page, pageSize);

            IQueryable<Models.Organizacion> query = db.Organizaciones;
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

            return Results.Ok(new { total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize), items });
        }

        // Misma regla que el resto de la API (OWASP API1): Cliente solo la suya; Analista no accede por organizacion.
        private static async Task<IResult?> ValidarAccesoAsync(int id, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var usuario = await authService.ObtenerUsuarioAutenticadoAsync(httpContext, db);
            if (usuario == null) return Results.Unauthorized();
            if (!authService.ValidaAccesoOrganizacion(usuario, id)) return Results.Forbid();
            return null;
        }

        private static async Task<IResult> Obtener(int id, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var denegado = await ValidarAccesoAsync(id, db, httpContext, authService);
            if (denegado != null) return denegado;

            var dto = await db.Organizaciones.Where(o => o.Id == id).Select(ADto).FirstOrDefaultAsync();
            return dto == null ? Results.NotFound() : Results.Ok(dto);
        }

        private static async Task<(Dictionary<string, string[]> Errores, string? Rut)> ValidarAsync(
            SentryGuardDbContext db, OrganizacionRequest req, int? idActual, string? rutActual)
        {
            var errores = new Dictionary<string, string[]>();
            Validacion.Texto(errores, "razonSocial", req.RazonSocial, RazonSocialMin, RazonSocialMax, requerido: true);

            var rut = Validacion.NormalizarRut(req.Rut);
            if (rut == null)
                errores["rut"] = ["Ingresa el RUT sin puntos y con guion, por ejemplo 76123456-0."];
            // El DV solo se exige si el RUT cambia: asi se puede editar una organizacion cargada antes de esta regla.
            else if (rut != rutActual && !Validacion.RutTieneDvValido(rut))
                errores["rut"] = ["El dígito verificador del RUT no es válido."];
            else if (await db.Organizaciones.AnyAsync(o => o.Rut == rut && o.Id != idActual))
                errores["rut"] = ["Ya existe una organización con ese RUT."];

            if (!await db.PlanesServicio.AnyAsync(p => p.Id == req.PlanId))
                errores["planId"] = ["El plan seleccionado no existe."];

            return (errores, rut);
        }

        private static async Task<IResult> Crear(OrganizacionRequest req, IOrganizacionRepository repo, SentryGuardDbContext db)
        {
            var (errores, rut) = await ValidarAsync(db, req, null, null);
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var org = new Models.Organizacion { RazonSocial = req.RazonSocial!.Trim(), Rut = rut!, PlanContratadoId = req.PlanId, FechaAlta = DateTime.UtcNow };
            await repo.AddAsync(org);

            var dto = await db.Organizaciones.Where(o => o.Id == org.Id).Select(ADto).FirstAsync();
            return Results.Created($"/api/v1/organizaciones/{org.Id}", dto);
        }

        private static async Task<IResult> Actualizar(int id, OrganizacionRequest req, IOrganizacionRepository repo, SentryGuardDbContext db)
        {
            var org = await repo.GetByIdAsync(id);
            if (org == null) return Results.NotFound();

            var (errores, rut) = await ValidarAsync(db, req, id, org.Rut);
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            org.RazonSocial = req.RazonSocial!.Trim();
            org.Rut = rut!;
            org.PlanContratadoId = req.PlanId;
            await repo.UpdateAsync(org);
            return Results.Ok(new { mensaje = "Actualizado" });
        }

        private static async Task<IResult> Eliminar(int id, IOrganizacionRepository repo, SentryGuardDbContext db)
        {
            var org = await repo.GetByIdAsync(id);
            if (org == null) return Results.NotFound();

            // Las FK hacia la organizacion son Restrict: sin este control el DELETE terminaria en un 500.
            if (await db.Activos.AnyAsync(a => a.OrganizacionId == id) ||
                await db.Incidentes.AnyAsync(i => i.OrganizacionId == id) ||
                await db.Usuarios.AnyAsync(u => u.OrganizacionId == id) ||
                await db.ContratosSla.AnyAsync(c => c.OrganizacionId == id))
                return Validacion.Conflicto("ORGANIZACION_CON_DATOS",
                    "No se puede eliminar la organización porque tiene activos, incidentes, usuarios o contratos SLA asociados.");

            await repo.DeleteAsync(id);
            return Results.NoContent();
        }

        // ---------- Contratos SLA ----------

        private static async Task<IResult> ListarContratos(int id, SentryGuardDbContext db, HttpContext httpContext, AuthService authService)
        {
            var denegado = await ValidarAccesoAsync(id, db, httpContext, authService);
            if (denegado != null) return denegado;
            if (!await db.Organizaciones.AnyAsync(o => o.Id == id)) return Results.NotFound();

            var contratos = await db.ContratosSla
                .Where(c => c.OrganizacionId == id)
                .OrderByDescending(c => c.VigenteDesde)
                .Select(c => new ContratoSlaDTO
                {
                    Id = c.Id,
                    VigenteDesde = c.VigenteDesde,
                    VigenteHasta = c.VigenteHasta,
                    Detalles = db.ContratosSlaDetalle
                        .Where(d => d.ContratoId == c.Id)
                        .Join(db.Severidades, d => d.SeveridadId, s => s.Id, (d, s) => new ContratoSlaDetalleDTO
                        {
                            SeveridadId = s.Id,
                            Severidad = s.Nombre,
                            HorasSla = d.HorasSla
                        })
                        .OrderBy(d => d.SeveridadId)
                        .ToList()
                })
                .ToListAsync();

            return Results.Ok(new { total = contratos.Count, items = contratos });
        }

        private static async Task<IResult> CrearContrato(int id, CrearContratoRequest req, SentryGuardDbContext db)
        {
            if (!await db.Organizaciones.AnyAsync(o => o.Id == id)) return Results.NotFound();

            var errores = new Dictionary<string, string[]>();
            var desde = req.VigenteDesde.Date;
            var hasta = req.VigenteHasta?.Date;
            if (hasta.HasValue && hasta.Value <= desde)
                errores["vigenteHasta"] = ["Debe ser posterior a la fecha de inicio."];

            var detalles = req.Detalles ?? [];
            if (detalles.Count == 0)
                errores["detalles"] = ["Indica las horas comprometidas para al menos una severidad."];
            else if (detalles.Select(d => d.SeveridadId).Distinct().Count() != detalles.Count)
                errores["detalles"] = ["Cada severidad puede aparecer una sola vez."];
            else
            {
                var idsSeveridad = detalles.Select(d => d.SeveridadId).ToList();
                if (await db.Severidades.CountAsync(s => idsSeveridad.Contains(s.Id)) != idsSeveridad.Count)
                    errores["detalles"] = ["Alguna de las severidades indicadas no existe."];
                else if (detalles.Any(d => d.HorasSla <= 0 || d.HorasSla > 720))
                    errores["detalles"] = ["Las horas deben ser mayores que 0 y no superar 720."];
            }

            // Los periodos no pueden superponerse (cubre tambien el indice unico de un solo contrato abierto).
            var fin = hasta ?? DateTime.MaxValue;
            if (!errores.ContainsKey("vigenteHasta") && await db.ContratosSla.AnyAsync(c =>
                    c.OrganizacionId == id && c.VigenteDesde < fin && (c.VigenteHasta ?? DateTime.MaxValue) > desde))
                errores["vigenteDesde"] = ["El periodo se superpone con otro contrato de la organización. Finaliza el contrato vigente primero."];

            if (errores.Count > 0) return Results.ValidationProblem(errores);

            await using var transaccion = await db.Database.BeginTransactionAsync();
            var contrato = new Models.ContratoSLA { OrganizacionId = id, VigenteDesde = desde, VigenteHasta = hasta };
            db.ContratosSla.Add(contrato);
            await db.SaveChangesAsync();

            db.ContratosSlaDetalle.AddRange(detalles.Select(d => new Models.ContratoSLADetalle
            {
                ContratoId = contrato.Id,
                SeveridadId = d.SeveridadId,
                HorasSla = d.HorasSla
            }));
            await db.SaveChangesAsync();
            await transaccion.CommitAsync();

            return Results.Created($"/api/v1/organizaciones/{id}/contratos-sla/{contrato.Id}", new { id = contrato.Id });
        }

        // Cierra un contrato abierto con fecha de hoy, para poder registrar el siguiente.
        private static async Task<IResult> FinalizarContrato(int id, int contratoId, SentryGuardDbContext db)
        {
            var contrato = await db.ContratosSla.FirstOrDefaultAsync(c => c.Id == contratoId && c.OrganizacionId == id);
            if (contrato == null) return Results.NotFound();
            if (contrato.VigenteHasta.HasValue)
                return Validacion.Conflicto("CONTRATO_YA_FINALIZADO", "El contrato ya tiene fecha de término.");

            var hoy = DateTime.UtcNow.Date;
            contrato.VigenteHasta = hoy > contrato.VigenteDesde ? hoy : contrato.VigenteDesde.AddDays(1);
            await db.SaveChangesAsync();
            return Results.Ok(new { mensaje = "Contrato finalizado" });
        }

        private static async Task<IResult> EliminarContrato(int id, int contratoId, SentryGuardDbContext db)
        {
            var contrato = await db.ContratosSla.FirstOrDefaultAsync(c => c.Id == contratoId && c.OrganizacionId == id);
            if (contrato == null) return Results.NotFound();

            // Los detalles se eliminan en cascada (FK con DeleteBehavior.Cascade).
            db.ContratosSla.Remove(contrato);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }

        record OrganizacionRequest(string? RazonSocial, string? Rut, int PlanId);
        record CrearContratoRequest(DateTime VigenteDesde, DateTime? VigenteHasta, List<DetalleContratoRequest>? Detalles);
        record DetalleContratoRequest(int SeveridadId, decimal HorasSla);
    }
}
