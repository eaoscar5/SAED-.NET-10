namespace saed.api.Services
{
    public class ApiValidationException : Exception
    {
        public ApiValidationException(string message) : base(message)
        {
        }
    }

    public class ApiConflictException : Exception
    {
        public ApiConflictException(string message) : base(message)
        {
        }
    }
}
