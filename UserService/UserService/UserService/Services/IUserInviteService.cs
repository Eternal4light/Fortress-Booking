using UserService.Domain;

namespace UserService.Services;

public enum InviteRespondOutcome
{
    /// <summary>Ответ записан.</summary>
    Updated,

    /// <summary>Приглашения нет или оно адресовано другому клиенту.</summary>
    NotFound,

    /// <summary>Клиент уже принял или отклонил это приглашение.</summary>
    AlreadyAnswered,

    /// <summary>Срок ответа (24 часа) истёк — приглашение отклонено автоматически.</summary>
    Expired
}

public sealed record InviteRespondResult(InviteRespondOutcome Outcome, UserInvite? Invite = null);

/// <summary>
/// Ответы клиентов на приглашения, автоматическое отклонение просроченных приглашений
/// и подсчёт занятых мест коворкинга.
/// </summary>
public interface IUserInviteService
{
    /// <summary>Клиент принимает (accept = true) или отклоняет своё приглашение.</summary>
    Task<InviteRespondResult> RespondAsync(Guid userId, Guid inviteId, bool accept, CancellationToken ct);

    /// <summary>Сколько клиентов сейчас занимают места коворкинга (приглашение в ожидании ответа или принято).</summary>
    Task<int> GetOccupiedSeatsAsync(Guid partnerId, CancellationToken ct);

    /// <summary>Отклоняет автоматически все приглашения, на которые не ответили за 24 часа; возвращает их число.</summary>
    Task<long> ExpireOverdueAsync(CancellationToken ct);
}
