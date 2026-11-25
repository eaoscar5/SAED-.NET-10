using Microsoft.EntityFrameworkCore;
using saed.api.Data;
using saed.api.DTOs.Reportes;
using saed.api.DTOs.Universidad;
using saed.api.Model;
using saed.api.Services.Universidad;

namespace saed.api.Services
{
    public class ReportesService
    {
        private readonly AppDbContext _context;
        private readonly IUniversidadApiService _universidadApi;
        private readonly CatalogoUniversidadService _catalogoUniversidad;

        public ReportesService(
            AppDbContext context,
            IUniversidadApiService universidadApi,
            CatalogoUniversidadService catalogoUniversidad)
        {
            _context = context;
            _universidadApi = universidadApi;
            _catalogoUniversidad = catalogoUniversidad;
        }

        public async Task<PagedResultDto<ReporteResumenDto>> Buscar(ReporteFiltroDto filtro)
        {
            NormalizarFiltro(filtro);

            if (!filtro.VistaGlobal && !TieneFiltros(filtro))
            {
                throw new InvalidOperationException(
                    "Selecciona al menos un filtro o activa la vista global.");
            }

            var query = _context.Evaluaciones.AsNoTracking();

            if (filtro.ProcesoId.HasValue)
                query = query.Where(e => e.ProcesoId == filtro.ProcesoId.Value);

            if (!string.IsNullOrWhiteSpace(filtro.AcademiaClave))
                query = query.Where(e => e.AcademiaClave == filtro.AcademiaClave);

            if (!string.IsNullOrWhiteSpace(filtro.CarreraClave))
                query = query.Where(e => e.CarreraClave == filtro.CarreraClave);

            if (!string.IsNullOrWhiteSpace(filtro.Grupo))
                query = query.Where(e => e.Grupo == filtro.Grupo);

            if (!string.IsNullOrWhiteSpace(filtro.NombreMaestro))
            {
                var nombre = filtro.NombreMaestro.Trim().ToLower();
                query = query.Where(e =>
                    e.NombreMaestro.ToLower().Contains(nombre) ||
                    e.IdMaestro.Contains(nombre));
            }

            if (filtro.TipoInstrumento.HasValue)
                query = query.Where(e => e.TipoInstrumento == filtro.TipoInstrumento.Value);

            var agrupado = query
                .GroupBy(e => new
                {
                    e.ProcesoId,
                    e.Proceso.Nombre,
                    e.Proceso.Anio,
                    e.Proceso.Periodo,
                    e.IdMaestro,
                    e.NombreMaestro,
                    e.MateriaId,
                    e.NombreMateria,
                    e.Grupo,
                    e.TipoInstrumento,
                    e.InstrumentoId,
                    e.AcademiaClave,
                    e.AcademiaNombre,
                    e.CarreraClave
                })
                .Select(g => new
                {
                    g.Key.ProcesoId,
                    NombreProceso = g.Key.Nombre,
                    g.Key.Anio,
                    g.Key.Periodo,
                    g.Key.IdMaestro,
                    g.Key.NombreMaestro,
                    g.Key.MateriaId,
                    g.Key.NombreMateria,
                    g.Key.Grupo,
                    g.Key.TipoInstrumento,
                    g.Key.InstrumentoId,
                    g.Key.AcademiaClave,
                    g.Key.AcademiaNombre,
                    g.Key.CarreraClave,
                    TotalEvaluaciones = g.Count(),
                    Promedio = g.SelectMany(e => e.Respuestas)
                        .Average(r => (double?)r.Opcion.Valor) ?? 0,
                    UltimaEvaluacion = g.Max(e => e.Fecha)
                });

            var total = await agrupado.CountAsync();

            var rows = await agrupado
                .OrderBy(r => r.NombreMaestro)
                .ThenBy(r => r.NombreMateria)
                .ThenBy(r => r.Grupo)
                .Skip((filtro.Page - 1) * filtro.PageSize)
                .Take(filtro.PageSize)
                .ToListAsync();

            return new PagedResultDto<ReporteResumenDto>
            {
                Items = rows.Select(r => new ReporteResumenDto
                {
                    ProcesoId = r.ProcesoId,
                    NombreProceso = r.NombreProceso,
                    Anio = r.Anio,
                    Periodo = r.Periodo,
                    IdMaestro = r.IdMaestro,
                    NombreMaestro = r.NombreMaestro,
                    MateriaId = r.MateriaId,
                    NombreMateria = r.NombreMateria,
                    Grupo = r.Grupo,
                    TipoInstrumento = r.TipoInstrumento,
                    InstrumentoId = r.InstrumentoId,
                    AcademiaClave = r.AcademiaClave,
                    AcademiaNombre = r.AcademiaNombre,
                    CarreraClave = r.CarreraClave,
                    TotalEvaluaciones = r.TotalEvaluaciones,
                    CalificacionPromedio = Math.Round(r.Promedio, 2),
                    UltimaEvaluacion = r.UltimaEvaluacion
                }).ToList(),
                Total = total,
                Page = filtro.Page,
                PageSize = filtro.PageSize
            };
        }

        public async Task<ReporteCatalogosDto> ObtenerCatalogos(
            int? procesoId = null,
            string? tokenUniversidad = null)
        {
            var estadoCatalogos = await ObtenerEstadoCatalogosLocales();
            var sincronizacionIntentada = false;
            string? mensaje = null;

            if (!estadoCatalogos.Completo)
            {
                sincronizacionIntentada = true;

                try
                {
                    await _catalogoUniversidad.SincronizarCatalogos(tokenUniversidad);
                    estadoCatalogos = await ObtenerEstadoCatalogosLocales();

                    if (estadoCatalogos.Completo)
                        mensaje = "Catalogos de universidad sincronizados automaticamente para habilitar filtros reales.";
                }
                catch (InvalidOperationException ex)
                {
                    mensaje = CrearMensajeCatalogosNoDisponibles(ex.Message);
                }
                catch (HttpRequestException ex)
                {
                    mensaje = CrearMensajeCatalogosNoDisponibles(
                        $"No se pudo contactar la API de universidad ({ex.StatusCode?.ToString() ?? "sin codigo HTTP"}).");
                }
            }

            var academias = await _context.Academias
                .AsNoTracking()
                .OrderBy(a => a.Nombre)
                .Select(a => new ReporteCatalogoItemDto
                {
                    Clave = a.AcaClave,
                    Nombre = a.Nombre
                })
                .ToListAsync();

            var carreras = await _context.MateriasCatalogo
                .AsNoTracking()
                .Where(m => m.CarreraClave != "" && m.CarreraClave != "0")
                .Select(m => m.CarreraClave)
                .Distinct()
                .OrderBy(c => c)
                .Select(c => new ReporteCatalogoItemDto
                {
                    Clave = c,
                    Nombre = c
                })
                .ToListAsync();

            var grupos = await _context.Evaluaciones
                .AsNoTracking()
                .Where(e => !procesoId.HasValue || e.ProcesoId == procesoId.Value)
                .Where(e => e.Grupo != "")
                .GroupBy(e => new
                {
                    e.Grupo,
                    e.AcademiaClave,
                    e.CarreraClave
                })
                .OrderBy(g => g.Key.Grupo)
                .Select(g => new ReporteCatalogoItemDto
                {
                    Clave = g.Key.Grupo,
                    Nombre = g.Key.Grupo,
                    AcademiaClave = g.Key.AcademiaClave,
                    CarreraClave = g.Key.CarreraClave
                })
                .ToListAsync();

            var gruposDesdeUniversidad = false;
            var proceso = await ObtenerProcesoParaReportes(procesoId, usarActivoSiNoHayId: true);
            if (proceso != null)
            {
                try
                {
                    var gruposApi = await _universidadApi.GetGruposActivosPorPeriodo(
                        proceso.Anio,
                        proceso.Periodo,
                        tokenUniversidad);

                    grupos = gruposApi
                        .GroupBy(g => g.Grupo, StringComparer.OrdinalIgnoreCase)
                        .Select(g =>
                        {
                            var item = g.First();
                            return new ReporteCatalogoItemDto
                            {
                                Clave = item.Grupo,
                                Nombre = string.IsNullOrWhiteSpace(item.NombreGrupo)
                                    ? item.Grupo
                                    : $"{item.Grupo} - {item.NombreGrupo}",
                                AcademiaClave = item.AcademiaClave,
                                CarreraClave = item.CarreraClave
                            };
                        })
                        .OrderBy(g => g.Clave)
                        .ToList();
                    gruposDesdeUniversidad = grupos.Count > 0;

                    academias = CombinarCatalogos(
                        academias,
                        gruposApi
                            .Where(g => !string.IsNullOrWhiteSpace(g.AcademiaClave))
                            .Select(g => new ReporteCatalogoItemDto
                            {
                                Clave = g.AcademiaClave,
                                Nombre = string.IsNullOrWhiteSpace(g.AcademiaNombre)
                                    ? g.AcademiaClave
                                    : g.AcademiaNombre
                            }));

                    carreras = CombinarCatalogos(
                        carreras,
                        gruposApi
                            .Where(g => !string.IsNullOrWhiteSpace(g.CarreraClave))
                            .Select(g => new ReporteCatalogoItemDto
                            {
                                Clave = g.CarreraClave,
                                Nombre = string.IsNullOrWhiteSpace(g.CarreraNombre)
                                    ? g.CarreraClave
                                    : g.CarreraNombre
                            }));
                }
                catch (InvalidOperationException ex)
                {
                    // Sin credenciales de UniversidadApi se conserva el fallback local.
                    if (!estadoCatalogos.Completo && string.IsNullOrWhiteSpace(mensaje))
                        mensaje = CrearMensajeCatalogosNoDisponibles(ex.Message);
                }
                catch (HttpRequestException ex)
                {
                    // Si la API escolar no responde, el panel sigue usando los datos locales.
                    if (!estadoCatalogos.Completo && string.IsNullOrWhiteSpace(mensaje))
                    {
                        mensaje = CrearMensajeCatalogosNoDisponibles(
                            $"No se pudo contactar la API de universidad ({ex.StatusCode?.ToString() ?? "sin codigo HTTP"}).");
                    }
                }
            }

            if (!estadoCatalogos.Completo && string.IsNullOrWhiteSpace(mensaje))
                mensaje = CrearMensajeCatalogosNoDisponibles(null);

            return new ReporteCatalogosDto
            {
                Academias = academias,
                Carreras = carreras,
                Grupos = grupos,
                CatalogosLocalesDisponibles = estadoCatalogos.Completo,
                SincronizacionRequerida = !estadoCatalogos.Completo,
                SincronizacionIntentada = sincronizacionIntentada,
                GruposDesdeUniversidad = gruposDesdeUniversidad,
                FechaUltimaSincronizacionUtc = estadoCatalogos.FechaUltimaSincronizacionUtc,
                Mensaje = mensaje
            };
        }

