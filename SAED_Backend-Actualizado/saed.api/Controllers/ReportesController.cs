using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using saed.api.DTOs.Reportes;
using saed.api.Security;
using saed.api.Services;

namespace saed.api.Controllers
{
    [ApiController]
    [Route("api/reportes")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public class ReportesController : ControllerBase
    {
        private readonly ReportesService _service;
        private readonly IWebHostEnvironment _environment;

        public ReportesController(ReportesService service, IWebHostEnvironment environment)
        {
            _service = service;
            _environment = environment;
        }

        [HttpGet("catalogos")]
        public async Task<IActionResult> Catalogos([FromQuery] int? procesoId = null)
        {
            return Ok(await _service.ObtenerCatalogos(procesoId));
        }

        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar([FromQuery] ReporteFiltroDto filtro)
        {
            try
            {
                return Ok(await _service.Buscar(filtro));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("progreso/grupos")]
        public async Task<IActionResult> ProgresoGrupos([FromQuery] ReporteProgresoGrupoFiltroDto filtro)
        {
            try
            {
                return Ok(await _service.ObtenerProgresoGrupos(filtro));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("progreso/grupos/{grupo}")]
        public async Task<IActionResult> ProgresoGrupoDetalle(
            string grupo,
            [FromQuery] int? procesoId = null)
        {
            try
            {
                return Ok(await _service.ObtenerProgresoGrupoDetalle(procesoId, grupo));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("mock/sembrar")]
        public async Task<IActionResult> SembrarMock()
        {
            if (!_environment.IsDevelopment())
                return Forbid();

            return Ok(await _service.SembrarDatosMock());
        }

        [HttpPost("mock/sembrar-detallado")]
        public async Task<IActionResult> SembrarMockDetallado()
        {
            if (!_environment.IsDevelopment())
                return Forbid();

            return Ok(await _service.SembrarDatosMockDetallado());
        }
    }
}
