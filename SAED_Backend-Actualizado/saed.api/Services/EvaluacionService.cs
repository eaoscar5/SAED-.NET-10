using Microsoft.EntityFrameworkCore;
using Npgsql;
using saed.api.Data;
using saed.api.DTOs.Evaluaciones;
using saed.api.DTOs.Universidad;
using saed.api.Model;
using saed.api.Security;
using saed.api.Services.Universidad;
using System.Net;
using System.Security.Cryptography;
using System.Security.Claims;

namespace saed.api.Services
{
    public class EvaluacionService
    {
        private const int TextoRespuestaAbiertaMaxLength = 2000;
        private const int ComentarioMaxLength = 2000;

        private readonly AppDbContext _context;
        private readonly IUniversidadApiService _universidadApi;
        private readonly CatalogoUniversidadService _catalogoUniversidad;
        private readonly IHttpContextAccessor _httpContext;

        public EvaluacionService(
            AppDbContext context,
            IUniversidadApiService universidadApi,
            CatalogoUniversidadService catalogoUniversidad,
            IHttpContextAccessor httpContext)
        {
            _context = context;
            _universidadApi = universidadApi;
            _catalogoUniversidad = catalogoUniversidad;
            _httpContext = httpContext;
        }

        public async Task<EvaluacionCreadaDto> CrearEvaluacion(CrearEvaluacionDto dto)
        {
            var matricula = ObtenerMatricula();
            ValidarContratoBasico(dto);

            var proceso = await ObtenerProcesoActivoConInstrumentos();
            var asignacionValidada = await ObtenerAsignacionValidada(
                matricula,
                proceso,
                dto.AsignacionAcademicaId,
                dto.InstrumentoId);
            var asignacion = asignacionValidada.Asignacion;

            var respuestas = await ValidarRespuestasContraInstrumento(
                dto.Respuestas,
                dto.InstrumentoId);

            var comentario = NormalizarComentario(dto.Comentario);

            var existe = await _context.Evaluaciones.AnyAsync(e =>
                e.ProcesoId == proceso.Id &&
                e.MatriculaAlumno == matricula &&
                e.AsignacionAcademicaId == asignacion.AsignacionAcademicaId &&
                e.InstrumentoId == dto.InstrumentoId);

            if (existe)
                throw new ApiConflictException("Ya registraste esta evaluación.");

            var evaluacion = new Evaluacion
            {
                ProcesoId = proceso.Id,
                MatriculaAlumno = matricula,
                AsignacionAcademicaId = asignacion.AsignacionAcademicaId,
                IdMaestro = asignacion.Materia.IdMaestro,
                NombreMaestro = asignacion.Materia.NombreMaestro,
                MateriaId = asignacion.Materia.IdMateria,
                NombreMateria = asignacion.Materia.Materia,
                Grupo = asignacion.Materia.Grupo,
                InstrumentoId = asignacion.Clasificacion.InstrumentoId,
                TipoInstrumento = asignacion.Clasificacion.TipoInstrumento,
                AcademiaClave = asignacion.Clasificacion.AcademiaClave,
                AcademiaNombre = asignacion.Clasificacion.AcademiaNombre,
                CarreraClave = asignacion.Clasificacion.CarreraClave,
                TipoMateria = asignacion.Clasificacion.TipoMateria,
                MateriaNoCurricular = asignacion.Clasificacion.MateriaNoCurricular,
                Fecha = DateTime.UtcNow,
                Respuestas = respuestas
            };

            if (!string.IsNullOrWhiteSpace(comentario))
            {
                evaluacion.Comentario = new Comentario
                {
                    Texto = comentario
                };
            }

            _context.Evaluaciones.Add(evaluacion);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (EsViolacionUnicidad(ex))
            {
                throw new ApiConflictException("Ya registraste esta evaluación.");
            }

            var finalizacion = await ObtenerFinalizacion(
                proceso,
                matricula,
                asignacionValidada.Asignaciones);

            return new EvaluacionCreadaDto
            {
                Id = evaluacion.Id,
                Fecha = evaluacion.Fecha,
                Finalizacion = finalizacion
            };
        }