        public async Task<List<ReporteProgresoGrupoResumenDto>> ObtenerProgresoGrupos(
            ReporteProgresoGrupoFiltroDto filtro,
            string? tokenUniversidad = null)
        {
            NormalizarFiltroProgreso(filtro);

            var proceso = await ObtenerProcesoParaReportes(filtro.ProcesoId, usarActivoSiNoHayId: true)
                ?? throw new InvalidOperationException("Selecciona un proceso para consultar el avance por grupo.");

            var gruposApi = await _universidadApi.GetGruposActivosPorPeriodo(
                proceso.Anio,
                proceso.Periodo,
                tokenUniversidad);

            gruposApi = FiltrarGruposApi(gruposApi, filtro);

            var gruposClaves = gruposApi
                .Select(g => g.Grupo)
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (gruposClaves.Count == 0)
                return new List<ReporteProgresoGrupoResumenDto>();

            var alumnosApi = await _universidadApi.GetAlumnosPorGrupoPeriodo(
                proceso.Anio,
                proceso.Periodo,
                tokenUniversidad);

            var alumnosPorGrupo = alumnosApi
                .Where(a => gruposClaves.Contains(a.Grupo, StringComparer.OrdinalIgnoreCase))
                .GroupBy(a => a.Grupo, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(a => a.Matricula)
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

            var evaluaciones = await _context.Evaluaciones
                .AsNoTracking()
                .Where(e => e.ProcesoId == proceso.Id && gruposClaves.Contains(e.Grupo))
                .Select(e => new
                {
                    e.Grupo,
                    e.MatriculaAlumno
                })
                .ToListAsync();

            var evaluacionesPorGrupo = evaluaciones
                .GroupBy(e => e.Grupo, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Total = g.Count(),
                        Matriculas = g.Select(e => e.MatriculaAlumno)
                            .Where(m => !string.IsNullOrWhiteSpace(m))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase)
                    },
                    StringComparer.OrdinalIgnoreCase);

            return gruposApi
                .Select(grupo =>
                {
                    alumnosPorGrupo.TryGetValue(grupo.Grupo, out var alumnos);
                    evaluacionesPorGrupo.TryGetValue(grupo.Grupo, out var evaluadas);

                    var totalAlumnos = alumnos?.Count > 0
                        ? alumnos.Count
                        : grupo.TotalAlumnos;

                    var alumnosEvaluaron = evaluadas?.Matriculas.Count ?? 0;
                    if (alumnos?.Count > 0 && evaluadas != null)
                    {
                        alumnosEvaluaron = evaluadas.Matriculas
                            .Count(m => alumnos.Contains(m));
                    }

                    alumnosEvaluaron = Math.Min(alumnosEvaluaron, totalAlumnos);
                    var faltantes = Math.Max(0, totalAlumnos - alumnosEvaluaron);

                    return new ReporteProgresoGrupoResumenDto
                    {
                        ProcesoId = proceso.Id,
                        NombreProceso = proceso.Nombre,
                        Anio = proceso.Anio,
                        Periodo = proceso.Periodo,
                        Grupo = grupo.Grupo,
                        NombreGrupo = grupo.NombreGrupo,
                        Cuatrimestre = grupo.Cuatrimestre,
                        AcademiaClave = grupo.AcademiaClave,
                        AcademiaNombre = grupo.AcademiaNombre,
                        CarreraClave = grupo.CarreraClave,
                        CarreraNombre = grupo.CarreraNombre,
                        TotalAlumnos = totalAlumnos,
                        AlumnosEvaluaron = alumnosEvaluaron,
                        AlumnosFaltantes = faltantes,
                        TotalEvaluacionesRegistradas = evaluadas?.Total ?? 0,
                        PorcentajeEvaluacion = CalcularPorcentaje(alumnosEvaluaron, totalAlumnos)
                    };
                })
                .OrderBy(g => g.AcademiaNombre)
                .ThenBy(g => g.CarreraNombre)
                .ThenBy(g => g.Grupo)
                .ToList();
        }

