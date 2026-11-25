using Microsoft.EntityFrameworkCore;
using saed.api.Configuration;
using saed.api.Data;
using saed.api.Model;

namespace saed.api.Seed
{
    public static class DbInitializer
    {
        public static async Task SeedAdminAsync(AppDbContext context, SeedAdminOptions options)
        {
            if (!options.Enabled)
                return;

            var username = options.Username.Trim();
            var password = options.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException(
                    "SeedAdmin está habilitado, pero faltan SeedAdmin__Username o SeedAdmin__Password.");

            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "SeedAdmin no permite crear el usuario administrador predeterminado conocido.");
            }

            var adminExiste = await context.Usuarios
                .AnyAsync(u => u.Rol == "Admin");

            if (adminExiste)
                return;

            var admin = new Usuario
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Rol = "Admin"
            };

            context.Usuarios.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
