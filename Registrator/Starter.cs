

using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Registrator.Endpoints;
using Registrator.Infrastructure;
using Registrator.Infrastructure.Jwt;
using Registrator.Services.LoginService;
using Registrator.Services.RabbitMqService;
using Registrator.Validators;
using Serilog;


namespace Registrator
{
    public static class Starter
    {
        /// <summary>
        /// строка подключения для rabbitMQ
        /// </summary>
        public static string RABBITMQ_CONNECTION_STR { get; private set; } = "localhost";

        /// <summary>
        /// Параметры для jwt
        /// </summary>
        public static JwtSettings LoadedJwtSettings { get; private set; } = new();

        /// <summary>
        /// Автоматический накат миграций при запуске
        /// </summary>
        /// <param name="app"></param>
        public static void ApplyDbMigrations(WebApplication app)
        {
            using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetService<ApplicationDbContext>())
                {
                    context?.Database.SetCommandTimeout(60);
                    context?.Database.Migrate();
                }
            }
        }

        /// <summary>
        /// Подключение к postgres и добавление контекста в DI
        /// </summary>
        /// <param name="builder"></param>
        /// <exception cref="Exception">нет строки подключения в конфигах</exception>
        private static void DefineDb(WebApplicationBuilder builder)
        {
            //Подключение БД
            var dbConnectionString = builder.Configuration.GetConnectionString("Postgres")
            ?? throw new Exception("no db connection str");


            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(dbConnectionString));

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Added postgres host: {builder.Configuration.GetConnectionString("Postgres")}");
            Console.ResetColor();
        }



        /// <summary>
        /// Добавление валидаторов
        /// </summary>
        /// <param name="builder"></param>
        private static void DefineValidators(WebApplicationBuilder builder)
        {
            builder.Services.AddValidatorsFromAssemblyContaining<EmailValidator>();

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Validators added");
            Console.ResetColor();
        }

        /// <summary>
        /// Добавление прочих сервисов в DI-контейнер
        /// </summary>
        /// <param name="builder"></param>
        private static void DefineCustomServices(WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<ILoginService, LoginService>();

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Custom Services added");
            Console.ResetColor();
        }

        /// <summary>
        /// Логгирование через Serilog: логгирует все в консоль
        /// </summary>
        /// <param name="builder"></param>
        private static void DefineLogger(WebApplicationBuilder builder)
        {
            builder.Logging.ClearProviders();
            var logger = new LoggerConfiguration()
            .WriteTo.Console()
            /* .WriteTo.File($"{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss_f")}.txt") */
            //.Filter.ByIncludingOnly(x => x.Level == Serilog.Events.LogEventLevel.Error)
            .CreateLogger();

            builder.Logging.AddSerilog(logger);
        }

        /// <summary>
        /// Подключение к RabbitMQ и добавление его сервиса в DI
        /// </summary>
        /// <param name="builder"></param>
        /// <exception cref="Exception">Нет строки подключения в конфигах</exception>
        private static void DefineRabbitMq(WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IRabbitMqService, RabbitMqService>();
            RABBITMQ_CONNECTION_STR = builder.Configuration.GetConnectionString("RabbitMQ")
            ?? throw new Exception("no rabbitMq str");
        }

        /// <summary>
        /// Подключение кеша на редисе
        /// </summary>
        /// <param name="builder"></param>
        /// <exception cref="Exception"></exception>
        private static void DefineCache(WebApplicationBuilder builder)
        {
            builder.Services.AddStackExchangeRedisCache(x =>
        {
            x.Configuration = builder.Configuration.GetConnectionString("Redis")
            ?? throw new Exception("no redis str");
            x.InstanceName = "Registrator_";
        });

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Added redis host: {builder.Configuration.GetConnectionString("Redis")}");
            Console.ResetColor();
        }

        /// <summary>
        /// аутентификация по JWT-токену
        /// </summary>
        /// <param name="builder"></param>
        /// <exception cref="Exception">Нет строк в конфигах</exception>
        private static void DefineAuthentication(WebApplicationBuilder builder)
        {
            LoadedJwtSettings.Issuer = builder.Configuration["JwtSettings:Issuer"]
            ?? throw new Exception("no JWT issuer");

            LoadedJwtSettings.Audience = builder.Configuration["JwtSettings:Audience"]
            ?? throw new Exception("no JWT audience");

            LoadedJwtSettings.Key = builder.Configuration["JwtSettings:Key"]
            ?? throw new Exception("no JWT key");

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
         .AddJwtBearer(x =>
         {
             x.TokenValidationParameters = new()
             {
                 ValidIssuer = LoadedJwtSettings.Issuer,
                 ValidAudience = LoadedJwtSettings.Audience,
                 IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(LoadedJwtSettings.Key)),
                 ValidateIssuer = true,
                 ValidateAudience = true,
                 ValidateIssuerSigningKey = true,
                 ValidateLifetime = true
             };
         });
        }

        /// <summary>
        /// Простая защита от дудоса - фиксированное количество запросов в промежуток
        /// </summary>
        /// <param name="builder"></param>
        /// <exception cref="Exception">Не найдены строки в конфигах</exception>
        private static void DefineRateLimiting(WebApplicationBuilder builder)
        {
            var limit = Convert.ToInt32(builder.Configuration["RateLimit:Limit"]
            ?? throw new Exception("no rateLimit str"));
            var windowSeconds = Convert.ToInt32(builder.Configuration["RateLimit:Seconds"]
            ?? throw new Exception("no rateSeconds str"));

            builder.Services.AddRateLimiter(cfg =>
            {
                cfg.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                cfg.AddFixedWindowLimiter("fixed", options =>
                {
                    options.PermitLimit = limit;
                    options.Window = TimeSpan.FromSeconds(windowSeconds);
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = limit;
                });
            });
        }


        /// <summary>
        /// Определение эндпоинтов
        /// </summary>
        /// <param name="app"></param>
        public static void DefineEndpoints(WebApplication app)
        {
            new LoginEndpoints().Define(app);
            new DummyEndpoints().Define(app);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Endpoints registered");
            Console.ResetColor();
        }


        public static void RegisterServices(WebApplicationBuilder builder)
        {

            DefineValidators(builder);

            DefineDb(builder);

            DefineCache(builder);

            DefineRabbitMq(builder);

            DefineCustomServices(builder);

            DefineLogger(builder);

            DefineAuthentication(builder);

            DefineRateLimiting(builder);

            builder.Services.AddAuthorization();

            builder.Services.AddAutoMapper(typeof(ApplicationProfile));

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen();

            builder.Services.AddCors();
        }

        public static void Configure(WebApplication app)
        {
            ApplyDbMigrations(app);

            app.UseRateLimiter();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            app.UseAuthentication();
            app.UseAuthorization();
        }


    }
}