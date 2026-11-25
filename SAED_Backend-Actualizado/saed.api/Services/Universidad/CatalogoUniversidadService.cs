using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using saed.api.Data;
using saed.api.DTOs.Universidad;
using saed.api.Model;
using System.Globalization;
using System.Text;

namespace saed.api.Services.Universidad
{
    public class CatalogoUniversidadService
    {
        private readonly AppDbContext _context;
        private readonly IUniversidadApiService _universidadApi;
        private readonly UniversidadApiOptions _options;

        public CatalogoUniversidadService(
            AppDbContext context,
            IUniversidadApiService universidadApi,
            IOptions<UniversidadApiOptions> options)
        {
            _context = context;
            _universidadApi = universidadApi;
            _options = options.Value;
        }

        public async Task<SincronizacionCatalogosDto> SincronizarCatalogos(string? tokenUniversidad = null)
        {
            var fecha = DateTime.UtcNow;
            var academiasApi = await _universidadApi.GetAcademias(tokenUniversidad);
            var maestrosApi = await _universidadApi.GetTodosMaestrosPorAcademia(tokenUniversidad);
            var materiasApi = await _universidadApi.GetMaterias(tokenUniversidad);
            var academiaExtracurricular = _options.ExtracurricularAcademiaClave.Trim();

            ValidarCatalogosRecibidos(academiasApi, maestrosApi, materiasApi);

            await using var tx = await _context.Database.BeginTransactionAsync();

            await UpsertAcademias(academiasApi, academiaExtracurricular, fecha);
            await ReemplazarMaestrosAcademia(maestrosApi, fecha);
            await UpsertMaterias(materiasApi, fecha);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return new SincronizacionCatalogosDto
            {
                Academias = academiasApi.Count,
                MaestrosAcademia = maestrosApi.Count,
                Materias = materiasApi.Count,
                FechaSincronizacionUtc = fecha
            };
        }

        private static void ValidarCatalogosRecibidos(
            List<AcademiaUniversidadDto> academias,
            List<MaestroAcademiaUniversidadDto> maestros,
            List<MateriaUniversidadDto> materias)
        {
            if (academias.Count > 0 && maestros.Count > 0 && materias.Count > 0)
                return;

            throw new InvalidOperationException(
                "La API de universidad no devolvio catalogos suficientes para sincronizar. " +
                $"Academias: {academias.Count}, maestros por academia: {maestros.Count}, materias: {materias.Count}.");
        }

        public async Task<EvaluacionClasificacion> Clasificar(
            Proceso proceso,
            string idMaestro,
            string materiaId,
            string? nombreMateria = null)
        {
            var materia = await _context.MateriasCatalogo
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MateriaId == materiaId);

            var academiasMaestro = await _context.MaestrosAcademia
                .AsNoTracking()
                .Join(
                    _context.Academias.AsNoTracking(),
                    ma => ma.AcaClave,
                    a => a.AcaClave,
                    (ma, a) => new { Maestro = ma, Academia = a })
                .Where(x => x.Maestro.IdMaestro == idMaestro)
                .OrderBy(x => x.Academia.Nombre)
                .Select(x => new AcademiaClasificacion(
                    x.Academia.AcaClave,
                    x.Academia.Nombre,
                    x.Academia.EsExtracurricular))
                .ToListAsync();

            var academiaExtracurricular = academiasMaestro
                .FirstOrDefault(a => a.EsExtracurricular);
            var academiaCurricular = academiasMaestro
                .FirstOrDefault(a => !a.EsExtracurricular)
                ?? academiasMaestro.FirstOrDefault();

            var esMateriaExtracurricular =
                materia?.TipoMateria == 3 ||
                materia?.NaturalezaNoCurricular == true ||
                (materia == null && NombreSugiereMateriaExtracurricular(nombreMateria));
            var esExtracurricular = academiaExtracurricular != null && esMateriaExtracurricular;
            var tipo = esExtracurricular
                ? TipoInstrumento.Extracurricular
                : TipoInstrumento.Curricular;
            var academiaAplicada = esExtracurricular
                ? academiaExtracurricular
                : academiaCurricular;

            return new EvaluacionClasificacion
            {
                TipoInstrumento = tipo,
                InstrumentoId = SeleccionarInstrumento(proceso, tipo),
                AcademiaClave = academiaAplicada?.AcaClave,
                AcademiaNombre = academiaAplicada?.Nombre,
                CarreraClave = string.IsNullOrWhiteSpace(materia?.CarreraClave) ? null : materia.CarreraClave,
                TipoMateria = materia?.TipoMateria,
                MateriaNoCurricular = materia?.NaturalezaNoCurricular ?? false
            };
        }

