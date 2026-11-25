namespace saed.api.Services.Universidad
{
    public class UniversidadApiOptions
    {
        public string BaseUrl { get; set; } = "http://siuts.uts.edu.mx/apienlinea/api";

        public string ServiceUsername { get; set; } = string.Empty;

        public string ServicePassword { get; set; } = string.Empty;

        public string ExtracurricularAcademiaClave { get; set; } = "ACD";

        public int ServiceTokenSeconds { get; set; } = 25;

        public List<string> AdminUsernames { get; set; } = new();
    }
}
