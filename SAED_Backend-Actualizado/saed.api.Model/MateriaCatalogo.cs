namespace saed.api.Model
{
    public class MateriaCatalogo
    {
        public int Id { get; set; }

        public string MateriaId { get; set; } = string.Empty;

        public string NombreMateria { get; set; } = string.Empty;

        public int HorasTotales { get; set; }

        public bool NaturalezaNoCurricular { get; set; }

        public int TipoMateria { get; set; }

        public string Seriacion { get; set; } = string.Empty;

        public string CarreraClave { get; set; } = string.Empty;

        public int NumeroCompetencias { get; set; }

        public bool MateriaIntegradora { get; set; }

        public DateTime FechaSincronizacionUtc { get; set; } = DateTime.UtcNow;
    }
}
