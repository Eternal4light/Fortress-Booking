using UserService.Domain;

namespace UserService.Repositories;

public interface IUserInviteRepository
{
    Task<IReadOnlyCollection<UserInvite>> GetByUserIdAsync(Guid userId);

    Task<UserInvite?> GetByIdAsync(Guid id);

    /// <summary>
    /// Приглашения коворкинга, которые сейчас занимают места: принятые и ожидающие
    /// ответа с неистёкшим сроком.
    /// </summary>
    Task<IReadOnlyCollection<UserInvite>> GetActiveByPartnerAsync(Guid partnerId, DateTime utcNow);

    Task<int> AddRangeAsync(IEnumerable<UserInvite> invites);

    /// <summary>
    /// Атомарно переводит приглашение из Pending в Accepted/Declined, если оно ещё
    /// ждёт ответа и срок не истёк. false — условие не выполнено (уже отвечено,
    /// срок истёк или приглашения нет).
    /// </summary>
    Task<bool> TryRespondAsync(Guid id, UserInviteStatus newStatus, DateTime utcNow);

    /// <summary>Переводит ждущие ответа приглашения с истёкшим сроком в Expired, возвращает их число.</summary>
    Task<long> ExpireOverdueAsync(DateTime utcNow);
}
