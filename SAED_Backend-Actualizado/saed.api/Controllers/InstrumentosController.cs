using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using saed.api.DTOs.Instrumentos;
using saed.api.Security;
using saed.api.Services;

namespace saed.api.Controllers
{
    [ApiController]
    [Route("api/instrumentos")]
    [Authorize]
    public class InstrumentosController : ControllerBase
    {
        private readonly InstrumentoService _service;

        public InstrumentosController(InstrumentoService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        public async Task<ActionResult<List<InstrumentoCreateDto>>> GetAll()
        {
            var instrumentos = await _service.GetAllAsync();
            return Ok(instrumentos);
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AuthPolicies.StudentOrAdmin)]
        public async Task<ActionResult<InstrumentoCreateDto>> GetById(int id)
        {
            var instrumento = await _service.GetByIdAsync(id);
            if (instrumento == null)
                return NotFound();

            return Ok(instrumento);
        }

        [HttpPost]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        public async Task<ActionResult<InstrumentoCreateDto>> Create([FromBody] InstrumentoCreateDto dto)
        {
            try
            {
                var creado = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id:int}/clonar")]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        public async Task<ActionResult<InstrumentoCreateDto>> ClonarPlantilla(int id)
        {
            try
            {
                var clonado = await _service.ClonePlantillaAsync(id);
                if (clonado == null)
                    return NotFound();

                return CreatedAtAction(nameof(GetById), new { id = clonado.Id }, clonado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        public async Task<ActionResult<InstrumentoCreateDto>> Update(int id, [FromBody] InstrumentoCreateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("El id de la ruta no coincide con el id del DTO.");

            try
            {
                var actualizado = await _service.UpdateAsync(id, dto);
                if (actualizado == null)
                    return NotFound();

                return Ok(actualizado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthPolicies.AdminOnly)]
        public async Task<IActionResult> Delete(int id)
        {
            var eliminado = await _service.DeleteAsync(id);
            if (!eliminado)
                return NotFound();

            return NoContent();
        }
    }
}
