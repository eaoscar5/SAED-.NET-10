using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace saed.api.Model
{
    public class Academia
    {
        public int Id { get; set; }

        public string AcaClave { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public bool EsExtracurricular { get; set; }

        public DateTime? FechaSincronizacionUtc { get; set; }
    }
}
