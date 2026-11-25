using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using saed.api.Security;
using saed.api.Services.Universidad;

namespace saed.api.Controllers
{
    [ApiController]
    [Route("api/universidad")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    public class UniversidadController : ControllerBase
    {
        private readonly CatalogoUniversidadService _catalogoService;

        public UniversidadController(CatalogoUniversidadService catalogoService)
        {
            _catalogoService = catalogoService;
        }

        [HttpPost("catalogos/sincronizar")]
        public async Task<IActionResult> SincronizarCatalogos()
        {
            try
            {
                var resultado = await _catalogoService.SincronizarCatalogos();
                return Ok(resultado);
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
    }
}
