namespace saed.api.Model
{
    public class ProcesoInstrumento
    {
        public int ProcesoId { get; set; }

        public int InstrumentoId { get; set; }

        public int Orden { get; set; }

        public Proceso Proceso { get; set; } = null!;

        public Instrumento Instrumento { get; set; } = null!;
    }
}
