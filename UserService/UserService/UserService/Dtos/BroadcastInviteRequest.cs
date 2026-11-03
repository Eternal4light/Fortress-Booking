namespace UserService.Dtos;

/// <summary>
/// Внутренний запрос от PartnerService: разослать приглашение клиентам с указанной деятельностью.
/// </summary>
public class BroadcastInviteRequest
{
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Вместимость коворкинга. Приглашений создаётся не больше, чем свободных мест.
    /// null — без ограничения (ручной вызов или сообщение без вместимости).
    /// </summary>
    public int? Capacity { get; set; }
}
