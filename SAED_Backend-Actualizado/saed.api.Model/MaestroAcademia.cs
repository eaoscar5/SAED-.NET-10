namespace saed.api.Model
{
    public class MaestroAcademia
    {
        public int Id { get; set; }

        public string AcaClave { get; set; } = string.Empty;

        public string NombreAcademia { get; set; } = string.Empty;

        public string IdMaestro { get; set; } = string.Empty;

        public string NombreMaestro { get; set; } = string.Empty;

        public DateTime FechaSincronizacionUtc { get; set; } = DateTime.UtcNow;
    }
}
