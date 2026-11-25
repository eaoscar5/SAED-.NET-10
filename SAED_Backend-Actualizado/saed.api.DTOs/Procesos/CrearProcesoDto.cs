using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace saed.api.DTOs.Procesos
{
    public class CrearProcesoDto
    {
        public int Anio { get; set; }

        public int Periodo { get; set; } // 1=Sep-Dic, 2=Ene-Abr, 3=May-Ago

        public DateOnly FechaInicio { get; set; }

        public DateOnly FechaFin { get; set; }

        // Compatibilidad con clientes actuales: si solo llega uno, se usa como lista de un elemento.
        public int InstrumentoId { get; set; }

        public List<int> InstrumentoIds { get; set; } = new();
    }
}