        private async Task UpsertAcademias(
            List<AcademiaUniversidadDto> academiasApi,
            string academiaExtracurricular,
            DateTime fecha)
        {
            var existentes = await _context.Academias.ToDictionaryAsync(a => a.AcaClave);

            foreach (var item in academiasApi.Where(a => !string.IsNullOrWhiteSpace(a.AcaClave)))
            {
                if (!existentes.TryGetValue(item.AcaClave, out var academia))
                {
                    academia = new Academia
                    {
                        AcaClave = item.AcaClave
                    };
                    _context.Academias.Add(academia);
                }

                academia.Nombre = item.Nombre;
                academia.EsExtracurricular =
                    item.AcaClave.Equals(academiaExtracurricular, StringComparison.OrdinalIgnoreCase);
                academia.FechaSincronizacionUtc = fecha;
            }
        }

        private async Task ReemplazarMaestrosAcademia(
            List<MaestroAcademiaUniversidadDto> maestrosApi,
            DateTime fecha)
        {
            _context.MaestrosAcademia.RemoveRange(await _context.MaestrosAcademia.ToListAsync());

            var maestros = maestrosApi
                .Where(m => !string.IsNullOrWhiteSpace(m.AcaClave) && !string.IsNullOrWhiteSpace(m.IdMaestro))
                .GroupBy(m => new { m.AcaClave, m.IdMaestro })
                .Select(g =>
                {
                    var item = g.First();
                    return new MaestroAcademia
                    {
                        AcaClave = item.AcaClave,
                        NombreAcademia = item.NombreAcademia,
                        IdMaestro = item.IdMaestro,
                        NombreMaestro = item.NombreMaestro,
                        FechaSincronizacionUtc = fecha
                    };
                })
                .ToList();

            _context.MaestrosAcademia.AddRange(maestros);
        }

        private async Task UpsertMaterias(List<MateriaUniversidadDto> materiasApi, DateTime fecha)
        {
            var existentes = await _context.MateriasCatalogo.ToDictionaryAsync(m => m.MateriaId);

            foreach (var item in materiasApi.Where(m => !string.IsNullOrWhiteSpace(m.MateriaId)))
            {
                if (!existentes.TryGetValue(item.MateriaId, out var materia))
                {
                    materia = new MateriaCatalogo
                    {
                        MateriaId = item.MateriaId
                    };
                    _context.MateriasCatalogo.Add(materia);
                }

                materia.NombreMateria = item.NombreMateria;
                materia.HorasTotales = item.HorasTotales;
                materia.NaturalezaNoCurricular = item.NaturalezaNoCurricular;
                materia.TipoMateria = item.TipoMateria;
                materia.Seriacion = item.Seriacion;
                materia.CarreraClave = item.CarreraClave;
                materia.NumeroCompetencias = item.NumeroCompetencias;
                materia.MateriaIntegradora = item.MateriaIntegradora;
                materia.FechaSincronizacionUtc = fecha;
            }
        }

        private static int? SeleccionarInstrumento(Proceso proceso, TipoInstrumento tipo)
        {
            var instrumento = proceso.ProcesoInstrumentos
                .OrderBy(pi => pi.Orden)
                .Select(pi => pi.Instrumento)
                .FirstOrDefault(i => i != null && i.TipoInstrumento == tipo);

            if (instrumento != null)
                return instrumento.Id;

            if (tipo == TipoInstrumento.Curricular)
                return proceso.InstrumentoId;

            return null;
        }

        private static bool NombreSugiereMateriaExtracurricular(string? nombreMateria)
        {
            if (string.IsNullOrWhiteSpace(nombreMateria))
                return false;

            var nombre = NormalizarNombreMateria(nombreMateria);
            var palabrasClave = new[]
            {
                "arte",
                "artes",
                "cultura",
                "cultural",
                "deporte",
                "deportiva",
                "danza",
                "teatro",
                "futbol",
                "acondicionamiento",
                "musica"
            };

            return palabrasClave.Any(nombre.Contains);
        }

        private static string NormalizarNombreMateria(string value)
        {
            var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    builder.Append(c);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private sealed record AcademiaClasificacion(
            string AcaClave,
            string Nombre,
            bool EsExtracurricular);
    }

    public class EvaluacionClasificacion
    {
        public int? InstrumentoId { get; set; }

        public TipoInstrumento TipoInstrumento { get; set; }

        public string? AcademiaClave { get; set; }

        public string? AcademiaNombre { get; set; }

        public string? CarreraClave { get; set; }

        public int? TipoMateria { get; set; }

        public bool MateriaNoCurricular { get; set; }
    }
}
