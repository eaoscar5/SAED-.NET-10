using saed.api.DTOs.Universidad;
using System.Text.Json;

namespace saed.api.Services.Universidad
{
    public interface IUniversidadApiService
    {
        Task<string> Authenticate(string username, string password);

        Task<AlumnoUniversidad?> GetStudentData(string token, string matricula);

        Task<List<MateriaAlumnoDto>> GetStudentSubjects(
            string? token,
            string matricula,
            int anio,
            int periodo);

        Task<JsonElement> GetCuatrimestres(string token);

        Task<bool> CanReadCatalogs(string token);

        Task<List<AcademiaUniversidadDto>> GetAcademias(string? token = null);

        Task<List<MaestroAcademiaUniversidadDto>> GetMaestrosPorAcademia(
            string acaClave,
            string? token = null);

        Task<List<MaestroAcademiaUniversidadDto>> GetTodosMaestrosPorAcademia(string? token = null);

        Task<List<MateriaUniversidadDto>> GetMaterias(string? token = null);

        Task<List<GrupoActivoUniversidadDto>> GetGruposActivosPorPeriodo(
            int anio,
            int periodo,
            string? token = null);

        Task<List<MateriaMaestroGrupoUniversidadDto>> GetMateriasMaestroPorGrupo(
            string grupo,
            int anio,
            int periodo,
            string? token = null);

        Task<List<AlumnoGrupoPeriodoUniversidadDto>> GetAlumnosPorGrupoPeriodo(
            int anio,
            int periodo,
            string? token = null);
    }
}
