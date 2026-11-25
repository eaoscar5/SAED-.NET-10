namespace saed.api.Model
{
    public class Evaluacion
    {
        public int Id { get; set; }

        public int ProcesoId { get; set; }

        public string MatriculaAlumno { get; set; } = string.Empty;

        public string AsignacionAcademicaId { get; set; } = string.Empty;

        // ── Datos del maestro desnormalizados (para reportes sin consultar API externa) ──
        public string IdMaestro { get; set; } = string.Empty;
        public string NombreMaestro { get; set; } = string.Empty;

        // ── Datos de la materia desnormalizados ──
        public string MateriaId { get; set; } = string.Empty;
        public string NombreMateria { get; set; } = string.Empty;
        public string Grupo { get; set; } = string.Empty;

        // ── Clasificación congelada al momento de evaluar ──
        public int? InstrumentoId { get; set; }
        public TipoInstrumento TipoInstrumento { get; set; } = TipoInstrumento.Curricular;
        public string? AcademiaClave { get; set; }
        public string? AcademiaNombre { get; set; }
        public string? CarreraClave { get; set; }
        public int? TipoMateria { get; set; }
        public bool MateriaNoCurricular { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        // ── Relaciones ──
        public Proceso Proceso { get; set; } = null!;
        public List<Respuesta> Respuestas { get; set; } = new();
        public Comentario? Comentario { get; set; }
    }
}
