using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using saed.api.Model;
using saed.api.Security;
using saed.api.Services;

namespace saed.api.Controllers
{
    [ApiController]
    [Route("api/calificaciones")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public class CalificacionesController : ControllerBase
    {
        private readonly CalificacionService _service;

        public CalificacionesController(CalificacionService service)
        {
            _service = service;
        }

        [HttpGet("procesos/{procesoId}")]
        public async Task<IActionResult> ListarPorProceso(int procesoId)
        {
            try
            {
                var resultado = await _service.ListarCalificaciones(procesoId);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("procesos/{procesoId}/maestros/{idMaestro}")]
        public async Task<IActionResult> ObtenerCalificacion(
            int procesoId,
            string idMaestro,
            [FromQuery] string? materiaId = null,
            [FromQuery] string? grupo = null,
            [FromQuery] TipoInstrumento? tipoInstrumento = null,
            [FromQuery] int? instrumentoId = null)
        {
            try
            {
                var resultado = await _service.ObtenerCalificacion(
                    idMaestro,
                    procesoId,
                    materiaId,
                    grupo,
                    tipoInstrumento,
                    instrumentoId);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
