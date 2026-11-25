namespace saed.api.DTOs.Universidad
{
    public class AcademiaUniversidadDto
    {
        public string AcaClave { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;
    }

    public class MaestroAcademiaUniversidadDto
    {
        public string AcaClave { get; set; } = string.Empty;

        public string NombreAcademia { get; set; } = string.Empty;

        public string IdMaestro { get; set; } = string.Empty;

        public string NombreMaestro { get; set; } = string.Empty;
    }

    public class MateriaUniversidadDto
    {
        public string MateriaId { get; set; } = string.Empty;

        public string NombreMateria { get; set; } = string.Empty;

        public int HorasTotales { get; set; }

        public bool NaturalezaNoCurricular { get; set; }

        public int TipoMateria { get; set; }

        public string Seriacion { get; set; } = string.Empty;

        public string CarreraClave { get; set; } = string.Empty;

        public int NumeroCompetencias { get; set; }

        public bool MateriaIntegradora { get; set; }
    }

    public class GrupoActivoUniversidadDto
    {
        public string Grupo { get; set; } = string.Empty;

        public string NombreGrupo { get; set; } = string.Empty;

        public int Cuatrimestre { get; set; }

        public string CarreraClave { get; set; } = string.Empty;

        public string CarreraNombre { get; set; } = string.Empty;

        public string AcademiaClave { get; set; } = string.Empty;

        public string AcademiaNombre { get; set; } = string.Empty;

        public int TotalAlumnos { get; set; }
    }

    public class MateriaMaestroGrupoUniversidadDto
    {
        public string IdMaestro { get; set; } = string.Empty;

        public string NombreMaestro { get; set; } = string.Empty;

        public string MateriaId { get; set; } = string.Empty;

        public string NombreMateria { get; set; } = string.Empty;

        public string Grupo { get; set; } = string.Empty;

        public string NombreGrupo { get; set; } = string.Empty;

        public int Anio { get; set; }

        public int Periodo { get; set; }
    }

    public class AlumnoGrupoPeriodoUniversidadDto
    {
        public string AcademiaClave { get; set; } = string.Empty;

        public string AcademiaNombre { get; set; } = string.Empty;

        public string CarreraClave { get; set; } = string.Empty;

        public string CarreraNombre { get; set; } = string.Empty;

        public string Grupo { get; set; } = string.Empty;

        public string NombreGrupo { get; set; } = string.Empty;

        public int Cuatrimestre { get; set; }

        public string Matricula { get; set; } = string.Empty;

        public string ApellidoPaterno { get; set; } = string.Empty;

        public string ApellidoMaterno { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string NombreCompleto =>
            string.Join(" ", new[] { ApellidoPaterno, ApellidoMaterno, Nombre }
                .Where(v => !string.IsNullOrWhiteSpace(v)));
    }

    public class SincronizacionCatalogosDto
    {
        public int Academias { get; set; }

        public int MaestrosAcademia { get; set; }

        public int Materias { get; set; }

        public DateTime FechaSincronizacionUtc { get; set; }
    }
}
