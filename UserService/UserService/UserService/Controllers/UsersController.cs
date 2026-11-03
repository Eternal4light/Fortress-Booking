using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Domain;
using UserService.Dtos;
using UserService.Repositories;
using UserService.Services;
using UserService.Services.Keycloak;

namespace UserService.Controllers;

/// <summary>
/// Закрытое CRUD API для работы с клиентской базой (доступно менеджерам и администратору).
/// Используется в т.ч. для получения справочника деятельностей клиентов.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Users")]
[Authorize(Roles = "Admin,Manager")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IKeycloakClient _keycloak;

    public UsersController(IUserRepository userRepository, IKeycloakClient keycloak)
    {
        _userRepository = userRepository;
        _keycloak = keycloak;
    }

    /// <summary>
    /// Получить клиента по идентификатору
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(user.ToResponse());
    }

    /// <summary>
    /// Получить всех клиентов, опционально отфильтрованных по деятельности
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAll([FromQuery] string? activity, CancellationToken ct)
    {
        var users = await _userRepository.GetAllAsync(UserRole.Client, activity);
        return Ok(users.Select(u => u.ToResponse()).ToList());
    }

    /// <summary>
    /// Создать клиента
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Conflict("Пользователь с таким email уже существует");
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

        return Created($"api/users/{user.Id}", user.ToResponse());
    }

    /// <summary>
    /// Обновить данные клиента
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>> Update([FromRoute] Guid id, [FromBody] UpdateCurrentUserRequest request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.FirstName = request.FirstName;
        user.SecondName = request.SecondName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.Activity = request.Activity;

        await _userRepository.UpdateAsync(user);

        return Ok(user.ToResponse());
    }

    /// <summary>
    /// Удалить клиента
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        await _keycloak.DeleteUserAsync(id);
        await _userRepository.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Внутренний эндпоинт: разослать приглашение всем клиентам с указанной деятельностью.
    /// Основной путь доставки — очередь RabbitMQ (см. UserService.Messaging.InviteBroadcastConsumer),
    /// PartnerService теперь публикует туда сообщение, а не вызывает этот HTTP-эндпоинт напрямую.
    /// Эндпоинт оставлен как ручной/тестовый вызов той же логики рассылки, защищён отдельным
    /// служебным ключом, а не JWT — это service-to-service вызов.
    /// </summary>
    [HttpPost("invites/broadcast")]
    [AllowAnonymous]
    public async Task<ActionResult<BroadcastInviteResponse>> BroadcastInvite(
        [FromBody] BroadcastInviteRequest request,
        [FromServices] IInviteBroadcastService inviteBroadcastService,
        [FromServices] IConfiguration configuration,
        CancellationToken ct)
    {
        if (!HasValidInternalKey(configuration))
        {
            return Unauthorized();
        }

        var count = await inviteBroadcastService.BroadcastAsync(request, ct);

        return Ok(new BroadcastInviteResponse { RecipientsCount = count });
    }

    /// <summary>
    /// Внутренний эндпоинт для PartnerService: сколько мест коворкинга занято приглашениями
    /// (в ожидании ответа и принятыми). Нужен для ранней проверки вместимости при выдаче
    /// приглашения. Защищён тем же служебным ключом, что и рассылка.
    /// </summary>
    [HttpGet("invites/occupancy")]
    [AllowAnonymous]
    public async Task<ActionResult<OccupancyResponse>> GetOccupancy(
        [FromQuery] Guid partnerId,
        [FromServices] IUserInviteService inviteService,
        [FromServices] IConfiguration configuration,
        CancellationToken ct)
    {
        if (!HasValidInternalKey(configuration))
        {
            return Unauthorized();
        }

        var occupied = await inviteService.GetOccupiedSeatsAsync(partnerId, ct);

        return Ok(new OccupancyResponse { OccupiedSeats = occupied });
    }

    private bool HasValidInternalKey(IConfiguration configuration)
    {
        var expectedKey = configuration["InternalApi:Key"];

        return !string.IsNullOrEmpty(expectedKey) &&
            Request.Headers.TryGetValue("X-Internal-Api-Key", out var providedKey) &&
            providedKey == expectedKey;
    }
}
