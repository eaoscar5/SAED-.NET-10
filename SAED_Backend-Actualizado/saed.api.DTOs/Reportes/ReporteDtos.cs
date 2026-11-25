using saed.api.Model;

namespace saed.api.DTOs.Reportes
{
    public class ReporteFiltroDto
    {
        public int? ProcesoId { get; set; }

        public string? AcademiaClave { get; set; }

        public string? CarreraClave { get; set; }

        public string? Grupo { get; set; }

        public string? NombreMaestro { get; set; }

        public TipoInstrumento? TipoInstrumento { get; set; }

        public bool VistaGlobal { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 25;
    }

    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();

        public int Total { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }
    }

    public class ReporteResumenDto
    {
        public int ProcesoId { get; set; }

        public string NombreProceso { get; set; } = string.Empty;

        public int Anio { get; set; }

        public int Periodo { get; set; }

        public string IdMaestro { get; set; } = string.Empty;

        public string NombreMaestro { get; set; } = string.Empty;

        public string MateriaId { get; set; } = string.Empty;

        public string NombreMateria { get; set; } = string.Empty;

        public string Grupo { get; set; } = string.Empty;

        public TipoInstrumento TipoInstrumento { get; set; }

        public int? InstrumentoId { get; set; }

        public string? AcademiaClave { get; set; }

        public string? AcademiaNombre { get; set; }

        public string? CarreraClave { get; set; }

        public int TotalEvaluaciones { get; set; }

        public double CalificacionPromedio { get; set; }

        public DateTime UltimaEvaluacion { get; set; }
    }

    public class ReporteCatalogosDto
    {
        public List<ReporteCatalogoItemDto> Academias { get; set; } = new();

        public List<ReporteCatalogoItemDto> Carreras { get; set; } = new();

        public List<ReporteCatalogoItemDto> Grupos { get; set; } = new();

        public bool CatalogosLocalesDisponibles { get; set; }

        public bool SincronizacionRequerida { get; set; }

        public bool SincronizacionIntentada { get; set; }

        public bool GruposDesdeUniversidad { get; set; }

        public DateTime? FechaUltimaSincronizacionUtc { get; set; }

        public string? Mensaje { get; set; }
    }

    public class ReporteCatalogoItemDto
    {
        public string Clave { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string? AcademiaClave { get; set; }

        public string? CarreraClave { get; set; }
    }

    public class ReporteMockSeedDto
    {
        public int ProcesoId { get; set; }

        public string NombreProceso { get; set; } = string.Empty;

        public int Academias { get; set; }

        public int MaestrosAcademia { get; set; }

        public int Materias { get; set; }

        public int Evaluaciones { get; set; }

        public int Reportes { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    public class ReporteProgresoGrupoFiltroDto
    {
        public int? ProcesoId { get; set; }

        public string? AcademiaClave { get; set; }

        public string? CarreraClave { get; set; }

        public string? Grupo { get; set; }
    }

    public class ReporteProgresoGrupoResumenDto
    {
        public int ProcesoId { get; set; }

        public string NombreProceso { get; set; } = string.Empty;

        public int Anio { get; set; }

        public int Periodo { get; set; }

        public string Grupo { get; set; } = string.Empty;

        public string NombreGrupo { get; set; } = string.Empty;

        public int Cuatrimestre { get; set; }

        public string AcademiaClave { get; set; } = string.Empty;

        public string AcademiaNombre { get; set; } = string.Empty;

        public string CarreraClave { get; set; } = string.Empty;

        public string CarreraNombre { get; set; } = string.Empty;

        public int TotalAlumnos { get; set; }

        public int AlumnosEvaluaron { get; set; }

        public int AlumnosFaltantes { get; set; }

        public int TotalEvaluacionesRegistradas { get; set; }

        public double PorcentajeEvaluacion { get; set; }
    }

    public class ReporteProgresoGrupoDetalleDto : ReporteProgresoGrupoResumenDto
    {
        public int MateriasAsignadas { get; set; }

        public List<ReporteProgresoMateriaDto> Materias { get; set; } = new();

        public List<ReporteProgresoAlumnoDto> Alumnos { get; set; } = new();
    }

    public class ReporteProgresoMateriaDto
    {
        public string IdMaestro { get; set; } = string.Empty;

        public string NombreMaestro { get; set; } = string.Empty;

        public string MateriaId { get; set; } = string.Empty;

        public string NombreMateria { get; set; } = string.Empty;

        public TipoInstrumento TipoInstrumento { get; set; }

        public int? InstrumentoId { get; set; }

        public string? AcademiaClave { get; set; }

        public string? AcademiaNombre { get; set; }

        public string? CarreraClave { get; set; }
    }

    public class ReporteProgresoAlumnoDto
    {
        public string Matricula { get; set; } = string.Empty;

        public string NombreAlumno { get; set; } = string.Empty;

        public int EvaluacionesEsperadas { get; set; }

        public int EvaluacionesRealizadas { get; set; }

        public int EvaluacionesFaltantes { get; set; }

        public bool Evaluado { get; set; }

        public DateTime? UltimaEvaluacion { get; set; }
    }
}
