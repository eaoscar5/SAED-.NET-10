using saed.api.Data.Repositories;
using saed.api.DTOs.Procesos;
using saed.api.Model;

namespace saed.api.Services
{
    public class ProcesoService
    {
        private readonly ProcesoRepository _repository;

        public ProcesoService(ProcesoRepository repository)
        {
            _repository = repository;
        }

        // ── Crear ─────────────────────────────────────────────────────────────
        public async Task<ProcesoDto> CrearProceso(CrearProcesoDto dto)
        {
            // Validar que no haya otro proceso activo
            var activo = await _repository.GetActivo();
            if (activo != null)
                throw new Exception("Ya existe un proceso activo. Ciérralo antes de crear uno nuevo.");

            // Validar periodo
            if (dto.Periodo < 1 || dto.Periodo > 3)
                throw new Exception("Periodo inválido. Debe ser 1, 2 o 3.");

            // Validar fechas
            if (dto.FechaInicio >= dto.FechaFin)
                throw new Exception("La fecha de inicio debe ser anterior a la fecha fin.");

            var instrumentoIds = NormalizarInstrumentoIds(dto);

            if (!instrumentoIds.Any())
                throw new Exception("Debes seleccionar al menos un instrumento de evaluación.");

            var existentes = await _repository.ObtenerInstrumentosExistentes(instrumentoIds);
            if (existentes.Count != instrumentoIds.Count)
                throw new Exception("Uno o más instrumentos seleccionados no existen.");

            // Generar nombre automático: "Oficial N YYYY"
            var procesosDelAnio = await _repository.GetByAnio(dto.Anio);
            var consecutivo = procesosDelAnio.Any() ? procesosDelAnio.Count + 1 : 1;
            var nombre = $"Oficial {consecutivo} {dto.Anio}";

            var proceso = new Proceso
            {
                Anio = dto.Anio,
                Periodo = dto.Periodo,
                Nombre = nombre,
                FechaInicio = dto.FechaInicio,
                FechaFin = dto.FechaFin,
                Activo = true,
                InstrumentoId = instrumentoIds.First(),
                ProcesoInstrumentos = instrumentoIds
                    .Select((instrumentoId, index) => new ProcesoInstrumento
                    {
                        InstrumentoId = instrumentoId,
                        Orden = index + 1
                    })
                    .ToList()
            };

            await _repository.Add(proceso);
            var creado = await _repository.GetById(proceso.Id)
                ?? throw new Exception("No se pudo recargar el proceso creado.");

            return MapToDto(creado);
        }

        // ── Consultas ─────────────────────────────────────────────────────────
        public async Task<List<ProcesoDto>> ObtenerProcesos()
        {
            var procesos = await _repository.GetAll();
            return procesos.Select(MapToDto).ToList();
        }

        public async Task<ProcesoDto?> ObtenerActivo()
        {
            var proceso = await _repository.GetActivo();
            return proceso == null ? null : MapToDto(proceso);
        }

        public async Task<ProcesoDto> ObtenerPorId(int id)
        {
            var proceso = await _repository.GetById(id);
            if (proceso == null)
                throw new Exception("Proceso no encontrado.");

            return MapToDto(proceso);
        }

        // ── Acciones ──────────────────────────────────────────────────────────
        public async Task<ProcesoDto> CerrarProceso(int id)
        {
            var proceso = await _repository.GetById(id);

            if (proceso == null)
                throw new Exception("Proceso no encontrado.");

            if (!proceso.Activo)
                throw new Exception("El proceso ya está cerrado.");

            proceso.Activo = false;
            await _repository.Update(proceso);

            return MapToDto(proceso);
        }

        public async Task<ProcesoDto> ActivarProceso(int id)
        {
            // Cerrar el proceso activo actual si existe
            var actual = await _repository.GetActivo();
            if (actual != null && actual.Id != id)
            {
                actual.Activo = false;
                await _repository.Update(actual);
            }

            var proceso = await _repository.GetById(id);
            if (proceso == null)
                throw new Exception("Proceso no encontrado.");

            proceso.Activo = true;
            await _repository.Update(proceso);

            return MapToDto(proceso);
        }

        // ── Mapeo ─────────────────────────────────────────────────────────────
        private static ProcesoDto MapToDto(Proceso p)
        {
            var instrumentoIds = p.ProcesoInstrumentos
                .OrderBy(pi => pi.Orden)
                .Select(pi => pi.InstrumentoId)
                .ToList();

            var nombresInstrumentos = p.ProcesoInstrumentos
                .OrderBy(pi => pi.Orden)
                .Select(pi => pi.Instrumento?.Nombre)
                .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
                .Cast<string>()
                .ToList();

            if (!instrumentoIds.Any() && p.InstrumentoId.HasValue)
                instrumentoIds.Add(p.InstrumentoId.Value);

            if (!nombresInstrumentos.Any() && !string.IsNullOrWhiteSpace(p.Instrumento?.Nombre))
                nombresInstrumentos.Add(p.Instrumento.Nombre);

            return new ProcesoDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Anio = p.Anio,
                Periodo = p.Periodo,
                FechaInicio = p.FechaInicio,
                FechaFin = p.FechaFin,
                Activo = p.Activo,
                InstrumentoId = p.InstrumentoId,
                NombreInstrumento = p.Instrumento?.Nombre,
                InstrumentoIds = instrumentoIds,
                NombresInstrumentos = nombresInstrumentos
            };
        }

        private static List<int> NormalizarInstrumentoIds(CrearProcesoDto dto)
        {
            var ids = dto.InstrumentoIds ?? new List<int>();

            if (!ids.Any() && dto.InstrumentoId > 0)
                ids.Add(dto.InstrumentoId);

            return ids
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }
    }
}
