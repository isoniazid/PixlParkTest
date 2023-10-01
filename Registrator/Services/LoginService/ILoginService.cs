using OneOf;
using Registrator.Infrastructure.ServiceResult;

namespace Registrator.Services.LoginService
{
    public interface ILoginService
    {
        public  Task<OneOf<string, APIError, ValidatorError>> ApplyEmailAsync(string email);

        public  Task<OneOf<string, APIError>> RegisterAsync(string numbers, string email);
    }
}