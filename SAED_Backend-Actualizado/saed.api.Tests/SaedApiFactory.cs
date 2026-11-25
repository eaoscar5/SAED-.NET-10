using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using saed.api.Data;
using saed.api.DTOs.Universidad;
using saed.api.Model;
using saed.api.Services.Universidad;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace saed.api.Tests;

public sealed class SaedApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"saed-api-tests-{Guid.NewGuid():N}.db");

    public const string JwtKey = "test_jwt_secret_key_for_saed_security_tests_123456";
    public const string Issuer = "saed.api";
    public const string Audience = "saed.api.users";
    public const string StudentMatricula = "A12345678";
    public const string StudentName = "Alumno Prueba";
    public const string AssignmentId = "G1|M1|MAT1";

    public const int InstrumentoCurricularId = 100;
    public const int InstrumentoAjenoId = 200;
    public const int PreguntaCerradaId = 1001;
    public const int PreguntaAbiertaId = 1002;
    public const int OpcionCerradaId = 1101;
    public const int OpcionOtraPreguntaId = 1201;
    public const int PreguntaOtroInstrumentoId = 2001;
    public const int OpcionOtroInstrumentoId = 2101;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_databasePath}",
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["UniversidadApi:BaseUrl"] = "http://siuts.uts.edu.mx/apienlinea/api",
                ["UniversidadApi:ServiceUsername"] = "svc-test",
                ["UniversidadApi:ServicePassword"] = "svc-test-password",
                ["UniversidadApi:AdminUsernames:0"] = "admin",
                ["SeedAdmin:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IUniversidadApiService>();

            services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={_databasePath}"));
            services.AddSingleton<IUniversidadApiService, FakeUniversidadApiService>();

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            Seed(context);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    public HttpClient CreateClientForRole(string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(role));
        return client;
    }

    public static string CreateToken(string role, string? nameIdentifier = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, nameIdentifier ?? (role == "Alumno" ? StudentMatricula : "admin")),
            new(ClaimTypes.Role, role)
        };

        if (role == "Alumno")
        {
            claims.Add(new Claim(ClaimTypes.Name, StudentName));
            claims.Add(new Claim("token_universidad", "token-universidad-test"));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Email, "admin"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<int> CountEvaluaciones()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.Evaluaciones.CountAsync();
    }

    public async Task<Evaluacion?> GetSingleEvaluacion()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.Evaluaciones
            .Include(e => e.Respuestas)
            .SingleOrDefaultAsync();
    }

    public static object EvaluacionValidaPayload(string? textoAbierto = "Respuesta abierta válida")
    {
        return new
        {
            asignacionAcademicaId = AssignmentId,
            instrumentoId = InstrumentoCurricularId,
            idMaestro = "M999",
            nombreMaestro = "Maestro falsificado",
            materiaId = "MAT999",
            nombreMateria = "Materia falsificada",
            grupo = "G999",
            respuestas = new object[]
            {
                new { preguntaId = PreguntaCerradaId, opcionId = OpcionCerradaId, texto = (string?)null },
                new { preguntaId = 1003, opcionId = OpcionOtraPreguntaId, texto = (string?)null },
                new { preguntaId = PreguntaAbiertaId, opcionId = (int?)null, texto = textoAbierto }
            },
            comentario = "Comentario válido"
        };
    }

    private static void Seed(AppDbContext context)
    {
        var instrumento = new Instrumento
        {
            Id = InstrumentoCurricularId,
            Nombre = "Instrumento curricular",
            TipoInstrumento = TipoInstrumento.Curricular,
            Activo = true,
            Categorias =
            {
                new Categoria
                {
                    Id = 1000,
                    Nombre = "Categoría",
                    Orden = 1,
                    Preguntas =
                    {
                        new Pregunta
                        {
                            Id = PreguntaCerradaId,
                            Texto = "Pregunta cerrada",
                            Tipo = PreguntaTipo.Cerrada,
                            Orden = 1,
                            Opciones =
                            {
                                new OpcionRespuesta { Id = OpcionCerradaId, Texto = "Siempre", Valor = 5 },
                                new OpcionRespuesta { Id = 1102, Texto = "Nunca", Valor = 1 }
                            }
                        },
                        new Pregunta
                        {
                            Id = PreguntaAbiertaId,
                            Texto = "Pregunta abierta",
                            Tipo = PreguntaTipo.Abierta,
                            Orden = 2
                        },
                        new Pregunta
                        {
                            Id = 1003,
                            Texto = "Pregunta cerrada alternativa",
                            Tipo = PreguntaTipo.Cerrada,
                            Orden = 3,
                            Opciones =
                            {
                                new OpcionRespuesta { Id = OpcionOtraPreguntaId, Texto = "Opción de otra pregunta", Valor = 3 }
                            }
                        }
                    }
                }
            }
        };

        var instrumentoAjeno = new Instrumento
        {
            Id = InstrumentoAjenoId,
            Nombre = "Instrumento ajeno",
            TipoInstrumento = TipoInstrumento.Curricular,
            Activo = true,
            Categorias =
            {
                new Categoria
                {
                    Id = 2000,
                    Nombre = "Categoría ajena",
                    Orden = 1,
                    Preguntas =
                    {
                        new Pregunta
                        {
                            Id = PreguntaOtroInstrumentoId,
                            Texto = "Pregunta de otro instrumento",
                            Tipo = PreguntaTipo.Cerrada,
                            Orden = 1,
                            Opciones =
                            {
                                new OpcionRespuesta { Id = OpcionOtroInstrumentoId, Texto = "Siempre", Valor = 5 }
                            }
                        }
                    }
                }
            }
        };

        context.Instrumentos.AddRange(instrumento, instrumentoAjeno);
        context.Procesos.Add(new Proceso
        {
            Id = 10,
            Nombre = "Oficial prueba",
            Anio = 2026,
            Periodo = 1,
            FechaInicio = new DateOnly(2026, 1, 1),
            FechaFin = new DateOnly(2026, 12, 31),
            Activo = true,
            InstrumentoId = InstrumentoCurricularId,
            ProcesoInstrumentos =
            {
                new ProcesoInstrumento
                {
                    InstrumentoId = InstrumentoCurricularId,
                    Orden = 1
                }
            }
        });

        context.Academias.Add(new Academia
        {
            Id = 1,
            AcaClave = "ACA",
            Nombre = "Academia prueba",
            EsExtracurricular = false
        });

        context.MaestrosAcademia.Add(new MaestroAcademia
        {
            Id = 1,
            AcaClave = "ACA",
            NombreAcademia = "Academia prueba",
            IdMaestro = "M1",
            NombreMaestro = "Maestro Uno"
        });

        context.MateriasCatalogo.Add(new MateriaCatalogo
        {
            Id = 1,
            MateriaId = "MAT1",
            NombreMateria = "Materia Uno",
            TipoMateria = 1,
            NaturalezaNoCurricular = false,
            CarreraClave = "CAR"
        });

        context.Usuarios.Add(new Usuario
        {
            Id = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminSeguro123*"),
            Rol = "Admin"
        });

        context.SaveChanges();
    }

    private sealed class FakeUniversidadApiService : IUniversidadApiService
    {
        public Task<string> Authenticate(string username, string password)
        {
            return Task.FromResult("token-universidad-test");
        }

        public Task<AlumnoUniversidad?> GetStudentData(string token, string matricula)
        {
            return Task.FromResult<AlumnoUniversidad?>(new AlumnoUniversidad
            {
                Matricula = matricula,
                Nombre = "Alumno",
                ApellidoPaterno = "Prueba"
            });
        }

        public Task<List<MateriaAlumnoDto>> GetStudentSubjects(
            string? token,
            string matricula,
            int anio,
            int periodo)
        {
            var materias = matricula == StudentMatricula
                ? new List<MateriaAlumnoDto>
                {
                    new()
                    {
                        Grupo = "G1",
                        NombreGrupo = "Grupo 1",
                        IdMaestro = "M1",
                        NombreMaestro = "Maestro Uno",
                        IdMateria = "MAT1",
                        Materia = "Materia Uno"
                    }
                }
                : new List<MateriaAlumnoDto>();

            return Task.FromResult(materias);
        }

        public Task<JsonElement> GetCuatrimestres(string token)
        {
            return Task.FromResult(JsonDocument.Parse("[]").RootElement);
        }

        public Task<bool> CanReadCatalogs(string token)
        {
            return Task.FromResult(true);
        }

        public Task<List<AcademiaUniversidadDto>> GetAcademias(string? token = null)
        {
            return Task.FromResult(new List<AcademiaUniversidadDto>());
        }

        public Task<List<MaestroAcademiaUniversidadDto>> GetMaestrosPorAcademia(string acaClave, string? token = null)
        {
            return Task.FromResult(new List<MaestroAcademiaUniversidadDto>());
        }

        public Task<List<MaestroAcademiaUniversidadDto>> GetTodosMaestrosPorAcademia(string? token = null)
        {
            return Task.FromResult(new List<MaestroAcademiaUniversidadDto>());
        }

        public Task<List<MateriaUniversidadDto>> GetMaterias(string? token = null)
        {
            return Task.FromResult(new List<MateriaUniversidadDto>());
        }

        public Task<List<GrupoActivoUniversidadDto>> GetGruposActivosPorPeriodo(int anio, int periodo, string? token = null)
        {
            return Task.FromResult(new List<GrupoActivoUniversidadDto>());
        }

        public Task<List<MateriaMaestroGrupoUniversidadDto>> GetMateriasMaestroPorGrupo(
            string grupo,
            int anio,
            int periodo,
            string? token = null)
        {
            return Task.FromResult(new List<MateriaMaestroGrupoUniversidadDto>());
        }

        public Task<List<AlumnoGrupoPeriodoUniversidadDto>> GetAlumnosPorGrupoPeriodo(
            int anio,
            int periodo,
            string? token = null)
        {
            return Task.FromResult(new List<AlumnoGrupoPeriodoUniversidadDto>());
        }
    }
}
