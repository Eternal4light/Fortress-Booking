using UserService.Domain;
using UserService.Repositories;

namespace UserService.Services;

public sealed class UserInviteService : IUserInviteService
{
    private readonly IUserInviteRepository _inviteRepository;
    private readonly TimeProvider _timeProvider;

    public UserInviteService(IUserInviteRepository inviteRepository, TimeProvider timeProvider)
    {
        _inviteRepository = inviteRepository;
        _timeProvider = timeProvider;
    }

    public async Task<InviteRespondResult> RespondAsync(Guid userId, Guid inviteId, bool accept, CancellationToken ct)
    {
        var invite = await _inviteRepository.GetByIdAsync(inviteId);

        // чужое приглашение не раскрываем — для клиента оно просто не существует
        if (invite is null || invite.UserId != userId)
        {
            return new InviteRespondResult(InviteRespondOutcome.NotFound);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        switch (invite.GetEffectiveStatus(now))
        {
            case UserInviteStatus.Expired:
                return new InviteRespondResult(InviteRespondOutcome.Expired, invite);
            case UserInviteStatus.Accepted:
            case UserInviteStatus.Declined:
                return new InviteRespondResult(InviteRespondOutcome.AlreadyAnswered, invite);
        }

        var newStatus = accept ? UserInviteStatus.Accepted : UserInviteStatus.Declined;

        // Обновление условное (только если приглашение всё ещё ждёт ответа и не просрочено),
        // поэтому параллельный ответ или фоновая очистка не дадут записать два ответа.
        if (!await _inviteRepository.TryRespondAsync(inviteId, newStatus, now))
        {
            var current = await _inviteRepository.GetByIdAsync(inviteId);
            var currentStatus = current?.GetEffectiveStatus(now);

            return currentStatus == UserInviteStatus.Expired
                ? new InviteRespondResult(InviteRespondOutcome.Expired, current)
                : new InviteRespondResult(InviteRespondOutcome.AlreadyAnswered, current);
        }

        invite.Status = newStatus;
        invite.RespondedAt = now;

        return new InviteRespondResult(InviteRespondOutcome.Updated, invite);
    }

    public async Task<int> GetOccupiedSeatsAsync(Guid partnerId, CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var active = await _inviteRepository.GetActiveByPartnerAsync(partnerId, now);

        return active.Select(i => i.UserId).Distinct().Count();
    }

    public Task<long> ExpireOverdueAsync(CancellationToken ct)
    {
        return _inviteRepository.ExpireOverdueAsync(_timeProvider.GetUtcNow().UtcDateTime);
    }
}
