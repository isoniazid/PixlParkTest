using Microsoft.AspNetCore.Mvc;
using Registrator.Services.LoginService;

namespace Registrator.Endpoints
{
    public class LoginEndpoints
    {
        public void Define(WebApplication app)
        {
            app.MapPost("api/v1/apply", ApplyEmail)
            .WithTags("Login")
            .Produces(200).Produces(422).Produces(409)
            .RequireRateLimiting("fixed")
            .WithDescription("Запрос на получение кода");

            app.MapPost("api/v1/register", Register)
            .WithTags("Login")
            .Produces(200).Produces(404).Produces(409).Produces(401)
            .RequireRateLimiting("fixed")
            .WithDescription("Получение токена по коду");
        }

        public async Task<IResult> Register(ILoginService loginService, [FromQuery] string email, [FromQuery] string code)
        {
            var result = await loginService.RegisterAsync(code, email);
            return result.Match(
                token => Results.Ok(token),
                apiError => Results.Problem(detail: apiError.Message, statusCode: apiError.StatusCode)
            );
        }

        public async Task<IResult> ApplyEmail(ILoginService loginService, [FromQuery] string email)
        {
            var result = await loginService.ApplyEmailAsync(email);
            return result.Match(
            email => Results.Ok(),
            apiError => Results.Problem(detail: apiError.Message, statusCode: apiError.StatusCode),
            validatorError => Results.UnprocessableEntity(validatorError.ValidationErrors));
        }

    }
}