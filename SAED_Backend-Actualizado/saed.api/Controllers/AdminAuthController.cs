using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using saed.api.DTOs.Auth;
using saed.api.Services;

namespace saed.api.Controllers
{
    [ApiController]
    [Route("api/admin/auth")]
    [AllowAnonymous]
    public class AdminAuthController : ControllerBase
    {
        private readonly AdminAuthService _service;

        public AdminAuthController(AdminAuthService service)
        {
            _service = service;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try
            {
                var token = await _service.Login(dto.Username, dto.Password);
                return Ok(new { token });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}
