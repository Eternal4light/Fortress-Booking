using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Domain;
using UserService.Dtos;
using UserService.Repositories;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("CurrentUser")]
[Authorize]
public sealed class CurrentUserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IUserInviteRepository _inviteRepository;
    private readonly IUserInviteService _inviteService;
    private readonly TimeProvider _timeProvider;

    public CurrentUserController(
        IUserRepository userRepository,
        IUserInviteRepository inviteRepository,
        IUserInviteService inviteService,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _inviteRepository = inviteRepository;
        _inviteService = inviteService;
        _timeProvider = timeProvider;
    }

    private Guid? CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    /// <summary>
    /// Получить текущего пользователя
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<UserResponse>> GetCurrentUser(CancellationToken ct)
    {
        if (CurrentUserId is not { } id)
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(user.ToResponse());
    }

    /// <summary>
    /// Обновить свои данные
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<UserResponse>> UpdateCurrentUser([FromBody] UpdateCurrentUserRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } id)
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.FirstName = request.FirstName;
        user.SecondName = request.SecondName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;

        if (user.Role == UserRole.Client)
        {
            user.Activity = request.Activity;
        }

        await _userRepository.UpdateAsync(user);

        return Ok(user.ToResponse());
    }

    /// <summary>
    /// Получить свои приглашения в коворкинги
    /// </summary>
    [HttpGet("invites")]
    public async Task<ActionResult<List<UserInviteResponse>>> GetMyInvites(CancellationToken ct)
    {
        if (CurrentUserId is not { } id)
        {
            return Unauthorized();
        }

        var invites = await _inviteRepository.GetByUserIdAsync(id);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var response = invites.Select(i => i.ToResponse(now)).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Принять приглашение. Ответить можно только на своё приглашение, которое ещё
    /// ждёт ответа и не просрочено (на ответ даётся 24 часа).
    /// </summary>
    [HttpPost("invites/{id:guid}/accept")]
    public Task<ActionResult<UserInviteResponse>> AcceptInvite([FromRoute] Guid id, CancellationToken ct)
        => RespondToInviteAsync(id, accept: true, ct);

    /// <summary>
    /// Отклонить приглашение — место в коворкинге освобождается.
    /// </summary>
    [HttpPost("invites/{id:guid}/decline")]
    public Task<ActionResult<UserInviteResponse>> DeclineInvite([FromRoute] Guid id, CancellationToken ct)
        => RespondToInviteAsync(id, accept: false, ct);

    private async Task<ActionResult<UserInviteResponse>> RespondToInviteAsync(Guid inviteId, bool accept, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId)
        {
            return Unauthorized();
        }

        var result = await _inviteService.RespondAsync(userId, inviteId, accept, ct);

        switch (result.Outcome)
        {
            case InviteRespondOutcome.Updated:
                return Ok(result.Invite!.ToResponse(_timeProvider.GetUtcNow().UtcDateTime));
            case InviteRespondOutcome.NotFound:
                return NotFound();
            case InviteRespondOutcome.Expired:
                return Conflict("Срок ответа на приглашение (24 часа) истёк — оно отклонено автоматически");
            default:
                return Conflict("Вы уже ответили на это приглашение");
        }
    }
}