        public async Task<List<EvaluacionDisponibleDto>> ObtenerDisponibles()
        {
            var matricula = ObtenerMatricula();
            var proceso = await ObtenerProcesoActivoConInstrumentos();
            var materias = await ObtenerMateriasAlumno(matricula, proceso);
            var asignaciones = await ConstruirAsignaciones(materias, proceso);

            var evaluadas = await _context.Evaluaciones
                .Where(e => e.MatriculaAlumno == matricula && e.ProcesoId == proceso.Id)
                .Select(e => new { e.AsignacionAcademicaId, e.InstrumentoId })
                .ToListAsync();

            return asignaciones
                .Select(a => new EvaluacionDisponibleDto
                {
                    AsignacionAcademicaId = a.AsignacionAcademicaId,
                    IdMaestro = a.Materia.IdMaestro,
                    NombreMaestro = a.Materia.NombreMaestro,
                    MateriaId = a.Materia.IdMateria,
                    Materia = a.Materia.Materia,
                    Grupo = a.Materia.Grupo,
                    InstrumentoId = a.Clasificacion.InstrumentoId,
                    TipoInstrumento = a.Clasificacion.TipoInstrumento,
                    AcademiaClave = a.Clasificacion.AcademiaClave,
                    AcademiaNombre = a.Clasificacion.AcademiaNombre,
                    CarreraClave = a.Clasificacion.CarreraClave,
                    TipoMateria = a.Clasificacion.TipoMateria,
                    MateriaNoCurricular = a.Clasificacion.MateriaNoCurricular,
                    YaEvaluado = a.Clasificacion.InstrumentoId.HasValue &&
                        evaluadas.Any(e =>
                            e.AsignacionAcademicaId == a.AsignacionAcademicaId &&
                            e.InstrumentoId == a.Clasificacion.InstrumentoId)
                })
                .ToList();
        }

        public async Task<FinalizacionEvaluacionesDto> ObtenerFinalizacionActual()
        {
            var matricula = ObtenerMatricula();
            var proceso = await ObtenerProcesoActivoConInstrumentos();
            var materias = await ObtenerMateriasAlumno(matricula, proceso);
            var asignaciones = await ConstruirAsignaciones(materias, proceso);

            return await ObtenerFinalizacion(proceso, matricula, asignaciones);
        }

        public async Task<ReporteMaestroDto> ObtenerReporteMaestro(string idMaestro, int procesoId)
        {
            var evaluaciones = await _context.Evaluaciones
                .Where(e => e.IdMaestro == idMaestro && e.ProcesoId == procesoId)
                .Include(e => e.Respuestas)
                    .ThenInclude(r => r.Pregunta)
                .Include(e => e.Respuestas)
                    .ThenInclude(r => r.Opcion)
                .Include(e => e.Comentario)
                .ToListAsync();

            if (!evaluaciones.Any())
                throw new Exception("No hay evaluaciones para este maestro en el proceso indicado.");

            var proceso = await _context.Procesos.FindAsync(procesoId)
                ?? throw new Exception("Proceso no encontrado.");

            var respuestasCerradas = evaluaciones
                .SelectMany(e => e.Respuestas)
                .Where(r => r.Opcion != null)
                .ToList();

            var preguntasAgrupadas = respuestasCerradas
                .GroupBy(r => r.PreguntaId)
                .Select(g =>
                {
                    var primero = g.First();
                    var promedio = g.Average(r => r.Opcion?.Valor ?? 0);
                    var distribucion = g
                        .GroupBy(r => r.Opcion?.Texto ?? "Sin texto")
                        .ToDictionary(d => d.Key, d => d.Count());

                    return new ReportePreguntaDto
                    {
                        PreguntaId = g.Key,
                        TextoPregunta = primero.Pregunta?.Texto ?? "Pregunta eliminada",
                        Promedio = Math.Round(promedio, 2),
                        Distribucion = distribucion
                    };
                })
                .ToList();

            var comentarios = evaluaciones
                .Where(e => e.Comentario != null && !string.IsNullOrWhiteSpace(e.Comentario.Texto))
                .Select(e => e.Comentario!.Texto)
                .ToList();

            return new ReporteMaestroDto
            {
                IdMaestro = idMaestro,
                NombreMaestro = evaluaciones.First().NombreMaestro,
                ProcesoId = procesoId,
                NombreProceso = proceso.Nombre,
                TotalEvaluaciones = evaluaciones.Count,
                Preguntas = preguntasAgrupadas,
                Comentarios = comentarios
            };
        }

