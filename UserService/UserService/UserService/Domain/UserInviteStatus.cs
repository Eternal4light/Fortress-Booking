namespace UserService.Domain;

/// <summary>
/// Состояние приглашения клиента. Занимают место в коворкинге только приглашения
/// в ожидании ответа (Pending) и принятые (Accepted).
/// </summary>
public enum UserInviteStatus
{
    /// <summary>Ждёт ответа клиента (на ответ даётся 24 часа).</summary>
    Pending = 0,

    /// <summary>Клиент принял приглашение.</summary>
    Accepted = 1,

    /// <summary>Клиент отклонил приглашение.</summary>
    Declined = 2,

    /// <summary>Клиент не ответил за 24 часа — приглашение отклонено автоматически.</summary>
    Expired = 3
}
