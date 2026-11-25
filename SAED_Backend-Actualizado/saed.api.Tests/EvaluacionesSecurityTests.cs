using System.Net;
using System.Net.Http.Json;

namespace saed.api.Tests;

public class EvaluacionesSecurityTests
{
    [Fact]
    public async Task Alumno_puede_evaluar_asignacion_propia_valida_y_respuesta_abierta_persiste()
    {
        using var factory = new SaedApiFactory();
        using var client = factory.CreateClientForRole("Alumno");

        var response = await client.PostAsJsonAsync(
            "/api/evaluaciones",
            SaedApiFactory.EvaluacionValidaPayload("Texto abierto persistido"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var evaluacion = await factory.GetSingleEvaluacion();
        Assert.NotNull(evaluacion);
        Assert.Equal("M1", evaluacion!.IdMaestro);
        Assert.Equal("Maestro Uno", evaluacion.NombreMaestro);
        Assert.Equal("MAT1", evaluacion.MateriaId);
        Assert.Equal("Materia Uno", evaluacion.NombreMateria);
        Assert.Equal("G1", evaluacion.Grupo);
        Assert.Equal(SaedApiFactory.AssignmentId, evaluacion.AsignacionAcademicaId);
        Assert.Contains(evaluacion.Respuestas, r => r.PreguntaId == SaedApiFactory.PreguntaAbiertaId && r.Texto == "Texto abierto persistido");
    }

    [Fact]
    public async Task Alumno_no_puede_evaluar_asignacion_ajena()
    {
        using var factory = new SaedApiFactory();
        using var client = factory.CreateClientForRole("Alumno");

        var response = await client.PostAsJsonAsync("/api/evaluaciones", new
        {
            asignacionAcademicaId = "G9|M9|MAT9",
            instrumentoId = SaedApiFactory.InstrumentoCurricularId,
            respuestas = RespuestasValidas(),
            comentario = "Comentario"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await factory.CountEvaluaciones());
    }

    [Fact]
    public async Task Alumno_no_puede_falsificar_maestro_materia_o_grupo_con_campos_extra()
    {
        using var factory = new SaedApiFactory();
        using var client = factory.CreateClientForRole("Alumno");

        var response = await client.PostAsJsonAsync(
            "/api/evaluaciones",
            SaedApiFactory.EvaluacionValidaPayload());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var evaluacion = await factory.GetSingleEvaluacion();
        Assert.NotNull(evaluacion);
        Assert.Equal("M1", evaluacion!.IdMaestro);
        Assert.Equal("MAT1", evaluacion.MateriaId);
        Assert.Equal("G1", evaluacion.Grupo);
    }

    [Fact]
    public async Task No_puede_responder_pregunta_de_otro_instrumento()
    {
        using var factory = new SaedApiFactory();
        using var client = factory.CreateClientForRole("Alumno");

        var response = await client.PostAsJsonAsync("/api/evaluaciones", new
        {
            asignacionAcademicaId = SaedApiFactory.AssignmentId,
            instrumentoId = SaedApiFactory.InstrumentoCurricularId,
            respuestas = new object[]
            {
                new { preguntaId = SaedApiFactory.PreguntaCerradaId, opcionId = SaedApiFactory.OpcionCerradaId, texto = (string?)null },
                new { preguntaId = 1003, opcionId = SaedApiFactory.OpcionOtraPreguntaId, texto = (string?)null },
                new { preguntaId = SaedApiFactory.PreguntaAbiertaId, opcionId = (int?)null, texto = "Abierta" },
                new { preguntaId = SaedApiFactory.PreguntaOtroInstrumentoId, opcionId = SaedApiFactory.OpcionOtroInstrumentoId, texto = (string?)null }
            },
            comentario = "Comentario"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await factory.CountEvaluaciones());
    }

    [Fact]
    public async Task No_puede_usar_opcion_de_otra_pregunta()
    {
        using var factory = new SaedApiFactory();
        using var client = factory.CreateClientForRole("Alumno");

        var response = await client.PostAsJsonAsync("/api/evaluaciones", new
        {
            asignacionAcademicaId = SaedApiFactory.AssignmentId,
            instrumentoId = SaedApiFactory.InstrumentoCurricularId,
            respuestas = new object[]
            {
                new { preguntaId = SaedApiFactory.PreguntaCerradaId, opcionId = SaedApiFactory.OpcionOtraPreguntaId, texto = (string?)null },
                new { preguntaId = 1003, opcionId = SaedApiFactory.OpcionOtraPreguntaId, texto = (string?)null },
                new { preguntaId = SaedApiFactory.PreguntaAbiertaId, opcionId = (int?)null, texto = "Abierta" }
            },
            comentario = "Comentario"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await factory.CountEvaluaciones());
    }

    [Fact]
    public async Task No_puede_enviar_respuestas_duplicadas()
    {
        using var factory = new SaedApiFactory();
        using var client = factory.CreateClientForRole("Alumno");

        var response = await client.PostAsJsonAsync("/api/evaluaciones", new
        {
            asignacionAcademicaId = SaedApiFactory.AssignmentId,
            instrumentoId = SaedApiFactory.InstrumentoCurricularId,
            respuestas = new object[]
            {
                new { preguntaId = SaedApiFactory.PreguntaCerradaId, opcionId = SaedApiFactory.OpcionCerradaId, texto = (string?)null },
                new { preguntaId = SaedApiFactory.PreguntaCerradaId, opcionId = SaedApiFactory.OpcionCerradaId, texto = (string?)null },
                new { preguntaId = 1003, opcionId = SaedApiFactory.OpcionOtraPreguntaId, texto = (string?)null },
                new { preguntaId = SaedApiFactory.PreguntaAbiertaId, opcionId = (int?)null, texto = "Abierta" }
            },
            comentario = "Comentario"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await factory.CountEvaluaciones());
    }

    [Fact]
    public async Task No_puede_omitir_preguntas_obligatorias()
    {
        using var factory = new SaedApiFactory();
        using var client = factory.CreateClientForRole("Alumno");

        var response = await client.PostAsJsonAsync("/api/evaluaciones", new
        {
            asignacionAcademicaId = SaedApiFactory.AssignmentId,
            instrumentoId = SaedApiFactory.InstrumentoCurricularId,
            respuestas = new object[]
            {
                new { preguntaId = SaedApiFactory.PreguntaCerradaId, opcionId = SaedApiFactory.OpcionCerradaId, texto = (string?)null },
                new { preguntaId = 1003, opcionId = SaedApiFactory.OpcionOtraPreguntaId, texto = (string?)null }
            },
            comentario = "Comentario"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await factory.CountEvaluaciones());
    }

    [Fact]
    public async Task Intento_repetido_devuelve_409()
    {
        using var factory = new SaedApiFactory();
        using var client = factory.CreateClientForRole("Alumno");

        var first = await client.PostAsJsonAsync("/api/evaluaciones", SaedApiFactory.EvaluacionValidaPayload());
        var second = await client.PostAsJsonAsync("/api/evaluaciones", SaedApiFactory.EvaluacionValidaPayload());

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(1, await factory.CountEvaluaciones());
    }

    [Fact]
    public async Task Dos_solicitudes_concurrentes_no_crean_duplicados_ni_evaluacion_vacia()
    {
        using var factory = new SaedApiFactory();
        using var client = factory.CreateClientForRole("Alumno");

        var first = client.PostAsJsonAsync("/api/evaluaciones", SaedApiFactory.EvaluacionValidaPayload("Concurrente"));
        var second = client.PostAsJsonAsync("/api/evaluaciones", SaedApiFactory.EvaluacionValidaPayload("Concurrente"));
        var responses = await Task.WhenAll(first, second);
        var statuses = responses.Select(r => r.StatusCode).ToList();

        Assert.Contains(HttpStatusCode.OK, statuses);
        Assert.Contains(HttpStatusCode.Conflict, statuses);
        Assert.Equal(1, await factory.CountEvaluaciones());

        var evaluacion = await factory.GetSingleEvaluacion();
        Assert.NotNull(evaluacion);
        Assert.Equal(3, evaluacion!.Respuestas.Count);
    }

    private static object[] RespuestasValidas()
    {
        return new object[]
        {
            new { preguntaId = SaedApiFactory.PreguntaCerradaId, opcionId = SaedApiFactory.OpcionCerradaId, texto = (string?)null },
            new { preguntaId = 1003, opcionId = SaedApiFactory.OpcionOtraPreguntaId, texto = (string?)null },
            new { preguntaId = SaedApiFactory.PreguntaAbiertaId, opcionId = (int?)null, texto = "Respuesta abierta" }
        };
    }
}