        public async Task<ReporteProgresoGrupoDetalleDto> ObtenerProgresoGrupoDetalle(
            int? procesoId,
            string grupo,
            string? tokenUniversidad = null)
        {
            grupo = (grupo ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(grupo))
                throw new InvalidOperationException("Selecciona un grupo para consultar el detalle.");

            var proceso = await ObtenerProcesoParaReportes(procesoId, usarActivoSiNoHayId: true)
                ?? throw new InvalidOperationException("Selecciona un proceso para consultar el avance por grupo.");

            var procesoClasificacion = await _context.Procesos
                .Include(p => p.ProcesoInstrumentos)
                    .ThenInclude(pi => pi.Instrumento)
                .FirstAsync(p => p.Id == proceso.Id);

            var gruposApi = await _universidadApi.GetGruposActivosPorPeriodo(
                proceso.Anio,
                proceso.Periodo,
                tokenUniversidad);

            var grupoInfo = gruposApi.FirstOrDefault(g =>
                g.Grupo.Equals(grupo, StringComparison.OrdinalIgnoreCase));

            if (grupoInfo == null)
                throw new InvalidOperationException("El grupo no existe en el periodo seleccionado.");

            var alumnos = (await _universidadApi.GetAlumnosPorGrupoPeriodo(
                    proceso.Anio,
                    proceso.Periodo,
                    tokenUniversidad))
                .Where(a => a.Grupo.Equals(grupo, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.ApellidoPaterno)
                .ThenBy(a => a.ApellidoMaterno)
                .ThenBy(a => a.Nombre)
                .ToList();

            var materiasApi = await _universidadApi.GetMateriasMaestroPorGrupo(
                grupo,
                proceso.Anio,
                proceso.Periodo,
                tokenUniversidad);

            var materias = new List<ReporteProgresoMateriaDto>();
            foreach (var item in materiasApi)
            {
                var clasificacion = await _catalogoUniversidad.Clasificar(
                    procesoClasificacion,
                    item.IdMaestro,
                    item.MateriaId,
                    item.NombreMateria);

                materias.Add(new ReporteProgresoMateriaDto
                {
                    IdMaestro = item.IdMaestro,
                    NombreMaestro = item.NombreMaestro,
                    MateriaId = item.MateriaId,
                    NombreMateria = item.NombreMateria,
                    TipoInstrumento = clasificacion.TipoInstrumento,
                    InstrumentoId = clasificacion.InstrumentoId,
                    AcademiaClave = clasificacion.AcademiaClave ?? grupoInfo.AcademiaClave,
                    AcademiaNombre = clasificacion.AcademiaNombre ?? grupoInfo.AcademiaNombre,
                    CarreraClave = clasificacion.CarreraClave ?? grupoInfo.CarreraClave
                });
            }

            var asignacionesEsperadas = materias
                .Select(m => CrearLlaveAsignacion(m.IdMaestro, m.MateriaId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var matriculas = alumnos
                .Select(a => a.Matricula)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var evaluacionesQuery = _context.Evaluaciones
                .AsNoTracking()
                .Where(e => e.ProcesoId == proceso.Id && e.Grupo == grupo);

            if (matriculas.Count > 0)
                evaluacionesQuery = evaluacionesQuery.Where(e => matriculas.Contains(e.MatriculaAlumno));

            var evaluaciones = await evaluacionesQuery
                .Select(e => new
                {
                    e.MatriculaAlumno,
                    e.IdMaestro,
                    e.MateriaId,
                    e.Fecha
                })
                .ToListAsync();

            var evaluacionesPorAlumno = evaluaciones
                .GroupBy(e => e.MatriculaAlumno, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Llaves = g.Select(e => CrearLlaveAsignacion(e.IdMaestro, e.MateriaId))
                            .Where(k => asignacionesEsperadas.Count == 0 || asignacionesEsperadas.Contains(k))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase),
                        UltimaEvaluacion = g.Max(e => (DateTime?)e.Fecha)
                    },
                    StringComparer.OrdinalIgnoreCase);

            var alumnosDetalle = alumnos.Select(alumno =>
            {
                evaluacionesPorAlumno.TryGetValue(alumno.Matricula, out var realizadas);

                var totalRealizadas = realizadas?.Llaves.Count ?? 0;
                var totalEsperadas = asignacionesEsperadas.Count;
                var faltantes = totalEsperadas > 0
                    ? Math.Max(0, totalEsperadas - totalRealizadas)
                    : 0;

                return new ReporteProgresoAlumnoDto
                {
                    Matricula = alumno.Matricula,
                    NombreAlumno = alumno.NombreCompleto,
                    EvaluacionesEsperadas = totalEsperadas,
                    EvaluacionesRealizadas = totalRealizadas,
                    EvaluacionesFaltantes = faltantes,
                    Evaluado = totalEsperadas > 0
                        ? totalRealizadas >= totalEsperadas
                        : totalRealizadas > 0,
                    UltimaEvaluacion = realizadas?.UltimaEvaluacion
                };
            }).ToList();

            var alumnosEvaluaron = alumnosDetalle.Count(a => a.Evaluado);
            var totalAlumnos = alumnosDetalle.Count > 0
                ? alumnosDetalle.Count
                : grupoInfo.TotalAlumnos;

            return new ReporteProgresoGrupoDetalleDto
            {
                ProcesoId = proceso.Id,
                NombreProceso = proceso.Nombre,
                Anio = proceso.Anio,
                Periodo = proceso.Periodo,
                Grupo = grupoInfo.Grupo,
                NombreGrupo = grupoInfo.NombreGrupo,
                Cuatrimestre = grupoInfo.Cuatrimestre,
                AcademiaClave = grupoInfo.AcademiaClave,
                AcademiaNombre = grupoInfo.AcademiaNombre,
                CarreraClave = grupoInfo.CarreraClave,
                CarreraNombre = grupoInfo.CarreraNombre,
                TotalAlumnos = totalAlumnos,
                AlumnosEvaluaron = alumnosEvaluaron,
                AlumnosFaltantes = Math.Max(0, totalAlumnos - alumnosEvaluaron),
                TotalEvaluacionesRegistradas = evaluaciones.Count,
                PorcentajeEvaluacion = CalcularPorcentaje(alumnosEvaluaron, totalAlumnos),
                MateriasAsignadas = asignacionesEsperadas.Count,
                Materias = materias
                    .OrderBy(m => m.NombreMateria)
                    .ThenBy(m => m.NombreMaestro)
                    .ToList(),
                Alumnos = alumnosDetalle
            };
        }

        public async Task<ReporteMockSeedDto> SembrarDatosMock()
        {
            var now = DateTime.UtcNow;

            var academias = new[]
            {
                new MockAcademia("ACD", "Academia de Cultura y Deporte", true),
                new MockAcademia("ADN", "Academia de Desarrollo de Negocios", false),
                new MockAcademia("AE", "Academia de Enfermeria", false),
                new MockAcademia("AI", "Academia de Idiomas", false),
                new MockAcademia("AM", "Academia de Mecatronica", false),
                new MockAcademia("APMA", "Academia de Procesos-Mantenimiento-Aeronautica", false),
                new MockAcademia("ATI", "Academia de TI", false)
            };

            var maestros = new[]
            {
                new MockMaestro("ACD", "Academia de Cultura y Deporte", "9019", "ALCANTAR CISNEROS ABRAHAM"),
                new MockMaestro("ACD", "Academia de Cultura y Deporte", "1434", "GARCIA AMAVIZCA LAURA YUBITZIA"),
                new MockMaestro("ACD", "Academia de Cultura y Deporte", "2188", "HERNANDEZ VEGA RAFAEL"),
                new MockMaestro("ACD", "Academia de Cultura y Deporte", "1387", "LARREA BREMONT IVAN NOE"),
                new MockMaestro("ACD", "Academia de Cultura y Deporte", "1445", "PARRA SANTOYO RICARDO"),
                new MockMaestro("ACD", "Academia de Cultura y Deporte", "2872", "REYES SAÑUDO JOEL"),
                new MockMaestro("ADN", "Academia de Desarrollo de Negocios", "1043", "ALONSO ALDANA RUTH"),
                new MockMaestro("ADN", "Academia de Desarrollo de Negocios", "1170", "CANTU ARMENDARIZ VICTOR MANUEL"),
                new MockMaestro("ADN", "Academia de Desarrollo de Negocios", "1261", "ARREDONDO HERNANDEZ GABRIELA"),
                new MockMaestro("ADN", "Academia de Desarrollo de Negocios", "1251", "BARCO BERNAL MONNE JANETH"),
                new MockMaestro("ATI", "Academia de TI", "1010", "AMAVIZCA VALDEZ LAURA OLIVIA"),
                new MockMaestro("ATI", "Academia de TI", "1170", "CANTU ARMENDARIZ VICTOR MANUEL"),
                new MockMaestro("ATI", "Academia de TI", "1427", "DUARTE FIGUEROA JOSE ELIU"),
                new MockMaestro("ATI", "Academia de TI", "1172", "FELIX AVILES CARLOS ALBERTO"),
                new MockMaestro("ATI", "Academia de TI", "1434", "GARCIA AMAVIZCA LAURA YUBITZIA"),
                new MockMaestro("ATI", "Academia de TI", "1057", "TOLANO GUTIERREZ HELGA KARINA"),
                new MockMaestro("ATI", "Academia de TI", "1052", "VAZQUEZ BRINDIS JUAN CARLOS"),
                new MockMaestro("ATI", "Academia de TI", "2054", "VAZQUEZ MORENO ERIKA"),
                new MockMaestro("ATI", "Academia de TI", "1002", "WONG GASTELUM JOSE LUIS")
            };

            var materias = new[]
            {
                new MockMateria("DSM-BDA", "Bases de datos avanzadas", 75, false, 1, "DSM"),
                new MockMateria("DSM-PI2", "Proyecto integrador II", 60, false, 1, "DSM"),
                new MockMateria("DSM-LID", "Liderazgo de equipos de alto desempeño", 45, false, 1, "DSM"),
                new MockMateria("DSM-EMD", "Estandares y metricas para el desarrollo de software", 60, false, 1, "DSM"),
                new MockMateria("DSM-ECD", "Ecuaciones diferenciales", 75, false, 1, "DSM"),
                new MockMateria("DSM-AWS", "Aplicaciones web orientadas a servicios", 75, false, 1, "DSM"),
                new MockMateria("LDN-NEG", "Negociacion empresarial", 60, false, 1, "LDN"),
                new MockMateria("LDN-ADM", "Administracion del tiempo", 45, false, 1, "LDN"),
                new MockMateria("ENF-BIO", "Bioetica en enfermeria", 60, false, 1, "ENF"),
                new MockMateria("MEC-PLC", "Controladores logicos programables", 75, false, 1, "MEC"),
                new MockMateria("ACD-AFI", "Acondicionamiento fisico", 30, true, 3, "PARAESCOLAR"),
                new MockMateria("ACD-FUT", "Futbol soccer", 30, true, 3, "PARAESCOLAR"),
                new MockMateria("ACD-DAN", "Danza folklorica", 30, true, 3, "PARAESCOLAR"),
                new MockMateria("ACD-TEA", "Teatro", 30, true, 3, "PARAESCOLAR"),
                new MockMateria("ACD-CUL", "Taller cultural", 30, true, 3, "PARAESCOLAR")
            };

            foreach (var item in academias)
                await UpsertAcademia(item, now);

            foreach (var item in maestros)
                await UpsertMaestro(item, now);

            foreach (var item in materias)
                await UpsertMateria(item, now);

            await _context.SaveChangesAsync();

            var instrumentoCurricular = await ObtenerOCrearInstrumentoMock(
                "Instrumento Mock Curricular",
                TipoInstrumento.Curricular);

            var instrumentoExtracurricular = await ObtenerOCrearInstrumentoMock(
                "Instrumento Mock Extracurricular",
                TipoInstrumento.Extracurricular);

            var proceso = await ObtenerOCrearProcesoMock(
                instrumentoCurricular.Id,
                instrumentoExtracurricular.Id);

            var anteriores = await _context.Evaluaciones
                .Where(e => e.ProcesoId == proceso.Id && e.MatriculaAlumno.StartsWith("MOCK"))
                .ToListAsync();

            _context.Evaluaciones.RemoveRange(anteriores);
            await _context.SaveChangesAsync();

            var preguntasCurricular = await ObtenerPreguntas(instrumentoCurricular.Id);
            var preguntasExtracurricular = await ObtenerPreguntas(instrumentoExtracurricular.Id);

            var escenarios = new[]
            {
                new MockReporteRow("1052", "VAZQUEZ BRINDIS JUAN CARLOS", "DSM-BDA", "Bases de datos avanzadas", "DSM5A", TipoInstrumento.Curricular, "ATI", "Academia de TI", "DSM", 13, 4.6),
                new MockReporteRow("1057", "TOLANO GUTIERREZ HELGA KARINA", "DSM-PI2", "Proyecto integrador II", "DSM5A", TipoInstrumento.Curricular, "ATI", "Academia de TI", "DSM", 12, 4.2),
                new MockReporteRow("1427", "DUARTE FIGUEROA JOSE ELIU", "DSM-LID", "Liderazgo de equipos de alto desempeño", "DSM5A", TipoInstrumento.Curricular, "ATI", "Academia de TI", "DSM", 11, 4.4),
                new MockReporteRow("1170", "CANTU ARMENDARIZ VICTOR MANUEL", "DSM-EMD", "Estandares y metricas para el desarrollo de software", "DSM5A", TipoInstrumento.Curricular, "ATI", "Academia de TI", "DSM", 14, 4.1),
                new MockReporteRow("1434", "GARCIA AMAVIZCA LAURA YUBITZIA", "DSM-AWS", "Aplicaciones web orientadas a servicios", "DSM5A", TipoInstrumento.Curricular, "ATI", "Academia de TI", "DSM", 12, 4.7),
                new MockReporteRow("1170", "CANTU ARMENDARIZ VICTOR MANUEL", "LDN-ADM", "Administracion del tiempo", "LDN3A", TipoInstrumento.Curricular, "ADN", "Academia de Desarrollo de Negocios", "LDN", 10, 3.9),
                new MockReporteRow("1043", "ALONSO ALDANA RUTH", "LDN-NEG", "Negociacion empresarial", "LDN5A", TipoInstrumento.Curricular, "ADN", "Academia de Desarrollo de Negocios", "LDN", 9, 4.5),
                new MockReporteRow("1010", "AMAVIZCA VALDEZ LAURA OLIVIA", "DSM-ECD", "Ecuaciones diferenciales", "DSM5A", TipoInstrumento.Curricular, "ATI", "Academia de TI", "DSM", 10, 3.7),
                new MockReporteRow("9019", "ALCANTAR CISNEROS ABRAHAM", "ACD-AFI", "Acondicionamiento fisico", "PAR5A", TipoInstrumento.Extracurricular, "ACD", "Academia de Cultura y Deporte", "PARAESCOLAR", 16, 4.8),
                new MockReporteRow("1445", "PARRA SANTOYO RICARDO", "ACD-FUT", "Futbol soccer", "PAR5B", TipoInstrumento.Extracurricular, "ACD", "Academia de Cultura y Deporte", "PARAESCOLAR", 15, 4.6),
                new MockReporteRow("2872", "REYES SAÑUDO JOEL", "ACD-AFI", "Acondicionamiento fisico", "PAR3A", TipoInstrumento.Extracurricular, "ACD", "Academia de Cultura y Deporte", "PARAESCOLAR", 13, 4.3),
                new MockReporteRow("2188", "HERNANDEZ VEGA RAFAEL", "ACD-DAN", "Danza folklorica", "PAR2A", TipoInstrumento.Extracurricular, "ACD", "Academia de Cultura y Deporte", "PARAESCOLAR", 12, 4.9),
                new MockReporteRow("1387", "LARREA BREMONT IVAN NOE", "ACD-TEA", "Teatro", "PAR4A", TipoInstrumento.Extracurricular, "ACD", "Academia de Cultura y Deporte", "PARAESCOLAR", 10, 4.2),
                new MockReporteRow("1434", "GARCIA AMAVIZCA LAURA YUBITZIA", "ACD-CUL", "Taller cultural", "PAR5C", TipoInstrumento.Extracurricular, "ACD", "Academia de Cultura y Deporte", "PARAESCOLAR", 11, 4.5)
            };

            var evaluacionesCreadas = 0;
            var alumnoConsecutivo = 1;

            foreach (var row in escenarios)
            {
                var preguntas = row.TipoInstrumento == TipoInstrumento.Extracurricular
                    ? preguntasExtracurricular
                    : preguntasCurricular;

                var instrumentoId = row.TipoInstrumento == TipoInstrumento.Extracurricular
                    ? instrumentoExtracurricular.Id
                    : instrumentoCurricular.Id;

                for (var i = 0; i < row.TotalEvaluaciones; i++)
                {
                    var evaluacion = new Evaluacion
                    {
                        ProcesoId = proceso.Id,
                        MatriculaAlumno = $"MOCK{alumnoConsecutivo:0000000000}",
                        AsignacionAcademicaId = CrearAsignacionAcademicaId(row.Grupo, row.IdMaestro, row.MateriaId),
                        IdMaestro = row.IdMaestro,
                        NombreMaestro = row.NombreMaestro,
                        MateriaId = row.MateriaId,
                        NombreMateria = row.NombreMateria,
                        Grupo = row.Grupo,
                        InstrumentoId = instrumentoId,
                        TipoInstrumento = row.TipoInstrumento,
                        AcademiaClave = row.AcademiaClave,
                        AcademiaNombre = row.AcademiaNombre,
                        CarreraClave = row.CarreraClave,
                        TipoMateria = row.TipoInstrumento == TipoInstrumento.Extracurricular ? 3 : 1,
                        MateriaNoCurricular = row.TipoInstrumento == TipoInstrumento.Extracurricular,
                        Fecha = now.AddDays(-(alumnoConsecutivo % 21)).AddMinutes(alumnoConsecutivo)
                    };

                    foreach (var pregunta in preguntas)
                    {
                        var objetivo = CalcularValor(row.PromedioBase, i, pregunta.Orden);
                        var opcion = pregunta.Opciones
                            .OrderBy(o => Math.Abs(o.Valor - objetivo))
                            .ThenBy(o => o.Valor)
                            .First();

                        evaluacion.Respuestas.Add(new Respuesta
                        {
                            PreguntaId = pregunta.Id,
                            OpcionId = opcion.Id
                        });
                    }

                    if (i % 5 == 0)
                    {
                        evaluacion.Comentario = new Comentario
                        {
                            Texto = row.TipoInstrumento == TipoInstrumento.Extracurricular
                                ? "Actividad bien organizada y con seguimiento constante del maestro."
                                : "El maestro explica con claridad y relaciona la materia con casos practicos."
                        };
                    }

                    _context.Evaluaciones.Add(evaluacion);
                    alumnoConsecutivo++;
                    evaluacionesCreadas++;
                }
            }

            await _context.SaveChangesAsync();

            return new ReporteMockSeedDto
            {
                ProcesoId = proceso.Id,
                NombreProceso = proceso.Nombre,
                Academias = academias.Length,
                MaestrosAcademia = maestros.Length,
                Materias = materias.Length,
                Evaluaciones = evaluacionesCreadas,
                Reportes = escenarios.Length,
                Message = "Datos demo de reportes generados correctamente."
            };
        }

        public async Task<ReporteMockSeedDto> SembrarDatosMockDetallado()
        {
            var now = DateTime.UtcNow;

            var academias = new[]
            {
                new MockAcademia("ACD", "Academia de Cultura y Deporte", true),
                new MockAcademia("ATI", "Academia de TI", false),
                new MockAcademia("ADN", "Academia de Desarrollo de Negocios", false),
                new MockAcademia("AI", "Academia de Idiomas", false)
            };

            var maestros = new[]
            {
                new MockMaestro("ATI", "Academia de TI", "1416", "LOPEZ HUMO MIGUEL ALEJANDRO"),
                new MockMaestro("ATI", "Academia de TI", "1434", "GARCIA AMAVIZCA LAURA YUBITZIA"),
                new MockMaestro("ACD", "Academia de Cultura y Deporte", "1434", "GARCIA AMAVIZCA LAURA YUBITZIA"),
                new MockMaestro("ACD", "Academia de Cultura y Deporte", "2188", "HERNANDEZ VEGA RAFAEL"),
                new MockMaestro("ADN", "Academia de Desarrollo de Negocios", "1170", "CANTU ARMENDARIZ VICTOR MANUEL"),
                new MockMaestro("AI", "Academia de Idiomas", "2054", "VAZQUEZ MORENO ERIKA")
            };

            var materias = new[]
            {
                new MockMateria("404410", "Administracion de base de datos", 90, false, 1, "IDyGS"),
                new MockMateria("704502", "Desarrollo movil multiplataforma", 105, false, 1, "TIDSM"),
                new MockMateria("704505", "Bases de datos para computo en la nube", 90, false, 1, "TIDSM"),
                new MockMateria("DSM-AWS", "Aplicaciones web orientadas a servicios", 75, false, 1, "DSM"),
                new MockMateria("DSM-BD", "Base de datos", 75, false, 1, "DSM"),
                new MockMateria("LDN-ADM", "Administracion del tiempo", 45, false, 1, "LDN"),
                new MockMateria("AI-ING3", "Ingles III", 60, false, 1, "DSM"),
                new MockMateria("8008", "Artes visuales", 30, true, 3, "PARAESCOLAR"),
                new MockMateria("ACD-CUL", "Taller cultural", 30, true, 3, "PARAESCOLAR"),
                new MockMateria("ACD-DAN", "Danza folklorica", 30, true, 3, "PARAESCOLAR")
            };

            foreach (var item in academias)
                await UpsertAcademia(item, now);

            foreach (var item in maestros)
                await UpsertMaestro(item, now);

            foreach (var item in materias)
                await UpsertMateria(item, now);

            await _context.SaveChangesAsync();

            var instrumentoCurricular = await ObtenerOCrearInstrumentoDetalladoMock(
                "Instrumento Mock Detallado Curricular",
                TipoInstrumento.Curricular);

            var instrumentoExtracurricular = await ObtenerOCrearInstrumentoDetalladoMock(
                "Instrumento Mock Detallado Extracurricular",
                TipoInstrumento.Extracurricular);

            var proceso = await ObtenerOCrearProcesoDetalladoMock(
                instrumentoCurricular.Id,
                instrumentoExtracurricular.Id);

            var anteriores = await _context.Evaluaciones
                .Where(e => e.ProcesoId == proceso.Id && e.MatriculaAlumno.StartsWith("MOCKDET"))
                .ToListAsync();

            _context.Evaluaciones.RemoveRange(anteriores);
            await _context.SaveChangesAsync();

            var preguntasCurricular = await ObtenerPreguntas(instrumentoCurricular.Id);
            var preguntasExtracurricular = await ObtenerPreguntas(instrumentoExtracurricular.Id);

            var escenarios = new[]
            {
                new MockReporteRow("1416", "LOPEZ HUMO MIGUEL ALEJANDRO", "404410", "Administracion de base de datos", "IDYGS8A", TipoInstrumento.Curricular, "ATI", "Academia de TI", "IDyGS", 12, 4.85),
                new MockReporteRow("1416", "LOPEZ HUMO MIGUEL ALEJANDRO", "704502", "Desarrollo movil multiplataforma", "TIDSM5A", TipoInstrumento.Curricular, "ATI", "Academia de TI", "TIDSM", 22, 4.65),
                new MockReporteRow("1416", "LOPEZ HUMO MIGUEL ALEJANDRO", "704505", "Bases de datos para computo en la nube", "TIDSM5A", TipoInstrumento.Curricular, "ATI", "Academia de TI", "TIDSM", 22, 4.35),
                new MockReporteRow("1434", "GARCIA AMAVIZCA LAURA YUBITZIA", "DSM-AWS", "Aplicaciones web orientadas a servicios", "DSM5A", TipoInstrumento.Curricular, "ATI", "Academia de TI", "DSM", 18, 4.45),
                new MockReporteRow("1434", "GARCIA AMAVIZCA LAURA YUBITZIA", "DSM-BD", "Base de datos", "DSM3B", TipoInstrumento.Curricular, "ATI", "Academia de TI", "DSM", 16, 4.05),
                new MockReporteRow("1434", "GARCIA AMAVIZCA LAURA YUBITZIA", "8008", "Artes visuales", "8008", TipoInstrumento.Extracurricular, "ACD", "Academia de Cultura y Deporte", "PARAESCOLAR", 20, 4.75),
                new MockReporteRow("1434", "GARCIA AMAVIZCA LAURA YUBITZIA", "ACD-CUL", "Taller cultural", "PAR5C", TipoInstrumento.Extracurricular, "ACD", "Academia de Cultura y Deporte", "PARAESCOLAR", 14, 4.55),
                new MockReporteRow("2188", "HERNANDEZ VEGA RAFAEL", "ACD-DAN", "Danza folklorica", "PAR2A", TipoInstrumento.Extracurricular, "ACD", "Academia de Cultura y Deporte", "PARAESCOLAR", 15, 4.7),
                new MockReporteRow("1170", "CANTU ARMENDARIZ VICTOR MANUEL", "LDN-ADM", "Administracion del tiempo", "LDN3A", TipoInstrumento.Curricular, "ADN", "Academia de Desarrollo de Negocios", "LDN", 11, 3.95),
                new MockReporteRow("2054", "VAZQUEZ MORENO ERIKA", "AI-ING3", "Ingles III", "DSM3B", TipoInstrumento.Curricular, "AI", "Academia de Idiomas", "DSM", 13, 4.25)
            };

            var evaluacionesCreadas = 0;
            var alumnoConsecutivo = 1;

            foreach (var row in escenarios)
            {
                var preguntas = row.TipoInstrumento == TipoInstrumento.Extracurricular
                    ? preguntasExtracurricular
                    : preguntasCurricular;

                var preguntasCerradas = preguntas
                    .Where(p => p.Tipo == PreguntaTipo.Cerrada && p.Opciones.Any())
                    .ToList();

                var instrumentoId = row.TipoInstrumento == TipoInstrumento.Extracurricular
                    ? instrumentoExtracurricular.Id
                    : instrumentoCurricular.Id;

                for (var i = 0; i < row.TotalEvaluaciones; i++)
                {
                    var evaluacion = new Evaluacion
                    {
                        ProcesoId = proceso.Id,
                        MatriculaAlumno = $"MOCKDET{alumnoConsecutivo:00000000}",
                        AsignacionAcademicaId = CrearAsignacionAcademicaId(row.Grupo, row.IdMaestro, row.MateriaId),
                        IdMaestro = row.IdMaestro,
                        NombreMaestro = row.NombreMaestro,
                        MateriaId = row.MateriaId,
                        NombreMateria = row.NombreMateria,
                        Grupo = row.Grupo,
                        InstrumentoId = instrumentoId,
                        TipoInstrumento = row.TipoInstrumento,
                        AcademiaClave = row.AcademiaClave,
                        AcademiaNombre = row.AcademiaNombre,
                        CarreraClave = row.CarreraClave,
                        TipoMateria = row.TipoInstrumento == TipoInstrumento.Extracurricular ? 3 : 1,
                        MateriaNoCurricular = row.TipoInstrumento == TipoInstrumento.Extracurricular,
                        Fecha = now.AddDays(-(alumnoConsecutivo % 28)).AddMinutes(alumnoConsecutivo)
                    };

                    foreach (var pregunta in preguntasCerradas)
                    {
                        var objetivo = CalcularValor(row.PromedioBase, i, pregunta.Orden);
                        var opcion = pregunta.Opciones
                            .OrderBy(o => Math.Abs(o.Valor - objetivo))
                            .ThenByDescending(o => o.Valor)
                            .First();

                        evaluacion.Respuestas.Add(new Respuesta
                        {
                            PreguntaId = pregunta.Id,
                            OpcionId = opcion.Id
                        });
                    }

                    if (i % 4 == 0)
                    {
                        evaluacion.Comentario = new Comentario
                        {
                            Texto = CrearComentarioDetallado(row, i)
                        };
                    }

                    _context.Evaluaciones.Add(evaluacion);
                    alumnoConsecutivo++;
                    evaluacionesCreadas++;
                }
            }

            await _context.SaveChangesAsync();

            return new ReporteMockSeedDto
            {
                ProcesoId = proceso.Id,
                NombreProceso = proceso.Nombre,
                Academias = academias.Length,
                MaestrosAcademia = maestros.Length,
                Materias = materias.Length,
                Evaluaciones = evaluacionesCreadas,
                Reportes = escenarios.Length,
                Message = "Datos demo detallados generados con instrumentos curricular y extracurricular."
            };
        }

        private async Task<EstadoCatalogosLocales> ObtenerEstadoCatalogosLocales()
        {
            var academias = await _context.Academias.AsNoTracking().CountAsync();
            var maestrosAcademia = await _context.MaestrosAcademia.AsNoTracking().CountAsync();
            var materias = await _context.MateriasCatalogo.AsNoTracking().CountAsync();

            var fechaAcademias = await _context.Academias
                .AsNoTracking()
                .MaxAsync(a => a.FechaSincronizacionUtc);
            var fechaMaestros = await _context.MaestrosAcademia
                .AsNoTracking()
                .MaxAsync(m => (DateTime?)m.FechaSincronizacionUtc);
            var fechaMaterias = await _context.MateriasCatalogo
                .AsNoTracking()
                .MaxAsync(m => (DateTime?)m.FechaSincronizacionUtc);

            var fechas = new[] { fechaAcademias, fechaMaestros, fechaMaterias }
                .Where(f => f.HasValue)
                .Select(f => f!.Value)
                .ToList();

            return new EstadoCatalogosLocales(
                academias,
                maestrosAcademia,
                materias,
                fechas.Count == 0 ? null : fechas.Max());
        }

        private static string CrearMensajeCatalogosNoDisponibles(string? detalle)
        {
            var mensaje =
                "No hay catalogos locales completos de universidad. " +
                "Sincroniza catalogos con una sesion admin autorizada o configura las credenciales de servicio de UniversidadApi para usar filtros reales.";

            return string.IsNullOrWhiteSpace(detalle)
                ? mensaje
                : $"{mensaje} Detalle: {detalle}";
        }

        private sealed record EstadoCatalogosLocales(
            int Academias,
            int MaestrosAcademia,
            int Materias,
            DateTime? FechaUltimaSincronizacionUtc)
        {
            public bool Completo => Academias > 0 && MaestrosAcademia > 0 && Materias > 0;
        }

        private static void NormalizarFiltro(ReporteFiltroDto filtro)
        {
            filtro.Page = Math.Max(1, filtro.Page);
            filtro.PageSize = Math.Clamp(filtro.PageSize, 1, 100);
            filtro.AcademiaClave = NormalizarTexto(filtro.AcademiaClave);
            filtro.CarreraClave = NormalizarTexto(filtro.CarreraClave);
            filtro.Grupo = NormalizarTexto(filtro.Grupo);
            filtro.NombreMaestro = NormalizarTexto(filtro.NombreMaestro);
        }

        private static bool TieneFiltros(ReporteFiltroDto filtro)
        {
            return filtro.ProcesoId.HasValue ||
                !string.IsNullOrWhiteSpace(filtro.AcademiaClave) ||
                !string.IsNullOrWhiteSpace(filtro.CarreraClave) ||
                !string.IsNullOrWhiteSpace(filtro.Grupo) ||
                !string.IsNullOrWhiteSpace(filtro.NombreMaestro) ||
                filtro.TipoInstrumento.HasValue;
        }

        private static void NormalizarFiltroProgreso(ReporteProgresoGrupoFiltroDto filtro)
        {
            filtro.AcademiaClave = NormalizarTexto(filtro.AcademiaClave);
            filtro.CarreraClave = NormalizarTexto(filtro.CarreraClave);
            filtro.Grupo = NormalizarTexto(filtro.Grupo);
        }

        private static string? NormalizarTexto(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private async Task<Proceso?> ObtenerProcesoParaReportes(
            int? procesoId,
            bool usarActivoSiNoHayId)
        {
            var query = _context.Procesos.AsNoTracking();

            if (procesoId.HasValue)
                return await query.FirstOrDefaultAsync(p => p.Id == procesoId.Value);

            if (!usarActivoSiNoHayId)
                return null;

            return await query
                .OrderByDescending(p => p.Activo)
                .ThenByDescending(p => p.Anio)
                .ThenByDescending(p => p.Periodo)
                .ThenByDescending(p => p.Id)
                .FirstOrDefaultAsync();
        }

        private static List<GrupoActivoUniversidadDto> FiltrarGruposApi(
            List<GrupoActivoUniversidadDto> grupos,
            ReporteProgresoGrupoFiltroDto filtro)
        {
            var query = grupos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filtro.AcademiaClave))
            {
                query = query.Where(g =>
                    g.AcademiaClave.Equals(filtro.AcademiaClave, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filtro.CarreraClave))
            {
                query = query.Where(g =>
                    g.CarreraClave.Equals(filtro.CarreraClave, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filtro.Grupo))
            {
                query = query.Where(g =>
                    g.Grupo.Equals(filtro.Grupo, StringComparison.OrdinalIgnoreCase));
            }

            return query.ToList();
        }

        private static List<ReporteCatalogoItemDto> CombinarCatalogos(
            List<ReporteCatalogoItemDto> actuales,
            IEnumerable<ReporteCatalogoItemDto> nuevos)
        {
            return actuales
                .Concat(nuevos)
                .Where(i => !string.IsNullOrWhiteSpace(i.Clave))
                .GroupBy(i => i.Clave, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var item = g.FirstOrDefault(i =>
                        !string.IsNullOrWhiteSpace(i.Nombre) &&
                        !i.Nombre.Equals(i.Clave, StringComparison.OrdinalIgnoreCase))
                        ?? g.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.Nombre))
                        ?? g.First();

                    return new ReporteCatalogoItemDto
                    {
                        Clave = item.Clave,
                        Nombre = string.IsNullOrWhiteSpace(item.Nombre) ? item.Clave : item.Nombre
                    };
                })
                .OrderBy(i => i.Nombre)
                .ToList();
        }

        private static string CrearLlaveAsignacion(string idMaestro, string materiaId)
        {
            return $"{idMaestro.Trim().ToUpperInvariant()}|{materiaId.Trim().ToUpperInvariant()}";
        }

        private static string CrearAsignacionAcademicaId(string grupo, string idMaestro, string materiaId)
        {
            return string.Join(
                "|",
                grupo.Trim().ToUpperInvariant(),
                idMaestro.Trim().ToUpperInvariant(),
                materiaId.Trim().ToUpperInvariant());
        }

        private static double CalcularPorcentaje(int valor, int total)
        {
            return total <= 0 ? 0 : Math.Round((valor * 100.0) / total, 2);
        }

        private async Task UpsertAcademia(MockAcademia item, DateTime now)
        {
            var academia = await _context.Academias
                .FirstOrDefaultAsync(a => a.AcaClave == item.Clave);

            if (academia == null)
            {
                _context.Academias.Add(new Academia
                {
                    AcaClave = item.Clave,
                    Nombre = item.Nombre,
                    EsExtracurricular = item.EsExtracurricular,
                    FechaSincronizacionUtc = now
                });
                return;
            }

            academia.Nombre = item.Nombre;
            academia.EsExtracurricular = item.EsExtracurricular;
            academia.FechaSincronizacionUtc = now;
        }

        private async Task UpsertMaestro(MockMaestro item, DateTime now)
        {
            var maestro = await _context.MaestrosAcademia
                .FirstOrDefaultAsync(m => m.AcaClave == item.AcademiaClave && m.IdMaestro == item.IdMaestro);

            if (maestro == null)
            {
                _context.MaestrosAcademia.Add(new MaestroAcademia
                {
                    AcaClave = item.AcademiaClave,
                    NombreAcademia = item.AcademiaNombre,
                    IdMaestro = item.IdMaestro,
                    NombreMaestro = item.NombreMaestro,
                    FechaSincronizacionUtc = now
                });
                return;
            }

            maestro.NombreAcademia = item.AcademiaNombre;
            maestro.NombreMaestro = item.NombreMaestro;
            maestro.FechaSincronizacionUtc = now;
        }

        private async Task UpsertMateria(MockMateria item, DateTime now)
        {
            var materia = await _context.MateriasCatalogo
                .FirstOrDefaultAsync(m => m.MateriaId == item.MateriaId);

            if (materia == null)
            {
                _context.MateriasCatalogo.Add(new MateriaCatalogo
                {
                    MateriaId = item.MateriaId,
                    NombreMateria = item.NombreMateria,
                    HorasTotales = item.HorasTotales,
                    NaturalezaNoCurricular = item.NaturalezaNoCurricular,
                    TipoMateria = item.TipoMateria,
                    Seriacion = "",
                    CarreraClave = item.CarreraClave,
                    NumeroCompetencias = 1,
                    MateriaIntegradora = item.NombreMateria.Contains("integrador", StringComparison.OrdinalIgnoreCase),
                    FechaSincronizacionUtc = now
                });
                return;
            }

            materia.NombreMateria = item.NombreMateria;
            materia.HorasTotales = item.HorasTotales;
            materia.NaturalezaNoCurricular = item.NaturalezaNoCurricular;
            materia.TipoMateria = item.TipoMateria;
            materia.CarreraClave = item.CarreraClave;
            materia.FechaSincronizacionUtc = now;
        }

        private async Task<Instrumento> ObtenerOCrearInstrumentoMock(string nombre, TipoInstrumento tipo)
        {
            var instrumento = await _context.Instrumentos
                .Include(i => i.Categorias)
                    .ThenInclude(c => c.Preguntas)
                        .ThenInclude(p => p.Opciones)
                .FirstOrDefaultAsync(i => i.Nombre == nombre && i.TipoInstrumento == tipo);

            if (instrumento != null && instrumento.Categorias.SelectMany(c => c.Preguntas).Any())
                return instrumento;

            if (instrumento == null)
            {
                instrumento = new Instrumento
                {
                    Nombre = nombre,
                    TipoInstrumento = tipo,
                    EsPlantilla = true,
                    Activo = true
                };
                _context.Instrumentos.Add(instrumento);
            }

            instrumento.Categorias.Add(new Categoria
            {
                Nombre = tipo == TipoInstrumento.Extracurricular
                    ? "Desempeño en actividad extracurricular"
                    : "Desempeño docente",
                Orden = 1,
                Preguntas = CrearPreguntasMock(tipo)
            });

            await _context.SaveChangesAsync();
            return instrumento;
        }

        private async Task<Proceso> ObtenerOCrearProcesoMock(int instrumentoCurricularId, int instrumentoExtracurricularId)
        {
            const string nombreProceso = "Proceso Mock Reportes May-Ago 2026";

            var proceso = await _context.Procesos
                .Include(p => p.ProcesoInstrumentos)
                .FirstOrDefaultAsync(p => p.Nombre == nombreProceso);

            if (proceso == null)
            {
                proceso = new Proceso
                {
                    Nombre = nombreProceso,
                    Anio = 2026,
                    Periodo = 3,
                    FechaInicio = new DateOnly(2026, 5, 1),
                    FechaFin = new DateOnly(2026, 8, 31),
                    Activo = false,
                    InstrumentoId = instrumentoCurricularId
                };

                _context.Procesos.Add(proceso);
                await _context.SaveChangesAsync();
            }
            else
            {
                proceso.InstrumentoId = instrumentoCurricularId;
            }

            if (!proceso.ProcesoInstrumentos.Any(pi => pi.InstrumentoId == instrumentoCurricularId))
            {
                proceso.ProcesoInstrumentos.Add(new ProcesoInstrumento
                {
                    ProcesoId = proceso.Id,
                    InstrumentoId = instrumentoCurricularId,
                    Orden = 1
                });
            }

            if (!proceso.ProcesoInstrumentos.Any(pi => pi.InstrumentoId == instrumentoExtracurricularId))
            {
                proceso.ProcesoInstrumentos.Add(new ProcesoInstrumento
                {
                    ProcesoId = proceso.Id,
                    InstrumentoId = instrumentoExtracurricularId,
                    Orden = 2
                });
            }

            await _context.SaveChangesAsync();
            return proceso;
        }

        private async Task<Instrumento> ObtenerOCrearInstrumentoDetalladoMock(string nombre, TipoInstrumento tipo)
        {
            var instrumento = await _context.Instrumentos
                .Include(i => i.Categorias)
                    .ThenInclude(c => c.Preguntas)
                        .ThenInclude(p => p.Opciones)
                .FirstOrDefaultAsync(i => i.Nombre == nombre && i.TipoInstrumento == tipo);

            if (instrumento != null && instrumento.Categorias.SelectMany(c => c.Preguntas).Any())
                return instrumento;

            if (instrumento == null)
            {
                instrumento = new Instrumento
                {
                    Nombre = nombre,
                    TipoInstrumento = tipo,
                    EsPlantilla = true,
                    Activo = true
                };
                _context.Instrumentos.Add(instrumento);
            }

            foreach (var categoria in CrearCategoriasDetalladas(tipo))
                instrumento.Categorias.Add(categoria);

            await _context.SaveChangesAsync();
            return instrumento;
        }

        private async Task<Proceso> ObtenerOCrearProcesoDetalladoMock(int instrumentoCurricularId, int instrumentoExtracurricularId)
        {
            const string nombreProceso = "Proceso Mock Reporte Detallado Ene-Abr 2026";

            var proceso = await _context.Procesos
                .Include(p => p.ProcesoInstrumentos)
                .FirstOrDefaultAsync(p => p.Nombre == nombreProceso);

            if (proceso == null)
            {
                proceso = new Proceso
                {
                    Nombre = nombreProceso,
                    Anio = 2026,
                    Periodo = 2,
                    FechaInicio = new DateOnly(2026, 1, 1),
                    FechaFin = new DateOnly(2026, 4, 30),
                    Activo = false,
                    InstrumentoId = instrumentoCurricularId
                };

                _context.Procesos.Add(proceso);
                await _context.SaveChangesAsync();
            }
            else
            {
                proceso.InstrumentoId = instrumentoCurricularId;
            }

            if (!proceso.ProcesoInstrumentos.Any(pi => pi.InstrumentoId == instrumentoCurricularId))
            {
                proceso.ProcesoInstrumentos.Add(new ProcesoInstrumento
                {
                    ProcesoId = proceso.Id,
                    InstrumentoId = instrumentoCurricularId,
                    Orden = 1
                });
            }

            if (!proceso.ProcesoInstrumentos.Any(pi => pi.InstrumentoId == instrumentoExtracurricularId))
            {
                proceso.ProcesoInstrumentos.Add(new ProcesoInstrumento
                {
                    ProcesoId = proceso.Id,
                    InstrumentoId = instrumentoExtracurricularId,
                    Orden = 2
                });
            }

            await _context.SaveChangesAsync();
            return proceso;
        }

        private async Task<List<Pregunta>> ObtenerPreguntas(int instrumentoId)
        {
            return await _context.Preguntas
                .Include(p => p.Categoria)
                .Include(p => p.Opciones)
                .Where(p => p.Categoria.InstrumentoId == instrumentoId)
                .OrderBy(p => p.Categoria.Orden)
                .ThenBy(p => p.Orden)
                .ToListAsync();
        }

        private static List<Pregunta> CrearPreguntasMock(TipoInstrumento tipo)
        {
            var textos = tipo == TipoInstrumento.Extracurricular
                ? new[]
                {
                    "El maestro organiza la actividad con puntualidad y claridad.",
                    "El maestro promueve la participacion y el respeto entre estudiantes.",
                    "La actividad aporta al desarrollo fisico, cultural o artistico.",
                    "El maestro da seguimiento adecuado al desempeno del grupo.",
                    "Recomendaria esta actividad extracurricular a otros estudiantes."
                }
                : new[]
                {
                    "El maestro explica los temas con claridad.",
                    "El maestro relaciona la materia con situaciones reales o profesionales.",
                    "El maestro utiliza actividades y recursos que apoyan el aprendizaje.",
                    "El maestro evalua de manera clara y coherente con lo visto en clase.",
                    "El maestro fomenta la participacion y resuelve dudas oportunamente."
                };

            return textos.Select((texto, index) => new Pregunta
            {
                Texto = texto,
                Tipo = PreguntaTipo.Cerrada,
                Orden = index + 1,
                Opciones = CrearOpcionesLikert()
            }).ToList();
        }

        private static List<OpcionRespuesta> CrearOpcionesLikert()
        {
            return new List<OpcionRespuesta>
            {
                new() { Valor = 1, Texto = "Nunca" },
                new() { Valor = 2, Texto = "Casi nunca" },
                new() { Valor = 3, Texto = "A veces" },
                new() { Valor = 4, Texto = "Casi siempre" },
                new() { Valor = 5, Texto = "Siempre" }
            };
        }

        private static List<Categoria> CrearCategoriasDetalladas(TipoInstrumento tipo)
        {
            var categorias = tipo == TipoInstrumento.Extracurricular
                ? new[]
                {
                    new CategoriaMock("PLANEACIÓN", 1, new[]
                    {
                        new PreguntaMock(1, "Entregó encuadre donde se identifican los contenidos de la materia, las actividades y los criterios de evaluación (60% práctico y 40% teórico).", PreguntaTipo.Cerrada),
                        new PreguntaMock(2, "Asiste de forma continua a las sesiones de clase (recuperando los días que no asistió, con el fin de ponerse al corriente con el contenido del programa.", PreguntaTipo.Cerrada)
                    }),
                    new CategoriaMock("METODOLOGÍA", 2, new[]
                    {
                        new PreguntaMock(3, "Muestra dominio de la disciplina, al ofrecer explicaciones claras, ejemplificando la técnica o el entrenamiento", PreguntaTipo.Cerrada),
                        new PreguntaMock(4, "Utiliza diversas estrategias (individual o en equipo), métodos y materiales (deportivo o instrumentos) para el logro del aprendizaje de la disciplina.", PreguntaTipo.Cerrada),
                        new PreguntaMock(5, "Realiza acciones que complementan la formación integral en aspectos como: trabajo en equipo, capacidad crítica. Así como la participación en actividades y eventos relacionados con la disciplina.", PreguntaTipo.Cerrada)
                    }),
                    new CategoriaMock("ACTITUD", 3, new[]
                    {
                        new PreguntaMock(6, "Se conduce de forma respetuosa, dentro y fuera de la clase.", PreguntaTipo.Cerrada),
                        new PreguntaMock(7, "Se muestra accesible y/o con disposición para resolver dudas por medio de asesorías, consultas grupales o personales.", PreguntaTipo.Cerrada),
                        new PreguntaMock(8, "Muestra un cuidado en la imagen y/o vestimenta que proyecta, con relación a su función docente.", PreguntaTipo.Cerrada)
                    }),
                    new CategoriaMock("EVALUACIÓN", 4, new[]
                    {
                        new PreguntaMock(9, "Especifica y aplica los criterios de evaluación de cada unidad de aprendizaje.", PreguntaTipo.Cerrada),
                        new PreguntaMock(10, "Realiza actividades para guiar y dar seguimiento al proceso de aprendizaje (prácticas, demostración de la disciplina, exámenes).", PreguntaTipo.Cerrada)
                    }),
                    new CategoriaMock("RECOMENDACIÓN", 5, new[]
                    {
                        new PreguntaMock(11, "Describe alguna recomendación que consideres necesaria para complementar la mejora del desempeño del docente:", PreguntaTipo.Abierta)
                    })
                }
                : new[]
                {
                    new CategoriaMock("PLANEACIÓN", 1, new[]
                    {
                        new PreguntaMock(1, "Entregó encuadre donde se identifican los contenidos de la materia, las actividades y los criterios de evaluación (60% práctico y 40% teórico).", PreguntaTipo.Cerrada),
                        new PreguntaMock(2, "Asiste de forma continua a las sesiones de clase (recuperando los días que no asistió, con el fin de ponerse al corriente con el programa.", PreguntaTipo.Cerrada)
                    }),
                    new CategoriaMock("METODOLOGÍA", 2, new[]
                    {
                        new PreguntaMock(3, "Muestra dominio de la materia al ofrecer explicaciones claras, lógicas y fundamentadas.", PreguntaTipo.Cerrada),
                        new PreguntaMock(4, "Utiliza diversas estrategias (exposiciones, prácticas, lecturas, casos, entre otros), métodos y materiales (incluyendo el uso de las TIC´s, para impulsar el aprendizaje.", PreguntaTipo.Cerrada),
                        new PreguntaMock(5, "Realiza actividades que complementan la formación profesional técnica en aspectos como: comunicación oral y escrita, trabajo en equipo, capacidad crítica y ética profesional.", PreguntaTipo.Cerrada)
                    }),
                    new CategoriaMock("ACTITUD", 3, new[]
                    {
                        new PreguntaMock(6, "Se conduce de forma respetuosa, dentro y fuera del aula.", PreguntaTipo.Cerrada),
                        new PreguntaMock(7, "Se muestra accesible y/o con disposición para resolver dudas por medio de asesorías académicas, consultas grupales o personales.", PreguntaTipo.Cerrada),
                        new PreguntaMock(8, "Muestra un cuidado en la imagen y/o vestimenta que proyecta, con relación a su función docente.", PreguntaTipo.Cerrada)
                    }),
                    new CategoriaMock("EVALUACIÓN", 4, new[]
                    {
                        new PreguntaMock(9, "Específica y aplica los criterios de evaluación de cada unidad de aprendizaje.", PreguntaTipo.Cerrada),
                        new PreguntaMock(10, "Realiza actividades para guiar y dar seguimiento al proceso de aprendizaje en el aula (atención al alumno, control de disciplina, participación del alumno.", PreguntaTipo.Cerrada)
                    })
                };

            return categorias
                .Select(c => new Categoria
                {
                    Nombre = c.Nombre,
                    Orden = c.Orden,
                    Preguntas = c.Preguntas.Select(CrearPreguntaDetallada).ToList()
                })
                .ToList();
        }

        private static Pregunta CrearPreguntaDetallada(PreguntaMock item)
        {
            return new Pregunta
            {
                Texto = item.Texto,
                Tipo = item.Tipo,
                Orden = item.Orden,
                Opciones = item.Tipo == PreguntaTipo.Cerrada
                    ? CrearOpcionesLikertOficial()
                    : new List<OpcionRespuesta>()
            };
        }

        private static List<OpcionRespuesta> CrearOpcionesLikertOficial()
        {
            return new List<OpcionRespuesta>
            {
                new() { Valor = 1, Texto = "Nunca" },
                new() { Valor = 2, Texto = "Pocas veces" },
                new() { Valor = 3, Texto = "Regularmente" },
                new() { Valor = 4, Texto = "Casi siempre" },
                new() { Valor = 5, Texto = "Siempre" }
            };
        }

        private static string CrearComentarioDetallado(MockReporteRow row, int index)
        {
            if (row.TipoInstrumento == TipoInstrumento.Extracurricular)
            {
                return index % 8 == 0
                    ? "La actividad motiva al grupo y ayuda a mejorar la disciplina."
                    : "El docente da seguimiento y organiza bien las prácticas.";
            }

            return index % 8 == 0
                ? "Explica con claridad y relaciona la materia con ejemplos de la carrera."
                : "Conviene reforzar algunos temas con más ejercicios prácticos.";
        }

        private static int CalcularValor(double promedioBase, int evaluacionIndex, int preguntaOrden)
        {
            var ajuste = ((evaluacionIndex + preguntaOrden) % 3) - 1;
            return Math.Clamp((int)Math.Round(promedioBase + (ajuste * 0.35)), 1, 5);
        }

        private record MockAcademia(string Clave, string Nombre, bool EsExtracurricular);

        private record MockMaestro(
            string AcademiaClave,
            string AcademiaNombre,
            string IdMaestro,
            string NombreMaestro);

        private record MockMateria(
            string MateriaId,
            string NombreMateria,
            int HorasTotales,
            bool NaturalezaNoCurricular,
            int TipoMateria,
            string CarreraClave);

        private record CategoriaMock(
            string Nombre,
            int Orden,
            PreguntaMock[] Preguntas);

        private record PreguntaMock(
            int Orden,
            string Texto,
            PreguntaTipo Tipo);

        private record MockReporteRow(
            string IdMaestro,
            string NombreMaestro,
            string MateriaId,
            string NombreMateria,
            string Grupo,
            TipoInstrumento TipoInstrumento,
            string AcademiaClave,
            string AcademiaNombre,
            string CarreraClave,
            int TotalEvaluaciones,
            double PromedioBase);
    }
}
