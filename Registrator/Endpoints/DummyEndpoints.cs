using Microsoft.AspNetCore.Mvc;
using Registrator.Services.LoginService;

namespace Registrator.Endpoints
{
    public class DummyEndpoints
    {
        public void Define(WebApplication app)
        {
            app.MapGet("api/v1/dummy", () => Results.Ok("Hello, pixlpark reviewer 👾👾👾"))
            .WithTags("Dummy")
            .Produces(200).Produces(401)
            .RequireRateLimiting("fixed")
            .RequireAuthorization();
        }

    }
}