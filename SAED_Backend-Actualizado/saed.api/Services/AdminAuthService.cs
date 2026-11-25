using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using saed.api.Data;
using saed.api.Services.Universidad;

namespace saed.api.Services
{
    public class AdminAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        private readonly IUniversidadApiService _universidadApi;
        private readonly UniversidadApiOptions _universidadOptions;

        public AdminAuthService(
            AppDbContext context,
            JwtService jwtService,
            IUniversidadApiService universidadApi,
            IOptions<UniversidadApiOptions> universidadOptions)
        {
            _context = context;
            _jwtService = jwtService;
            _universidadApi = universidadApi;
            _universidadOptions = universidadOptions.Value;
        }

        public async Task<string> Login(string username, string password)
        {
            username = username.Trim();

            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user != null)
            {
                var valid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

                if (!valid)
                    throw new UnauthorizedAccessException("Credenciales incorrectas.");

                return _jwtService.GenerateToken(user);
            }

            return await LoginAdminUniversidad(username, password);
        }

        private async Task<string> LoginAdminUniversidad(string username, string password)
        {
            var autorizado = _universidadOptions.AdminUsernames.Any(admin =>
                admin.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (!autorizado)
                throw new UnauthorizedAccessException("Usuario no encontrado o no autorizado como administrador.");

            try
            {
                var tokenUniversidad = await _universidadApi.Authenticate(username, password);
                var puedeLeerCatalogos = await _universidadApi.CanReadCatalogs(tokenUniversidad);

                if (!puedeLeerCatalogos)
                    throw new UnauthorizedAccessException("Usuario sin permisos para consultar catalogos universitarios.");

                return _jwtService.GenerateExternalAdminToken(username);
            }
            catch (HttpRequestException)
            {
                throw new UnauthorizedAccessException("Credenciales incorrectas o sin permiso de administrador.");
            }
        }
    }
}
