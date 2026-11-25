using System.Net;
using System.Net.Http.Json;

namespace saed.api.Tests;

public class AuthorizationTests
{
    [Fact]
    public async Task Get_administrativo_sin_token_devuelve_401()
    {
        using var factory = new SaedApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/procesos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_administrativo_sin_token_devuelve_401()
    {
        using var factory = new SaedApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/procesos", new
        {
            anio = 2026,
            periodo = 1,
            fechaInicio = "2026-01-01",
            fechaFin = "2026-12-31",
            instrumentoId = SaedApiFactory.InstrumentoCurricularId
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Alumno_en_endpoint_administrativo_devuelve_403()
    {
        using var factory = new SaedApiFactory();
        using var client = factory.CreateClientForRole("Alumno");

        var response = await client.GetAsync("/api/procesos");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Administrador_autorizado_obtiene_endpoint_administrativo()
    {
        using var factory = new SaedApiFactory();
        using var client = factory.CreateClientForRole("Admin");

        var response = await client.GetAsync("/api/procesos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
