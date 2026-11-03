using FluentAssertions;
using Moq;
using UserService.Domain;
using UserService.Repositories;
using UserService.Services;
using Xunit;

namespace UserService.UnitTests;

public class UserInviteServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserInviteRepository> _repositoryMock = new();
    private readonly TestTimeProvider _time = new(Now);

    private UserInviteService CreateSut() => new(_repositoryMock.Object, _time);

    private static UserInvite CreateInvite(
        Guid userId,
        UserInviteStatus status = UserInviteStatus.Pending,
        int expiresInMinutes = 23 * 60) => new()
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

    [Theory]
    [InlineData(true, UserInviteStatus.Accepted)]
    [InlineData(false, UserInviteStatus.Declined)]
    public async Task RespondAsync_PendingInvite_RecordsAnswer(bool accept, UserInviteStatus expectedStatus)
    {
        var userId = Guid.NewGuid();
        var invite = CreateInvite(userId);

        _repositoryMock.Setup(x => x.GetByIdAsync(invite.Id)).ReturnsAsync(invite);
        _repositoryMock.Setup(x => x.TryRespondAsync(invite.Id, expectedStatus, Now)).ReturnsAsync(true);

        var result = await CreateSut().RespondAsync(userId, invite.Id, accept, CancellationToken.None);

        result.Outcome.Should().Be(InviteRespondOutcome.Updated);
        result.Invite!.Status.Should().Be(expectedStatus);
        result.Invite.RespondedAt.Should().Be(Now);
        _repositoryMock.Verify(x => x.TryRespondAsync(invite.Id, expectedStatus, Now), Times.Once);
    }

    [Fact]
    public async Task RespondAsync_UnknownInvite_ReturnsNotFound()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((UserInvite?)null);

        var result = await CreateSut().RespondAsync(Guid.NewGuid(), Guid.NewGuid(), true, CancellationToken.None);

        result.Outcome.Should().Be(InviteRespondOutcome.NotFound);
    }

    [Fact]
    public async Task RespondAsync_SomeoneElsesInvite_ReturnsNotFoundAndDoesNotChangeIt()
    {
        var invite = CreateInvite(Guid.NewGuid());
        _repositoryMock.Setup(x => x.GetByIdAsync(invite.Id)).ReturnsAsync(invite);

        var result = await CreateSut().RespondAsync(Guid.NewGuid(), invite.Id, true, CancellationToken.None);

        result.Outcome.Should().Be(InviteRespondOutcome.NotFound);
        _repositoryMock.Verify(
            x => x.TryRespondAsync(It.IsAny<Guid>(), It.IsAny<UserInviteStatus>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task RespondAsync_PendingInviteAfterDeadline_ReturnsExpiredAndDoesNotAccept()
    {
        var userId = Guid.NewGuid();
        // на момент ответа прошли сутки, а фоновая очистка ещё не отметила приглашение в базе
        var invite = CreateInvite(userId, UserInviteStatus.Pending, expiresInMinutes: -1);
        _repositoryMock.Setup(x => x.GetByIdAsync(invite.Id)).ReturnsAsync(invite);

        var result = await CreateSut().RespondAsync(userId, invite.Id, true, CancellationToken.None);

        result.Outcome.Should().Be(InviteRespondOutcome.Expired);
        _repositoryMock.Verify(
            x => x.TryRespondAsync(It.IsAny<Guid>(), It.IsAny<UserInviteStatus>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task RespondAsync_AlreadyExpiredInvite_ReturnsExpired()
    {
        var userId = Guid.NewGuid();
        var invite = CreateInvite(userId, UserInviteStatus.Expired, expiresInMinutes: -600);
        _repositoryMock.Setup(x => x.GetByIdAsync(invite.Id)).ReturnsAsync(invite);

        var result = await CreateSut().RespondAsync(userId, invite.Id, false, CancellationToken.None);

        result.Outcome.Should().Be(InviteRespondOutcome.Expired);
    }

    [Theory]
    [InlineData(UserInviteStatus.Accepted)]
    [InlineData(UserInviteStatus.Declined)]
    public async Task RespondAsync_AlreadyAnsweredInvite_ReturnsAlreadyAnswered(UserInviteStatus status)
    {
        var userId = Guid.NewGuid();
        var invite = CreateInvite(userId, status);
        _repositoryMock.Setup(x => x.GetByIdAsync(invite.Id)).ReturnsAsync(invite);

        var result = await CreateSut().RespondAsync(userId, invite.Id, true, CancellationToken.None);

        result.Outcome.Should().Be(InviteRespondOutcome.AlreadyAnswered);
        _repositoryMock.Verify(
            x => x.TryRespondAsync(It.IsAny<Guid>(), It.IsAny<UserInviteStatus>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task RespondAsync_ConcurrentAnswerWon_ReturnsAlreadyAnswered()
    {
        var userId = Guid.NewGuid();
        var pending = CreateInvite(userId);
        var answered = CreateInvite(userId, UserInviteStatus.Declined);
        answered.Id = pending.Id;

        // первое чтение видит приглашение ждущим, но к записи его уже успели отклонить
        _repositoryMock.SetupSequence(x => x.GetByIdAsync(pending.Id))
            .ReturnsAsync(pending)
            .ReturnsAsync(answered);
        _repositoryMock
            .Setup(x => x.TryRespondAsync(pending.Id, UserInviteStatus.Accepted, Now))
            .ReturnsAsync(false);

        var result = await CreateSut().RespondAsync(userId, pending.Id, true, CancellationToken.None);

        result.Outcome.Should().Be(InviteRespondOutcome.AlreadyAnswered);
    }

    [Fact]
    public async Task RespondAsync_DeadlinePassedBetweenReadAndWrite_ReturnsExpired()
    {
        var userId = Guid.NewGuid();
        var pending = CreateInvite(userId);
        var overdue = CreateInvite(userId, UserInviteStatus.Expired);
        overdue.Id = pending.Id;

        _repositoryMock.SetupSequence(x => x.GetByIdAsync(pending.Id))
            .ReturnsAsync(pending)
            .ReturnsAsync(overdue);
        _repositoryMock
            .Setup(x => x.TryRespondAsync(pending.Id, UserInviteStatus.Accepted, Now))
            .ReturnsAsync(false);

        var result = await CreateSut().RespondAsync(userId, pending.Id, true, CancellationToken.None);

        result.Outcome.Should().Be(InviteRespondOutcome.Expired);
    }

    [Fact]
    public async Task GetOccupiedSeatsAsync_CountsDistinctClientsWithActiveInvites()
    {
        var partnerId = Guid.NewGuid();
        var firstClient = Guid.NewGuid();
        var secondClient = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetActiveByPartnerAsync(partnerId, Now))
            .ReturnsAsync(new List<UserInvite>
            {
                CreateInvite(firstClient),
                CreateInvite(firstClient, UserInviteStatus.Accepted),
                CreateInvite(secondClient)
            });

        var occupied = await CreateSut().GetOccupiedSeatsAsync(partnerId, CancellationToken.None);

        occupied.Should().Be(2);
    }

    [Fact]
    public async Task GetOccupiedSeatsAsync_NoActiveInvites_ReturnsZero()
    {
        _repositoryMock
            .Setup(x => x.GetActiveByPartnerAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<UserInvite>());

        var occupied = await CreateSut().GetOccupiedSeatsAsync(Guid.NewGuid(), CancellationToken.None);

        occupied.Should().Be(0);
    }

    [Fact]
    public async Task ExpireOverdueAsync_ExpiresInvitesUsingCurrentTime()
    {
        _repositoryMock.Setup(x => x.ExpireOverdueAsync(Now)).ReturnsAsync(3);

        var expired = await CreateSut().ExpireOverdueAsync(CancellationToken.None);

        expired.Should().Be(3);
        _repositoryMock.Verify(x => x.ExpireOverdueAsync(Now), Times.Once);
    }
}
