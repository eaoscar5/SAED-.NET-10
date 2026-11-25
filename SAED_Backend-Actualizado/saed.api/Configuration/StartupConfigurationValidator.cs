namespace saed.api.Configuration
{
    public static class StartupConfigurationValidator
    {
        public static void Validate(IConfiguration configuration, IWebHostEnvironment environment)
        {
            if (!environment.IsProduction())
                return;

            var missing = new List<string>();

            Require(configuration.GetConnectionString("DefaultConnection"), "ConnectionStrings__DefaultConnection", missing);
            Require(configuration["Jwt:Issuer"], "Jwt__Issuer", missing);
            Require(configuration["Jwt:Audience"], "Jwt__Audience", missing);
            RequireSecret(configuration["Jwt:Key"], "Jwt__Key", missing, minLength: 32);
            Require(configuration["UniversidadApi:BaseUrl"], "UniversidadApi__BaseUrl", missing);
            RequireSecret(configuration["UniversidadApi:ServiceUsername"], "UniversidadApi__ServiceUsername", missing);
            RequireSecret(configuration["UniversidadApi:ServicePassword"], "UniversidadApi__ServicePassword", missing);

            var adminUsernames = configuration
                .GetSection("UniversidadApi:AdminUsernames")
                .Get<List<string>>() ?? new List<string>();

            if (!adminUsernames.Any(u => !string.IsNullOrWhiteSpace(u)))
                missing.Add("UniversidadApi__AdminUsernames__0");

            if (missing.Count == 0)
                return;

            throw new InvalidOperationException(
                "Configuración crítica de producción incompleta. Faltan: " +
                string.Join(", ", missing) +
                ". Configure los valores mediante variables de entorno o un proveedor seguro de secretos.");
        }

        private static void Require(string? value, string key, List<string> missing)
        {
            if (string.IsNullOrWhiteSpace(value) || IsPlaceholder(value))
                missing.Add(key);
        }

        private static void RequireSecret(
            string? value,
            string key,
            List<string> missing,
            int minLength = 1)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Trim().Length < minLength ||
                IsPlaceholder(value))
            {
                missing.Add(key);
            }
        }

        private static bool IsPlaceholder(string value)
        {
            var normalized = value.Trim().ToUpperInvariant();

            return normalized.Contains("CAMBIAR") ||
                   normalized.Contains("REEMPLAZAR") ||
                   normalized.Contains("PASSWORD") ||
                   normalized.Contains("USUARIO_REAL") ||
                   normalized.Contains("SECRETO") ||
                   normalized.Contains("SERVIDOR_BD") ||
                   normalized.Contains("NOMBRE_BD");
        }
    }
}
