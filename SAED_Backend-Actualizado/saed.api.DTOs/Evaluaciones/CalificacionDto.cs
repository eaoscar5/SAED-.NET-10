using saed.api.Model;

namespace saed.api.DTOs.Evaluaciones
{
    public class CalificacionMaestroDto
    {
        public string IdMaestro { get; set; } = string.Empty;
        public string NombreMaestro { get; set; } = string.Empty;
        public int ProcesoId { get; set; }
        public string? MateriaId { get; set; }
        public string? Grupo { get; set; }
        public int? InstrumentoId { get; set; }
        public bool FiltradoPorMateria { get; set; }
        public int TotalEvaluaciones { get; set; }
        public double CalificacionGlobal { get; set; }
        public TipoInstrumento? TipoInstrumento { get; set; }

        public List<CalificacionPorMateriaDto> PorMateria { get; set; } = new();
    }

    public class CalificacionPorMateriaDto
    {
        public string MateriaId { get; set; } = string.Empty;
        public string NombreMateria { get; set; } = string.Empty;
        public string Grupo { get; set; } = string.Empty;
        public TipoInstrumento TipoInstrumento { get; set; }
        public int? InstrumentoId { get; set; }
        public string? AcademiaClave { get; set; }
        public string? AcademiaNombre { get; set; }
        public string? CarreraClave { get; set; }
        public int TotalEvaluaciones { get; set; }
        public double Calificacion { get; set; }

        public List<CalificacionPreguntaDto> PorPregunta { get; set; } = new();
        public List<string> Comentarios { get; set; } = new();
    }

    public class CalificacionPreguntaDto
    {
        public int PreguntaId { get; set; }
        public int Orden { get; set; }
        public int CategoriaId { get; set; }
        public int CategoriaOrden { get; set; }
        public string CategoriaNombre { get; set; } = string.Empty;
        public string TextoPregunta { get; set; } = string.Empty;
        public double Promedio { get; set; }
        public int TotalRespuestas { get; set; }
        public Dictionary<string, int> Distribucion { get; set; } = new();
    }

    public class ResumenCalificacionDto
    {
        public string IdMaestro { get; set; } = string.Empty;
        public string NombreMaestro { get; set; } = string.Empty;
        public int TotalEvaluaciones { get; set; }
        public double CalificacionGlobal { get; set; }
        public int TotalMaterias { get; set; }
        public int TotalMateriasCurriculares { get; set; }
        public int TotalMateriasExtracurriculares { get; set; }
    }
}
