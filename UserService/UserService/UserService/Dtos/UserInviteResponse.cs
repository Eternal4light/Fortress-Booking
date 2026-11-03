namespace UserService.Dtos;

public class UserInviteResponse
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime IssuedAt { get; set; }

    /// <summary>Pending, Accepted, Declined или Expired (срок истёк — отклонено автоматически).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>До какого момента можно ответить на приглашение.</summary>
    public DateTime ExpiresAt { get; set; }

    public DateTime? RespondedAt { get; set; }
}
