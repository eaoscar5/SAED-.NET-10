using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using saed.api.Configuration;
using saed.api.Data;
using saed.api.Seed;

namespace saed.api.Tests;

public class ConfigurationSecurityTests
{
    [Fact]
    public void Produccion_falla_si_falta_configuracion_critica()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "saed.api",
                ["Jwt:Audience"] = "saed.api.users"
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            StartupConfigurationValidator.Validate(configuration, new TestEnvironment("Production")));

        Assert.Contains("ConnectionStrings__DefaultConnection", ex.Message);
        Assert.Contains("Jwt__Key", ex.Message);
        Assert.DoesNotContain("super_secret", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Administrador_predeterminado_conocido_no_se_crea_por_defecto()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        await DbInitializer.SeedAdminAsync(context, new SeedAdminOptions
        {
            Enabled = false
        });

        Assert.Empty(context.Usuarios);
    }

    [Fact]
    public async Task Seed_rechaza_usuario_admin_predeterminado_si_se_habilita()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DbInitializer.SeedAdminAsync(context, new SeedAdminOptions
            {
                Enabled = true,
                Username = "admin",
                Password = "password-no-permitido"
            }));

        Assert.Empty(context.Usuarios);
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public TestEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string ApplicationName { get; set; } = "saed.api.Tests";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; }

        public string WebRootPath { get; set; } = string.Empty;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
