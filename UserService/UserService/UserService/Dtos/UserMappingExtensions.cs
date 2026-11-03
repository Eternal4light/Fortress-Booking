using UserService.Domain;

namespace UserService.Dtos;

public static class UserMappingExtensions
{
    public static UserResponse ToResponse(this User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        SecondName = user.SecondName,
        LastName = user.LastName,
        Phone = user.Phone,
        Email = user.Email,
        Role = user.Role.ToString(),
        Activity = user.Activity,
        IsActive = user.IsActive
    };

    public static UserInviteResponse ToResponse(this UserInvite invite, DateTime utcNow) => new()
    {
        Id = invite.Id,
        PartnerId = invite.PartnerId,
        PartnerName = invite.PartnerName,
        Specialization = invite.Specialization,
        Activity = invite.Activity,
        Description = invite.Description,
        IssuedAt = invite.IssuedAt,
        Status = invite.GetEffectiveStatus(utcNow).ToString(),
        ExpiresAt = invite.ExpiresAt,
        RespondedAt = invite.RespondedAt
    };
}
