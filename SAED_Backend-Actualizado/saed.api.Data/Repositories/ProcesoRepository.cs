using Microsoft.EntityFrameworkCore;
using saed.api.Model;

namespace saed.api.Data.Repositories
{
    public class ProcesoRepository
    {
        private readonly AppDbContext _context;

        public ProcesoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Proceso>> GetAll()
        {
            return await _context.Procesos
                .Include(p => p.Instrumento)
                .Include(p => p.ProcesoInstrumentos)
                    .ThenInclude(pi => pi.Instrumento)
                .OrderByDescending(p => p.Anio)
                .ToListAsync();
        }

        public async Task<Proceso?> GetActivo()
        {
            return await _context.Procesos
                .Include(p => p.Instrumento)
                .Include(p => p.ProcesoInstrumentos)
                    .ThenInclude(pi => pi.Instrumento)
                .FirstOrDefaultAsync(p => p.Activo);
        }

        public async Task<Proceso?> GetById(int id)
        {
            return await _context.Procesos
                .Include(p => p.Instrumento)
                .Include(p => p.ProcesoInstrumentos)
                    .ThenInclude(pi => pi.Instrumento)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Proceso>> GetByAnio(int anio)
        {
            return await _context.Procesos
                .Where(p => p.Anio == anio)
                .ToListAsync();
        }

        public async Task Add(Proceso proceso)
        {
            _context.Procesos.Add(proceso);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Proceso proceso)
        {
            _context.Procesos.Update(proceso);
            await _context.SaveChangesAsync();
        }

        public async Task<List<int>> ObtenerInstrumentosExistentes(List<int> ids)
        {
            return await _context.Instrumentos
                .Where(i => ids.Contains(i.Id))
                .Select(i => i.Id)
                .ToListAsync();
        }
    }
}
