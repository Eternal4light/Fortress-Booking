using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UserService;
using UserService.Data;
using UserService.Messaging;
using UserService.Services;
using UserService.Services.Keycloak;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var keycloakSection = builder.Configuration.GetSection("Keycloak");
builder.Services.Configure<KeycloakOptions>(keycloakSection);

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
            // Роли в токене Keycloak лежат вложенно в realm_access.roles, а не плоским
            // claim'ом "role", который ожидает [Authorize(Roles=...)] в ASP.NET Core.
            // Разбираем сырой JWT вручную по строке из заголовка Authorization, а не
            // через тип context.SecurityToken — так поведение не зависит от того, какой
            // обработчик токенов используется под капотом текущей версии пакетов
            // Microsoft.IdentityModel, что нельзя было проверить компиляцией в этой среде.
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
                    // Не удалось разобрать полезную нагрузку токена — роли не добавятся,
                    // авторизация по ролям для этого запроса не пройдёт.
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection("MongoDb"));
builder.Services.AddUserServiceDependencies();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddHostedService<InviteBroadcastConsumer>();
builder.Services.AddHostedService<InviteExpirationWorker>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// MongoDB при старте контейнера может быть ещё не готова принимать соединения —
// пробуем засеять демо-данные несколько раз с паузой, а не падаем при первом обращении.
using (var scope = app.Services.CreateScope())
{
    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await MongoDbSeeder.SeedAsync(scope.ServiceProvider);
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            Console.WriteLine($"Не удалось подключиться к MongoDB (попытка {attempt}/{maxAttempts}): {ex.Message}. Повтор через 3 секунды...");
            await Task.Delay(3000);
        }
    }
}

await app.RunAsync();

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
