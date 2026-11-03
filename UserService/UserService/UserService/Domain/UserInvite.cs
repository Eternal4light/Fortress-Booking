namespace UserService.Domain;

/// <summary>
/// Копия приглашения, полученного конкретным клиентом (по совпадению деятельности).
/// </summary>
public class UserInvite
{
    /// <summary>Сколько времени у клиента на ответ, после этого приглашение отклоняется автоматически.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid PartnerId { get; set; }

    public string PartnerName { get; set; } = string.Empty;

    public string? Specialization { get; set; }

    public string Activity { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>До какого момента можно ответить на приглашение.</summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow + Lifetime;

    public UserInviteStatus Status { get; set; } = UserInviteStatus.Pending;

    /// <summary>Когда клиент принял или отклонил приглашение (не заполняется при автоматическом отклонении).</summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// Статус с учётом срока: ждущее ответа приглашение с истёкшим сроком считается
    /// отклонённым, даже если фоновая очистка ещё не успела обновить запись в базе.
    /// </summary>
    public UserInviteStatus GetEffectiveStatus(DateTime utcNow) =>
        Status == UserInviteStatus.Pending && ExpiresAt <= utcNow ? UserInviteStatus.Expired : Status;

    /// <summary>Занимает ли приглашение место в коворкинге (ждёт ответа или принято).</summary>
    public bool OccupiesSeat(DateTime utcNow) =>
        GetEffectiveStatus(utcNow) is UserInviteStatus.Pending or UserInviteStatus.Accepted;
}
