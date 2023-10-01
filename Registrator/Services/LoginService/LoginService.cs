using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MagazinchikAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using OneOf;
using RabbitMQ.Client;
using Registrator.Infrastructure;
using Registrator.Infrastructure.ServiceResult;
using Registrator.Model;
using Registrator.Services.RabbitMqService;
using Registrator.Validators;

namespace Registrator.Services.LoginService
{
    public class LoginService : ILoginService
    {
        private readonly IRabbitMqService _rmqService;
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<LoginService> _logger;
        private readonly EmailValidator _emailValidator;

        //Время жизни кода доступа
        private double timeoutMinutes_ = 3;

        public LoginService(IRabbitMqService rmqService, ApplicationDbContext context, IDistributedCache cache,
        EmailValidator emailValidator, ILogger<LoginService> logger)
        {
            _context = context;
            _rmqService = rmqService;
            _cache = cache;
            _emailValidator = emailValidator;
            _logger = logger;
        }


        /// <summary>
        /// Запрос на регистрацию и генерация кода доступа
        /// </summary>
        /// <param name="email">Электронная почта (можно в не нормализованном виде)</param>
        /// <returns>При успешном завершении - почта, при ошибке - APIError либо ValidatorError</returns>
        public async Task<OneOf<string, APIError, ValidatorError>> ApplyEmailAsync(string email)
        {
            //Проверка на валидность почты
            var validation = _emailValidator.Validate(email);
            if (!validation.IsValid)
            {
                _logger.LogError("Validation Error: {Errors}", validation.Errors);
                return new ValidatorError(validation);
            }


            //Приводим почту к нормальному виду
            email = email.ToLower().Trim();

            //Проверка, не был ли код уже отправлен
            var existingCode = await _cache.GetRecordAsync<SecretCode>(email);
            if (existingCode is not null)
            {
                var span = existingCode.TimeOut.Subtract(DateTime.UtcNow);//.Subtract(existingCode.TimeOut);
                return new APIError(429, $"Новый код можно получить через {span.Minutes * 60 + span.Seconds} сек");
            }



            //Проверка, зарегистрирован ли пользователь
            var existingUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
            if (existingUser is not null) return new APIError(409, "Уже зарегистрирован");

            //Генерация кода
            var random = new Random();
            var secretCodeStr = new string(Enumerable.Repeat("0123456789", 4)
            .Select(s => s[random.Next(s.Length)]).ToArray());

            var codeToSend = new SecretCode
            {
                CodeString = secretCodeStr,
                Email = email,
                TimeOut = DateTime.UtcNow.AddMinutes(timeoutMinutes_)
            };

            //Сохранение кода в кеш редиса
            await _cache.SetRecordAsync(email, codeToSend, TimeSpan.FromMinutes(timeoutMinutes_));

            //Отправка кода в очередь RabbitMQ для последующей отправки на почту
            _rmqService.SendMessage(codeToSend);

            return email;
        }

        /// <summary> 
        /// Метод выполняет регистрацию пользователя. 
        /// </summary> 
        /// <param name="numbers">Код, который нужно проверить.</param> 
        /// <param name="email">Электронная почта пользователя.</param> 
        /// <returns>Результат регистрации: либо строка с сгенерированным токеном, либо объект APIError в случае ошибки.</returns>
        public async Task<OneOf<string, APIError>> RegisterAsync(string numbers, string email)
        {
            //Приводим почту к нормальному виду
            email = email.ToLower().Trim();

            //Взятие кода из кеша 
            var codeToCompare = await _cache.GetRecordAsync<SecretCode>(email);

            //Если кода в кеше нет...
            if (codeToCompare is null) return new APIError(404, "Время истекло либо не было такого пользователя");


            //Если код неверный...
            if (codeToCompare.CodeString != numbers) return new APIError(401, "Неверный код");

            //Если код был отправлен второй раз...
            var existingUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
            if (existingUser is not null) return new APIError(409, "Уже зарегистрирован");

            //Если все ок, то сохранение пользователя и отправка сгенеренного токена
            await _context.Users.AddAsync(new() { Email = email });
            await _context.SaveChangesAsync();

            return GenerateToken(email);
        }

        /// <summary> 
        /// Метод генерирует токен для указанной электронной почты. 
        /// </summary> 
        /// <param name="email">Электронная почта пользователя.</param> 
        /// <returns>Сгенерированный токен в виде строки.</returns>
        private string GenerateToken(string email)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(Starter.LoadedJwtSettings.Key);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Email, email)
            };

            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
             SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(timeoutMinutes_),
                Issuer = Starter.LoadedJwtSettings.Issuer,
                Audience = Starter.LoadedJwtSettings.Audience,
                SigningCredentials = signingCredentials
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}