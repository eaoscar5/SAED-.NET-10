using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using saed.api.DTOs.Evaluaciones;
using saed.api.Security;
using saed.api.Services;

namespace saed.api.Controllers
{
    [ApiController]
    [Route("api/evaluaciones")]
    [Authorize]
    public class EvaluacionesController : ControllerBase
    {
        private readonly EvaluacionService _service;

        public EvaluacionesController(EvaluacionService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Policy = AuthPolicies.StudentOnly)]
        public async Task<IActionResult> Crear([FromBody] CrearEvaluacionDto dto)
        {
            try
            {
                var resultado = await _service.CrearEvaluacion(dto);
                return Ok(resultado);
            }
            catch (ApiValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ApiConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("disponibles")]
        [Authorize(Policy = AuthPolicies.StudentOnly)]
        public async Task<IActionResult> ObtenerDisponibles()
        {
            try
            {
                var result = await _service.ObtenerDisponibles();
                return Ok(result);
            }
            catch (ApiValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("finalizacion")]
        [Authorize(Policy = AuthPolicies.StudentOnly)]
        public async Task<IActionResult> ObtenerFinalizacion()
        {
            try
            {
                return Ok(await _service.ObtenerFinalizacionActual());
            }
            catch (ApiValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("procesos/{procesoId:int}/maestros")]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        public async Task<IActionResult> ListarMaestros(int procesoId)
        {
            try
            {
                var result = await _service.ListarMaestrosPorProceso(procesoId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("procesos/{procesoId:int}/resumen")]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        public async Task<IActionResult> ObtenerResumenProceso(int procesoId)
        {
            var result = await _service.ObtenerResumenProceso(procesoId);
            return Ok(result);
        }

        [HttpGet("procesos/{procesoId:int}/maestros/{idMaestro}/reporte")]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        public async Task<IActionResult> ReporteMaestro(int procesoId, string idMaestro)
        {
            try
            {
                var reporte = await _service.ObtenerReporteMaestro(idMaestro, procesoId);
                return Ok(reporte);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
