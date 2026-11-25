using Microsoft.IdentityModel.Tokens;
using saed.api.Configuration;
using saed.api.Model;
using saed.api.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace saed.api.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;
        private readonly IHostEnvironment _environment;

        public JwtService(IConfiguration config, IHostEnvironment environment)
        {
            _config = config;
            _environment = environment;
        }

        public string GenerateToken(Usuario user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Username),
                new(ClaimTypes.Role, user.Rol)
            };

            return BuildToken(claims.ToArray());
        }

        public string GenerateExternalAdminToken(string username)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, username),
                new(ClaimTypes.Email, username),
                new(ClaimTypes.Role, "Admin")
            };

            return BuildToken(claims.ToArray());
        }

        public string GenerateAlumnoToken(string matricula, string nombre, string tokenUniversidad)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, matricula),
                new(ClaimTypes.Name, nombre),
                new(ClaimTypes.Role, "Alumno"),
                new(AuthPolicies.UniversidadTokenClaim, tokenUniversidad)
            };

            return BuildToken(claims.ToArray());
        }

        private string BuildToken(Claim[] claims)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(JwtConfiguration.GetSigningKey(_config, _environment)));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
