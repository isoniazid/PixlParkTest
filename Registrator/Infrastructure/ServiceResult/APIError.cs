namespace Registrator.Infrastructure.ServiceResult
{
    public class APIError
    {
        public APIError(int statusCode, string message)
        {
            StatusCode = statusCode;
            Message = message;
        }

        public int StatusCode { get; set; }
        public string Message { get; set; }
    }
}