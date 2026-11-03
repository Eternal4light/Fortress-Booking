using Microsoft.AspNetCore.Mvc;
using UserService.Domain;
using UserService.Dtos;
using UserService.Repositories;
using UserService.Services.Keycloak;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IKeycloakClient _keycloak;

    public AuthController(IUserRepository userRepository, IKeycloakClient keycloak)
    {
        _userRepository = userRepository;
        _keycloak = keycloak;
    }

    /// <summary>
    /// Регистрация клиента, который ищет коворкинг.
    /// Учётная запись и пароль создаются в Keycloak, в MongoDB хранится только профиль
    /// (Id профиля = Id пользователя в Keycloak).
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Email и пароль обязательны");
        }

        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Conflict("Пользователь с таким email уже зарегистрирован");
        }

        Guid keycloakId;
        try
        {
            keycloakId = await _keycloak.CreateUserAsync(request.Email, request.Password, request.FirstName, request.LastName, UserRole.Client);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(title: "Не удалось создать пользователя в Keycloak", detail: ex.Message, statusCode: 502);
        }
        catch (HttpRequestException ex)
        {
            return Problem(title: "Keycloak недоступен", detail: ex.Message, statusCode: 503);
        }

        var user = new User
        {
            Id = keycloakId,
            FirstName = request.FirstName,
            SecondName = request.SecondName,
            LastName = request.LastName,
            Phone = request.Phone,
            Email = request.Email,
            Role = UserRole.Client,
            Activity = request.Activity,
            IsActive = true
        };

        await _userRepository.AddAsync(user);

        string? token;
        try
        {
            token = await _keycloak.LoginAsync(request.Email, request.Password);
        }
        catch (HttpRequestException)
        {
            token = null;
        }

        // Пользователь в любом случае создан — если автологин не удался (Keycloak
        // на мгновение недоступен и т.п.), просто вернём пустой токен, фронтенд
        // предложит войти вручную.
        return Ok(new AuthResponse { Token = token ?? string.Empty, User = user.ToResponse() });
    }

    /// <summary>
    /// Вход в систему (клиент, менеджер, администратор). Пароль проверяет Keycloak,
    /// UserService только подтягивает профиль по email для ответа.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        string? token;
        try
        {
            token = await _keycloak.LoginAsync(request.Email, request.Password);
        }
        catch (HttpRequestException ex)
        {
            return Problem(title: "Keycloak недоступен", detail: ex.Message, statusCode: 503);
        }

        if (token is null)
        {
            return Unauthorized("Неверный email или пароль");
        }

        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return Unauthorized("Неверный email или пароль");
        }

        return Ok(new AuthResponse { Token = token, User = user.ToResponse() });
    }
}
