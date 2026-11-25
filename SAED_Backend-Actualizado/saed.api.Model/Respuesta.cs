namespace saed.api.Model
{
    public class Respuesta
    {
        public int Id { get; set; }

        public int EvaluacionId { get; set; }
        public int PreguntaId { get; set; }
        public int? OpcionId { get; set; }
        public string? Texto { get; set; }

        // ── Navegación ──
        public Evaluacion Evaluacion { get; set; } = null!;
        public Pregunta Pregunta { get; set; } = null!;
        public OpcionRespuesta? Opcion { get; set; } // ← para leer texto y valor en reportes
    }
}
