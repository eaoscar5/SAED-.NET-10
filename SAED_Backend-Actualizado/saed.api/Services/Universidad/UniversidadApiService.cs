using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using saed.api.DTOs.Universidad;
using System.Net;
using System.Text;
using System.Text.Json;

namespace saed.api.Services.Universidad
{
    public class UniversidadApiService : IUniversidadApiService
    {
        private const string ServiceTokenCacheKey = "UniversidadApi:ServiceToken";
        private static readonly SemaphoreSlim ServiceTokenLock = new(1, 1);

        private readonly HttpClient _http;
        private readonly UniversidadApiOptions _options;
        private readonly IMemoryCache _cache;

        public UniversidadApiService(
            HttpClient http,
            IOptions<UniversidadApiOptions> options,
            IMemoryCache cache)
        {
            _http = http;
            _options = options.Value;
            _cache = cache;
        }

        private HttpRequestMessage CreateRequest(string endpoint, object body, string? token = null)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}");

            if (!string.IsNullOrEmpty(token))
                request.Headers.Add("Authorization", $"Bearer {token}");

            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            return request;
        }

        private async Task<JsonElement> MakeRequest(string endpoint, object body, string? token = null)
        {
            using var request = CreateRequest(endpoint, body, token);
            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Error llamando API universidad: {response.StatusCode}",
                    null,
                    response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<JsonElement>(json);
        }

        private async Task<JsonElement> MakeServiceRequest(string endpoint, object body)
        {
            var token = await GetServiceToken();

            try
            {
                return await MakeRequest(endpoint, body, token);
            }
            catch (HttpRequestException ex) when (IsAuthenticationFailure(ex.StatusCode))
            {
                InvalidateServiceToken(token);
                return await MakeRequest(endpoint, body, await GetServiceToken());
            }
        }

        private async Task<JsonElement> MakeCatalogRequest(string endpoint, object body, string? token = null)
        {
            return string.IsNullOrWhiteSpace(token)
                ? await MakeServiceRequest(endpoint, body)
                : await MakeRequest(endpoint, body, token);
        }

        private async Task<string> GetServiceToken()
        {
            if (_cache.TryGetValue(ServiceTokenCacheKey, out string? cachedToken) &&
                !string.IsNullOrWhiteSpace(cachedToken))
                return cachedToken;

            await ServiceTokenLock.WaitAsync();
            try
            {
                if (_cache.TryGetValue(ServiceTokenCacheKey, out cachedToken) &&
                    !string.IsNullOrWhiteSpace(cachedToken))
                    return cachedToken;

                if (string.IsNullOrWhiteSpace(_options.ServiceUsername) ||
                    string.IsNullOrWhiteSpace(_options.ServicePassword))
                {
                    throw new InvalidOperationException(
                        "No hay credenciales de servicio configuradas para UniversidadApi.");
                }

                var token = await Authenticate(_options.ServiceUsername, _options.ServicePassword);
                var cacheSeconds = Math.Max(5, _options.ServiceTokenSeconds - 2);
                _cache.Set(
                    ServiceTokenCacheKey,
                    token,
                    TimeSpan.FromSeconds(cacheSeconds));

                return token;
            }
            finally
            {
                ServiceTokenLock.Release();
            }
        }

        private void InvalidateServiceToken(string failedToken)
        {
            if (_cache.TryGetValue(ServiceTokenCacheKey, out string? cachedToken) &&
                string.Equals(cachedToken, failedToken, StringComparison.Ordinal))
            {
                _cache.Remove(ServiceTokenCacheKey);
            }
        }

        private static bool IsAuthenticationFailure(HttpStatusCode? statusCode)
        {
            return statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
        }

        public async Task<string> Authenticate(string username, string password)
        {
            var result = await MakeRequest(
                "login/authenticate",
                new
                {
                    Username = username,
                    Password = password
                });

            return result.GetString()!;
        }

        public async Task<AlumnoUniversidad?> GetStudentData(string token, string matricula)
        {
            var data = await MakeRequest(
                "buscar",
                new
                {
                    sp = "Bus_DatosAlumno",
                    parametros = new[] { matricula }
                },
                token);

            if (data.ValueKind != JsonValueKind.Array || data.GetArrayLength() == 0)
                return null;

            var alumno = data[0];

            return new AlumnoUniversidad
            {
                Matricula = GetString(alumno, 0),
                Nombre = GetString(alumno, 1),
                ApellidoPaterno = GetString(alumno, 2),
                ApellidoMaterno = GetString(alumno, 3),
                Correo = GetString(alumno, 13)
            };
        }

        public async Task<List<MateriaAlumnoDto>> GetStudentSubjects(
            string? token,
            string matricula,
            int anio,
            int periodo)
        {
            var body = new
            {
                sp = "Lst_GrupoMateriaMaestroxMatriculaLinea",
                parametros = new object[] { matricula, anio, periodo }
            };

            var data = string.IsNullOrWhiteSpace(token)
                ? await MakeServiceRequest("buscar", body)
                : await MakeRequest("buscar", body, token);

            var result = new List<MateriaAlumnoDto>();

            foreach (var item in data.EnumerateArray())
            {
                result.Add(new MateriaAlumnoDto
                {
                    Grupo = GetString(item, 0),
                    NombreGrupo = GetString(item, 1),
                    IdMaestro = GetString(item, 2),
                    NombreMaestro = GetString(item, 3),
                    IdMateria = GetString(item, 4),
                    Materia = GetString(item, 5)
                });
            }

            return DeduplicarMateriasAlumno(result);
        }

        public async Task<JsonElement> GetCuatrimestres(string token)
        {
            return await MakeRequest(
                "buscar",
                new
                {
                    sp = "Lst_Cuatrimestres",
                    parametros = new object[] { -1 }
                },
                token);
        }

        public async Task<bool> CanReadCatalogs(string token)
        {
            var academias = await GetAcademias(token);
            return academias.Any();
        }

        public async Task<List<AcademiaUniversidadDto>> GetAcademias(string? token = null)
        {
            var data = await MakeCatalogRequest(
                "buscar",
                new
                {
                    sp = "ObtenerAcademias_Linea",
                    parametros = Array.Empty<object>()
                },
                token);

            return data.EnumerateArray()
                .Select(item => new AcademiaUniversidadDto
                {
                    AcaClave = GetString(item, 0),
                    Nombre = GetString(item, 1)
                })
                .ToList();
        }

        public async Task<List<MaestroAcademiaUniversidadDto>> GetMaestrosPorAcademia(
            string acaClave,
            string? token = null)
        {
            var data = await MakeCatalogRequest(
                "buscar",
                new
                {
                    sp = "MaestrosPorAcademia_Linea",
                    parametros = new object[] { acaClave }
                },
                token);

            return MapMaestrosAcademia(data);
        }

        public async Task<List<MaestroAcademiaUniversidadDto>> GetTodosMaestrosPorAcademia(string? token = null)
        {
            var data = await MakeCatalogRequest(
                "buscar",
                new
                {
                    sp = "TodosMaestrosPorAcademia_Linea",
                    parametros = Array.Empty<object>()
                },
                token);

            return MapMaestrosAcademia(data);
        }

        public async Task<List<MateriaUniversidadDto>> GetMaterias(string? token = null)
        {
            var data = await MakeCatalogRequest(
                "buscar",
                new
                {
                    sp = "Bus_MateriasLinea",
                    parametros = new object[] { "%" }
                },
                token);

            return data.EnumerateArray()
                .Select(item => new MateriaUniversidadDto
                {
                    MateriaId = GetString(item, 0),
                    NombreMateria = GetString(item, 1),
                    HorasTotales = GetInt(item, 2),
                    NaturalezaNoCurricular = GetBool(item, 3),
                    TipoMateria = GetInt(item, 4),
                    Seriacion = GetString(item, 5),
                    CarreraClave = GetString(item, 6),
                    NumeroCompetencias = GetInt(item, 7),
                    MateriaIntegradora = GetBool(item, 8)
                })
                .ToList();
        }

        public async Task<List<GrupoActivoUniversidadDto>> GetGruposActivosPorPeriodo(
            int anio,
            int periodo,
            string? token = null)
        {
            var data = await MakeCatalogRequest(
                "buscar",
                new
                {
                    sp = "sp_GruposActivosPorPeriodo_Linea",
                    parametros = new object[] { anio, periodo }
                },
                token);

            return data.EnumerateArray()
                .Select(item => new GrupoActivoUniversidadDto
                {
                    Grupo = GetString(item, 0),
                    NombreGrupo = GetString(item, 1),
                    Cuatrimestre = GetInt(item, 2),
                    CarreraClave = GetString(item, 3),
                    CarreraNombre = GetString(item, 4),
                    AcademiaClave = GetString(item, 5),
                    AcademiaNombre = GetString(item, 6),
                    TotalAlumnos = GetInt(item, 7)
                })
                .Where(g => !string.IsNullOrWhiteSpace(g.Grupo))
                .ToList();
        }

        public async Task<List<MateriaMaestroGrupoUniversidadDto>> GetMateriasMaestroPorGrupo(
            string grupo,
            int anio,
            int periodo,
            string? token = null)
        {
            var data = await MakeCatalogRequest(
                "buscar",
                new
                {
                    sp = "Bus_MateriasMaePorGrupo_Linea",
                    parametros = new object[] { grupo, anio, periodo }
                },
                token);

            return data.EnumerateArray()
                .Select(item => new MateriaMaestroGrupoUniversidadDto
                {
                    IdMaestro = GetString(item, 0),
                    NombreMaestro = GetString(item, 1),
                    MateriaId = GetString(item, 2),
                    NombreMateria = GetString(item, 3),
                    Grupo = GetString(item, 4),
                    NombreGrupo = GetString(item, 5),
                    Anio = GetInt(item, 6),
                    Periodo = GetInt(item, 7)
                })
                .Where(m =>
                    !string.IsNullOrWhiteSpace(m.Grupo) &&
                    !string.IsNullOrWhiteSpace(m.IdMaestro) &&
                    !string.IsNullOrWhiteSpace(m.MateriaId))
                .GroupBy(m => new
                {
                    Grupo = NormalizarKey(m.Grupo),
                    IdMaestro = NormalizarKey(m.IdMaestro),
                    MateriaId = NormalizarKey(m.MateriaId)
                })
                .Select(g => g.First())
                .ToList();
        }

        public async Task<List<AlumnoGrupoPeriodoUniversidadDto>> GetAlumnosPorGrupoPeriodo(
            int anio,
            int periodo,
            string? token = null)
        {
            var data = await MakeCatalogRequest(
                "buscar",
                new
                {
                    sp = "sp_AlumnosPorGrupoPeriodo_Linea",
                    parametros = new object[] { anio, periodo }
                },
                token);

            return data.EnumerateArray()
                .Select(item => new AlumnoGrupoPeriodoUniversidadDto
                {
                    AcademiaClave = GetString(item, 0),
                    AcademiaNombre = GetString(item, 1),
                    CarreraClave = GetString(item, 2),
                    CarreraNombre = GetString(item, 3),
                    Grupo = GetString(item, 4),
                    NombreGrupo = GetString(item, 5),
                    Cuatrimestre = GetInt(item, 6),
                    Matricula = GetString(item, 7),
                    ApellidoPaterno = GetString(item, 8),
                    ApellidoMaterno = GetString(item, 9),
                    Nombre = GetString(item, 10)
                })
                .Where(a =>
                    !string.IsNullOrWhiteSpace(a.Grupo) &&
                    !string.IsNullOrWhiteSpace(a.Matricula))
                .GroupBy(a => new
                {
                    Grupo = NormalizarKey(a.Grupo),
                    Matricula = NormalizarKey(a.Matricula)
                })
                .Select(g => g.First())
                .ToList();
        }

        private static List<MaestroAcademiaUniversidadDto> MapMaestrosAcademia(JsonElement data)
        {
            return data.EnumerateArray()
                .Select(item => new MaestroAcademiaUniversidadDto
                {
                    AcaClave = GetString(item, 0),
                    NombreAcademia = GetString(item, 1),
                    IdMaestro = GetString(item, 2),
                    NombreMaestro = GetString(item, 3)
                })
                .ToList();
        }

        private static List<MateriaAlumnoDto> DeduplicarMateriasAlumno(IEnumerable<MateriaAlumnoDto> materias)
        {
            var result = new List<MateriaAlumnoDto>();
            var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var materia in materias)
            {
                var key = string.Join(
                    "|",
                    NormalizarKey(materia.Grupo),
                    NormalizarKey(materia.IdMaestro),
                    NormalizarKey(materia.IdMateria));

                if (vistas.Add(key))
                    result.Add(materia);
            }

            return result;
        }

        private static string NormalizarKey(string value)
        {
            return value.Trim().ToUpperInvariant();
        }

        private static string GetString(JsonElement item, int index)
        {
            if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() <= index)
                return string.Empty;

            return item[index].ToString().Trim();
        }

        private static int GetInt(JsonElement item, int index)
        {
            var value = GetString(item, index);
            return int.TryParse(value, out var result) ? result : 0;
        }

        private static bool GetBool(JsonElement item, int index)
        {
            var value = GetString(item, index);

            if (bool.TryParse(value, out var boolResult))
                return boolResult;

            return value == "1";
        }
    }
}