        public async Task<List<ResumenMaestroDto>> ListarMaestrosPorProceso(int procesoId)
        {
            return await _context.Evaluaciones
                .Where(e => e.ProcesoId == procesoId)
                .GroupBy(e => new { e.IdMaestro, e.NombreMaestro })
                .Select(g => new ResumenMaestroDto
                {
                    IdMaestro = g.Key.IdMaestro,
                    NombreMaestro = g.Key.NombreMaestro,
                    TotalEvaluaciones = g.Count()
                })
                .OrderBy(r => r.NombreMaestro)
                .ToListAsync();
        }

        public async Task<ResumenProcesoDto> ObtenerResumenProceso(int procesoId)
        {
            return await _context.Evaluaciones
                .Where(e => e.ProcesoId == procesoId)
                .GroupBy(_ => 1)
                .Select(g => new ResumenProcesoDto
                {
                    MaestrosEvaluados = g.Select(e => e.IdMaestro).Distinct().Count(),
                    AlumnosQueEvaluaron = g.Select(e => e.MatriculaAlumno).Distinct().Count(),
                    EvaluacionesTotales = g.Count()
                })
                .SingleOrDefaultAsync()
                ?? new ResumenProcesoDto();
        }

        public static string CrearAsignacionAcademicaId(MateriaAlumnoDto materia)
        {
            return string.Join(
                "|",
                NormalizarIdentificador(materia.Grupo),
                NormalizarIdentificador(materia.IdMaestro),
                NormalizarIdentificador(materia.IdMateria));
        }

        private string ObtenerMatricula()
        {
            var value = _httpContext.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(value))
                throw new UnauthorizedAccessException("No se pudo obtener la matrícula del token.");

            return value.Trim();
        }

        private string ObtenerTokenUniversidad()
        {
            var value = _httpContext.HttpContext?.User.FindFirstValue(AuthPolicies.UniversidadTokenClaim);
            if (string.IsNullOrWhiteSpace(value))
                throw new UnauthorizedAccessException("No se encontró el token universitario en la sesión.");

            return value;
        }

