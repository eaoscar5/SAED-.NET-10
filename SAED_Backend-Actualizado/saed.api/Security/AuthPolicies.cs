using System.Security.Claims;

namespace saed.api.Security
{
    public static class AuthPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string StudentOnly = "StudentOnly";
        public const string StudentOrAdmin = "StudentOrAdmin";

        public const string AdminRole = "Admin";
        public const string StudentRole = "Alumno";
        public const string UniversidadTokenClaim = "token_universidad";

        public static readonly string RoleClaimType = ClaimTypes.Role;
    }
}
