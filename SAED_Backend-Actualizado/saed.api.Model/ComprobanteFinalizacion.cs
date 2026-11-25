namespace saed.api.Model
{
    public class ComprobanteFinalizacion
    {
        public int Id { get; set; }

        public int ProcesoId { get; set; }

        public string MatriculaAlumno { get; set; } = string.Empty;

        public string Grupo { get; set; } = string.Empty;

        public string Folio { get; set; } = string.Empty;

        public DateTime FechaFinalizacion { get; set; } = DateTime.UtcNow;

        public Proceso Proceso { get; set; } = null!;
    }
}
