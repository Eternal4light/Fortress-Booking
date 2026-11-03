using FluentAssertions;
using UserService.Domain;
using Xunit;

namespace UserService.UnitTests;

public class UserInviteTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    private static UserInvite CreateInvite(UserInviteStatus status, DateTime expiresAt) => new()
    {
        Id = Guid.NewGuid(),
        Status = status,
        IssuedAt = expiresAt - UserInvite.Lifetime,
        ExpiresAt = expiresAt
    };

    [Fact]
    public void Lifetime_IsTwentyFourHours()
    {
        UserInvite.Lifetime.Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public void GetEffectiveStatus_Pending_BeforeDeadline_StaysPending()
    {
        var invite = CreateInvite(UserInviteStatus.Pending, Now.AddMinutes(1));

        invite.GetEffectiveStatus(Now).Should().Be(UserInviteStatus.Pending);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-600)]
    public void GetEffectiveStatus_Pending_AtOrAfterDeadline_IsExpired(int minutesFromNow)
    {
        var invite = CreateInvite(UserInviteStatus.Pending, Now.AddMinutes(minutesFromNow));

        invite.GetEffectiveStatus(Now).Should().Be(UserInviteStatus.Expired);
    }

    [Theory]
    [InlineData(UserInviteStatus.Accepted)]
    [InlineData(UserInviteStatus.Declined)]
    [InlineData(UserInviteStatus.Expired)]
    public void GetEffectiveStatus_AnsweredInvite_KeepsItsStatusEvenAfterDeadline(UserInviteStatus status)
    {
        var invite = CreateInvite(status, Now.AddHours(-5));

        invite.GetEffectiveStatus(Now).Should().Be(status);
    }

    [Theory]
    [InlineData(UserInviteStatus.Pending, 60, true)]
    [InlineData(UserInviteStatus.Pending, -60, false)]
    [InlineData(UserInviteStatus.Accepted, -60, true)]
    [InlineData(UserInviteStatus.Declined, 60, false)]
    [InlineData(UserInviteStatus.Expired, 60, false)]
    public void OccupiesSeat_OnlyPendingNotExpiredAndAccepted(UserInviteStatus status, int expiresInMinutes, bool expected)
    {
        var invite = CreateInvite(status, Now.AddMinutes(expiresInMinutes));

        invite.OccupiesSeat(Now).Should().Be(expected);
    }
}