        private static void ValidarContratoBasico(CrearEvaluacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.AsignacionAcademicaId))
                throw new ApiValidationException("La asignación académica es obligatoria.");

            if (dto.InstrumentoId <= 0)
                throw new ApiValidationException("El instrumento es obligatorio.");

            if (dto.Respuestas.Count == 0)
                throw new ApiValidationException("La evaluación no contiene respuestas.");
        }

        private async Task<AsignacionValidadaContexto> ObtenerAsignacionValidada(
            string matricula,
            Proceso proceso,
            string asignacionAcademicaId,
            int instrumentoId)
        {
            var materias = await ObtenerMateriasAlumno(matricula, proceso);
            var asignaciones = await ConstruirAsignaciones(materias, proceso);
            var normalizada = NormalizarAsignacionId(asignacionAcademicaId);

            var asignacion = asignaciones.FirstOrDefault(a =>
                a.AsignacionAcademicaId == normalizada &&
                a.Clasificacion.InstrumentoId == instrumentoId);

            if (asignacion == null)
                throw new ApiValidationException("La asignación enviada no pertenece al alumno autenticado o no corresponde al instrumento indicado.");

            return new AsignacionValidadaContexto(asignacion, asignaciones);
        }

        private async Task<List<AsignacionEvaluable>> ConstruirAsignaciones(
            List<MateriaAlumnoDto> materias,
            Proceso proceso)
        {
            var result = new List<AsignacionEvaluable>();

            foreach (var materia in materias)
            {
                ValidarMateriaAlumno(materia);

                var clasificacion = await _catalogoUniversidad.Clasificar(
                    proceso,
                    materia.IdMaestro,
                    materia.IdMateria,
                    materia.Materia);

                result.Add(new AsignacionEvaluable(
                    CrearAsignacionAcademicaId(materia),
                    materia,
                    clasificacion));
            }

            return result;
        }

        private async Task<FinalizacionEvaluacionesDto> ObtenerFinalizacion(
            Proceso proceso,
            string matricula,
            IReadOnlyCollection<AsignacionEvaluable> asignaciones)
        {
            var asignacionesEsperadas = asignaciones
                .Where(a => a.Clasificacion.InstrumentoId.HasValue)
                .Select(a => CrearClaveEvaluacion(
                    a.AsignacionAcademicaId,
                    a.Clasificacion.InstrumentoId!.Value))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var evaluadas = await _context.Evaluaciones
                .AsNoTracking()
                .Where(e => e.ProcesoId == proceso.Id && e.MatriculaAlumno == matricula && e.InstrumentoId.HasValue)
                .Select(e => new { e.AsignacionAcademicaId, e.InstrumentoId })
                .ToListAsync();

            var evaluadasRegistradas = evaluadas
                .Where(e => e.InstrumentoId.HasValue)
                .Select(e => CrearClaveEvaluacion(e.AsignacionAcademicaId, e.InstrumentoId!.Value))
                .ToHashSet(StringComparer.Ordinal);

            var evaluacionesCompletadas = asignacionesEsperadas
                .Count(evaluacion => evaluadasRegistradas.Contains(evaluacion));
            var completada = asignacionesEsperadas.Count > 0 &&
                evaluacionesCompletadas == asignacionesEsperadas.Count;
            var grupo = ObtenerGrupoComprobante(asignaciones);

            ComprobanteFinalizacion? comprobante = null;
            if (completada)
                comprobante = await ObtenerOCrearComprobante(proceso, matricula, grupo);

            return new FinalizacionEvaluacionesDto
            {
                Completada = completada,
                Folio = comprobante?.Folio,
                FechaFinalizacion = comprobante?.FechaFinalizacion,
                TotalEvaluaciones = asignacionesEsperadas.Count,
                EvaluacionesCompletadas = evaluacionesCompletadas,
                Grupo = comprobante?.Grupo ?? grupo,
                NombreProceso = proceso.Nombre,
                Anio = proceso.Anio,
                Periodo = proceso.Periodo
            };
        }

        private async Task<ComprobanteFinalizacion> ObtenerOCrearComprobante(
            Proceso proceso,
            string matricula,
            string grupo)
        {
            var existente = await _context.ComprobantesFinalizacion
                .FirstOrDefaultAsync(c => c.ProcesoId == proceso.Id && c.MatriculaAlumno == matricula);

            if (existente != null)
                return existente;

            for (var intento = 0; intento < 5; intento++)
            {
                var comprobante = new ComprobanteFinalizacion
                {
                    ProcesoId = proceso.Id,
                    MatriculaAlumno = matricula,
                    Grupo = grupo,
                    Folio = await GenerarFolioUnico(proceso.Anio, proceso.Periodo),
                    FechaFinalizacion = DateTime.UtcNow
                };

                _context.ComprobantesFinalizacion.Add(comprobante);

                try
                {
                    await _context.SaveChangesAsync();
                    return comprobante;
                }
                catch (DbUpdateException ex) when (EsViolacionUnicidad(ex))
                {
                    _context.Entry(comprobante).State = EntityState.Detached;
                    existente = await _context.ComprobantesFinalizacion
                        .FirstOrDefaultAsync(c => c.ProcesoId == proceso.Id && c.MatriculaAlumno == matricula);

                    if (existente != null)
                        return existente;
                }
            }

            throw new ApiValidationException("No se pudo generar el folio de finalización. Inténtalo de nuevo.");
        }

        private async Task<string> GenerarFolioUnico(int anio, int periodo)
        {
            for (var intento = 0; intento < 5; intento++)
            {
                var aleatorio = Convert.ToHexString(RandomNumberGenerator.GetBytes(6));
                var folio = $"SAED-{anio:D4}-{periodo}-{aleatorio}";
                var existe = await _context.ComprobantesFinalizacion
                    .AsNoTracking()
                    .AnyAsync(c => c.Folio == folio);

                if (!existe)
                    return folio;
            }

            throw new ApiValidationException("No se pudo generar un folio único. Inténtalo de nuevo.");
        }

        private static string CrearClaveEvaluacion(string asignacionAcademicaId, int instrumentoId)
        {
            return $"{NormalizarAsignacionId(asignacionAcademicaId)}::{instrumentoId}";
        }

        private static string ObtenerGrupoComprobante(IReadOnlyCollection<AsignacionEvaluable> asignaciones)
        {
            var grupo = asignaciones
                .Select(a => a.Materia.Grupo?.Trim())
                .FirstOrDefault(grupo => !string.IsNullOrWhiteSpace(grupo))
                ?? string.Empty;

            return grupo.Length <= 120 ? grupo : grupo[..120];
        }

        private static void ValidarMateriaAlumno(MateriaAlumnoDto materia)
        {
            if (string.IsNullOrWhiteSpace(materia.Grupo) ||
                string.IsNullOrWhiteSpace(materia.IdMaestro) ||
                string.IsNullOrWhiteSpace(materia.IdMateria))
            {
                throw new ApiValidationException(
                    "La carga académica del alumno contiene una asignación incompleta.");
            }
        }

        private async Task<List<Respuesta>> ValidarRespuestasContraInstrumento(
            List<RespuestaDto> respuestas,
            int instrumentoId)
        {
            var duplicadas = respuestas
                .GroupBy(r => r.PreguntaId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicadas.Count > 0)
                throw new ApiValidationException("La evaluación contiene respuestas duplicadas.");

            var preguntas = await _context.Preguntas
                .AsNoTracking()
                .Where(p => p.Categoria.InstrumentoId == instrumentoId)
                .Select(p => new PreguntaValidacion(
                    p.Id,
                    p.Tipo,
                    p.Opciones.Select(o => o.Id).ToList()))
                .ToListAsync();

            if (preguntas.Count == 0)
                throw new ApiValidationException("El instrumento no existe o no contiene preguntas.");

            var preguntasPorId = preguntas.ToDictionary(p => p.Id);
            var respuestasPorPregunta = respuestas.ToDictionary(r => r.PreguntaId);

            var preguntasAjenas = respuestasPorPregunta.Keys
                .Where(id => !preguntasPorId.ContainsKey(id))
                .ToList();

            if (preguntasAjenas.Count > 0)
                throw new ApiValidationException("Una o más preguntas no pertenecen al instrumento indicado.");

            var preguntasFaltantes = preguntasPorId.Keys
                .Where(id => !respuestasPorPregunta.ContainsKey(id))
                .ToList();

            if (preguntasFaltantes.Count > 0)
                throw new ApiValidationException("Faltan respuestas obligatorias del instrumento.");

            var entidades = new List<Respuesta>();

            foreach (var pregunta in preguntas)
            {
                var respuesta = respuestasPorPregunta[pregunta.Id];

                if (pregunta.Tipo == PreguntaTipo.Cerrada)
                {
                    if (!respuesta.OpcionId.HasValue ||
                        !pregunta.OpcionIds.Contains(respuesta.OpcionId.Value))
                    {
                        throw new ApiValidationException(
                            "Una o más opciones no corresponden a las preguntas enviadas.");
                    }

                    if (!string.IsNullOrWhiteSpace(respuesta.Texto))
                        throw new ApiValidationException("Las preguntas cerradas no aceptan texto abierto.");

                    entidades.Add(new Respuesta
                    {
                        PreguntaId = pregunta.Id,
                        OpcionId = respuesta.OpcionId.Value
                    });
                }
                else if (pregunta.Tipo == PreguntaTipo.Abierta)
                {
                    var texto = respuesta.Texto?.Trim();

                    if (respuesta.OpcionId.HasValue)
                        throw new ApiValidationException("Las preguntas abiertas no aceptan opción cerrada.");

                    if (string.IsNullOrWhiteSpace(texto))
                        throw new ApiValidationException("Faltan respuestas abiertas obligatorias.");

                    if (texto.Length > TextoRespuestaAbiertaMaxLength)
                        throw new ApiValidationException($"Las respuestas abiertas no deben exceder {TextoRespuestaAbiertaMaxLength} caracteres.");

                    entidades.Add(new Respuesta
                    {
                        PreguntaId = pregunta.Id,
                        Texto = texto
                    });
                }
                else
                {
                    throw new ApiValidationException("Tipo de pregunta no soportado.");
                }
            }

            return entidades;
        }

        private async Task<Proceso> ObtenerProcesoActivoConInstrumentos()
        {
            return await _context.Procesos
                .Include(p => p.Instrumento)
                .Include(p => p.ProcesoInstrumentos)
                    .ThenInclude(pi => pi.Instrumento)
                .FirstOrDefaultAsync(p => p.Activo)
                ?? throw new ApiValidationException("No hay proceso activo.");
        }

        private async Task<List<MateriaAlumnoDto>> ObtenerMateriasAlumno(
            string matricula,
            Proceso proceso)
        {
            try
            {
                return await _universidadApi.GetStudentSubjects(
                    null,
                    matricula,
                    proceso.Anio,
                    proceso.Periodo);
            }
            catch (InvalidOperationException)
            {
                return await _universidadApi.GetStudentSubjects(
                    ObtenerTokenUniversidad(),
                    matricula,
                    proceso.Anio,
                    proceso.Periodo);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await _universidadApi.GetStudentSubjects(
                    ObtenerTokenUniversidad(),
                    matricula,
                    proceso.Anio,
                    proceso.Periodo);
            }
        }

        private static string? NormalizarComentario(string? comentario)
        {
            if (string.IsNullOrWhiteSpace(comentario))
                return null;

            var texto = comentario.Trim();

            if (texto.Length > ComentarioMaxLength)
                throw new ApiValidationException($"El comentario no debe exceder {ComentarioMaxLength} caracteres.");

            return texto;
        }

        private static string NormalizarAsignacionId(string value)
        {
            return string.Join(
                "|",
                value.Split('|', StringSplitOptions.TrimEntries)
                    .Select(NormalizarIdentificador));
        }

        private static string NormalizarIdentificador(string value)
        {
            return value.Trim().ToUpperInvariant();
        }

        private static bool EsViolacionUnicidad(DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException postgresException)
                return postgresException.SqlState == PostgresErrorCodes.UniqueViolation;

            var message = ex.InnerException?.Message ?? ex.Message;
            return message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase);
        }

        private sealed record AsignacionEvaluable(
            string AsignacionAcademicaId,
            MateriaAlumnoDto Materia,
            EvaluacionClasificacion Clasificacion);

        private sealed record AsignacionValidadaContexto(
            AsignacionEvaluable Asignacion,
            List<AsignacionEvaluable> Asignaciones);

        private sealed record PreguntaValidacion(
            int Id,
            PreguntaTipo Tipo,
            List<int> OpcionIds);
    }
}
