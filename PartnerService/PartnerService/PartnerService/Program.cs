using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi.Generated;
using Microsoft.IdentityModel.Tokens;
using PartnerService;
using PartnerService.DataAccess;
using PartnerService.DataAccess.Data;
using PartnerService.WebHost.Messaging;
using PartnerService.WebHost.Services;
var builder = WebApplication.CreateBuilder();

builder.Services.AddEfDataAccess(builder.Configuration);

// Redis — кэш списков /partners и /activities (часто читаются, редко меняются).
// Используется через IDistributedCache напрямую в контроллерах (cache-aside),
// без отдельного пакета сверх Microsoft.Extensions.Caching.StackExchangeRedis.
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Строка подключения ConnectionStrings:Redis не настроена");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "fortress:";
});

builder.Services.AddProblemDetails();
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});
builder.Services.AddControllers();

builder.Services.AddOpenApi(builder.Environment);

var keycloakSection = builder.Configuration.GetSection("Keycloak");
var keycloakBaseUrl = keycloakSection["BaseUrl"] ?? throw new InvalidOperationException("Keycloak:BaseUrl не настроен в конфигурации");
var keycloakRealm = keycloakSection["Realm"] ?? throw new InvalidOperationException("Keycloak:Realm не настроен в конфигурации");
var keycloakIssuer = $"{keycloakBaseUrl}/realms/{keycloakRealm}";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakIssuer;
        options.RequireHttpsMetadata = false; // локальный Keycloak в docker-compose работает по http

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = keycloakIssuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            // См. аналогичный комментарий в UserService/Program.cs — роли Keycloak лежат
            // вложенно в realm_access.roles, разбираем сырой токен вручную, не полагаясь
            // на конкретный тип SecurityToken из внутренностей библиотеки.
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is not ClaimsIdentity identity)
                {
                    return Task.CompletedTask;
                }

                var rawToken = context.HttpContext.Request.Headers["Authorization"].ToString();
                if (rawToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    rawToken = rawToken["Bearer ".Length..];
                }

                var parts = rawToken.Split('.');
                if (parts.Length < 2)
                {
                    return Task.CompletedTask;
                }

                try
                {
                    var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
                    using var document = JsonDocument.Parse(payloadJson);

                    if (document.RootElement.TryGetProperty("realm_access", out var realmAccess) &&
                        realmAccess.TryGetProperty("roles", out var roles) &&
                        roles.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in roles.EnumerateArray())
                        {
                            var roleName = role.GetString();
                            if (!string.IsNullOrEmpty(roleName))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                            }
                        }
                    }
                }
                catch (FormatException)
                {
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IInvitePublisher, RabbitMqInvitePublisher>();

// Ранняя проверка вместимости коворкинга: PartnerService спрашивает у UserService,
// сколько мест уже занято приглашениями (приглашения клиентов хранятся в UserService).
var userServiceBaseUrl = builder.Configuration["UserService:BaseUrl"]
    ?? throw new InvalidOperationException("UserService:BaseUrl не настроен в конфигурации");
builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
{
    client.BaseAddress = new Uri(userServiceBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

app.UseExceptionHandler();

app.MapOpenApi();
app.MapSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// PostgreSQL при старте контейнера может быть ещё не готов принимать соединения —
// пробуем несколько раз с паузой, вместо того чтобы упасть при первом же обращении.
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            initializer.InitializeDb();
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            Console.WriteLine($"Не удалось подключиться к PostgreSQL (попытка {attempt}/{maxAttempts}): {ex.Message}. Повтор через 3 секунды...");
            Thread.Sleep(3000);
        }
    }
}

app.Run();

static byte[] Base64UrlDecode(string input)
{
    var value = input.Replace('-', '+').Replace('_', '/');
    switch (value.Length % 4)
    {
        case 2:
            value += "==";
            break;
        case 3:
            value += "=";
            break;
    }

    return Convert.FromBase64String(value);
}
