namespace saed.api.Configuration
{
    public static class JwtConfiguration
    {
        private const string LocalDevelopmentSigningKey =
            "SAED_LOCAL_DEVELOPMENT_ONLY_JWT_SIGNING_KEY_DO_NOT_USE_IN_PRODUCTION_2026";

        public static string GetSigningKey(IConfiguration configuration, IHostEnvironment environment)
        {
            var key = configuration["Jwt:Key"];

            if (!string.IsNullOrWhiteSpace(key))
                return key;

            if (environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "Configuración crítica de producción incompleta. Falta Jwt__Key.");
            }

            return LocalDevelopmentSigningKey;
        }

        public static bool UsesLocalDevelopmentSigningKey(
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            return !environment.IsProduction() &&
                   string.IsNullOrWhiteSpace(configuration["Jwt:Key"]);
        }
    }
}
