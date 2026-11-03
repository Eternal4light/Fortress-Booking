using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UserService.Controllers;
using UserService.Domain;
using UserService.Dtos;
using UserService.Repositories;
using UserService.Services;
using Xunit;

namespace UserService.UnitTests;

public class CurrentUserControllerInvitesTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUserInviteRepository> _inviteRepositoryMock = new();
    private readonly Mock<IUserInviteService> _inviteServiceMock = new();
    private readonly TestTimeProvider _time = new(Now);

    private CurrentUserController CreateSut(Guid? userId)
    {
        var sut = new CurrentUserController(
            _userRepositoryMock.Object,
            _inviteRepositoryMock.Object,
            _inviteServiceMock.Object,
            _time);

        var httpContext = new DefaultHttpContext();
        if (userId is { } id)
        {
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "test"));
        }

        sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return sut;
    }

    private static UserInvite CreateInvite(Guid userId, UserInviteStatus status, int expiresInMinutes) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        PartnerId = Guid.NewGuid(),
        PartnerName = "Beauty Space",
        Activity = "Красота",
        IssuedAt = Now.AddHours(-1),
        ExpiresAt = Now.AddMinutes(expiresInMinutes),
        Status = status
    };

    [Fact]
    public async Task AcceptInvite_ReturnsUpdatedInvite()
    {
        var userId = Guid.NewGuid();
        var invite = CreateInvite(userId, UserInviteStatus.Accepted, expiresInMinutes: 600);
        _inviteServiceMock
            .Setup(x => x.RespondAsync(userId, invite.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InviteRespondResult(InviteRespondOutcome.Updated, invite));

        var result = await CreateSut(userId).AcceptInvite(invite.Id, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<UserInviteResponse>().Which.Status.Should().Be("Accepted");
    }

    [Fact]
    public async Task DeclineInvite_ReturnsUpdatedInvite()
    {
        var userId = Guid.NewGuid();
        var invite = CreateInvite(userId, UserInviteStatus.Declined, expiresInMinutes: 600);
        _inviteServiceMock
            .Setup(x => x.RespondAsync(userId, invite.Id, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InviteRespondResult(InviteRespondOutcome.Updated, invite));

        var result = await CreateSut(userId).DeclineInvite(invite.Id, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<UserInviteResponse>().Which.Status.Should().Be("Declined");
    }

    [Fact]
    public async Task AcceptInvite_WithoutUserClaim_ReturnsUnauthorized()
    {
        var result = await CreateSut(userId: null).AcceptInvite(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        _inviteServiceMock.Verify(
            x => x.RespondAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(InviteRespondOutcome.NotFound, typeof(NotFoundResult))]
    [InlineData(InviteRespondOutcome.Expired, typeof(ConflictObjectResult))]
    [InlineData(InviteRespondOutcome.AlreadyAnswered, typeof(ConflictObjectResult))]
    public async Task AcceptInvite_MapsServiceOutcomeToHttpResult(InviteRespondOutcome outcome, Type expectedResult)
    {
        var userId = Guid.NewGuid();
        _inviteServiceMock
            .Setup(x => x.RespondAsync(userId, It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InviteRespondResult(outcome));

        var result = await CreateSut(userId).AcceptInvite(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType(expectedResult);
    }

    [Fact]
    public async Task GetMyInvites_ShowsOverduePendingInviteAsExpired()
    {
        var userId = Guid.NewGuid();
        var waiting = CreateInvite(userId, UserInviteStatus.Pending, expiresInMinutes: 120);
        var overdue = CreateInvite(userId, UserInviteStatus.Pending, expiresInMinutes: -5);
        var accepted = CreateInvite(userId, UserInviteStatus.Accepted, expiresInMinutes: -600);

        _inviteRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<UserInvite> { waiting, overdue, accepted });

        var result = await CreateSut(userId).GetMyInvites(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var invites = ok.Value.Should().BeAssignableTo<IEnumerable<UserInviteResponse>>().Subject.ToList();

        invites.Single(i => i.Id == waiting.Id).Status.Should().Be("Pending");
        invites.Single(i => i.Id == overdue.Id).Status.Should().Be("Expired");
        invites.Single(i => i.Id == accepted.Id).Status.Should().Be("Accepted");
    }
}
