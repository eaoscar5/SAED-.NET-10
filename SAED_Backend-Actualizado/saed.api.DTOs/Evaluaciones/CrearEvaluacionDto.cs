using saed.api.Model;

namespace saed.api.DTOs.Evaluaciones
{
    public class CrearEvaluacionDto
    {
        public string AsignacionAcademicaId { get; set; } = string.Empty;

        public int InstrumentoId { get; set; }

        public List<RespuestaDto> Respuestas { get; set; } = new();

        public string? Comentario { get; set; }
    }

    public class RespuestaDto
    {
        public int PreguntaId { get; set; }

        public int? OpcionId { get; set; }

        public string? Texto { get; set; }
    }

    public class EvaluacionCreadaDto
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        public FinalizacionEvaluacionesDto Finalizacion { get; set; } = new();
    }

    public class FinalizacionEvaluacionesDto
    {
        public bool Completada { get; set; }

        public string? Folio { get; set; }

        public DateTime? FechaFinalizacion { get; set; }

        public int TotalEvaluaciones { get; set; }

        public int EvaluacionesCompletadas { get; set; }

        public string Grupo { get; set; } = string.Empty;

        public string NombreProceso { get; set; } = string.Empty;

        public int Anio { get; set; }

        public int Periodo { get; set; }
    }

    public class EvaluacionDisponibleDto
    {
        public string AsignacionAcademicaId { get; set; } = string.Empty;

        public string IdMaestro { get; set; } = string.Empty;

        public string NombreMaestro { get; set; } = string.Empty;

        public string MateriaId { get; set; } = string.Empty;

        public string Materia { get; set; } = string.Empty;

        public string Grupo { get; set; } = string.Empty;

        public int? InstrumentoId { get; set; }

        public TipoInstrumento TipoInstrumento { get; set; } = TipoInstrumento.Curricular;

        public string? AcademiaClave { get; set; }

        public string? AcademiaNombre { get; set; }

        public string? CarreraClave { get; set; }

        public int? TipoMateria { get; set; }

        public bool MateriaNoCurricular { get; set; }

        public bool YaEvaluado { get; set; }
    }

    public class ReporteMaestroDto
    {
        public string IdMaestro { get; set; } = string.Empty;

        public string NombreMaestro { get; set; } = string.Empty;

        public int ProcesoId { get; set; }

        public string NombreProceso { get; set; } = string.Empty;

        public int TotalEvaluaciones { get; set; }

        public List<ReportePreguntaDto> Preguntas { get; set; } = new();

        public List<string> Comentarios { get; set; } = new();
    }

    public class ReportePreguntaDto
    {
        public int PreguntaId { get; set; }

        public string TextoPregunta { get; set; } = string.Empty;

        public double Promedio { get; set; }

        public Dictionary<string, int> Distribucion { get; set; } = new();
    }
}
