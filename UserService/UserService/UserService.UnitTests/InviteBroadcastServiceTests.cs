using FluentAssertions;
using Moq;
using UserService.Domain;
using UserService.Dtos;
using UserService.Repositories;
using UserService.Services;
using Xunit;

namespace UserService.UnitTests;

public class InviteBroadcastServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid PartnerId = Guid.NewGuid();

    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUserInviteRepository> _inviteRepositoryMock = new();
    private readonly TestTimeProvider _time = new(Now);
    private readonly List<UserInvite> _created = new();

    public InviteBroadcastServiceTests()
    {
        _inviteRepositoryMock
            .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<UserInvite>>()))
            .Callback<IEnumerable<UserInvite>>(invites => _created.AddRange(invites))
            .Returns<IEnumerable<UserInvite>>(invites => Task.FromResult(invites.Count()));

        _inviteRepositoryMock
            .Setup(x => x.GetActiveByPartnerAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<UserInvite>());
    }

    private InviteBroadcastService CreateSut() =>
        new(_userRepositoryMock.Object, _inviteRepositoryMock.Object, _time);

    private static User CreateClient() => new()
    {
        Id = Guid.NewGuid(),
        Role = UserRole.Client,
        Activity = "Красота",
        Email = $"{Guid.NewGuid()}@example.com"
    };

    private void GivenClients(params User[] clients) =>
        _userRepositoryMock
            .Setup(x => x.GetAllAsync(UserRole.Client, "Красота"))
            .ReturnsAsync(clients);

    private void GivenActiveInvitesFor(params Guid[] userIds) =>
        _inviteRepositoryMock
            .Setup(x => x.GetActiveByPartnerAsync(PartnerId, Now))
            .ReturnsAsync(userIds.Select(id => new UserInvite
            {
                Id = Guid.NewGuid(),
                UserId = id,
                PartnerId = PartnerId,
                Status = UserInviteStatus.Pending,
                ExpiresAt = Now.AddHours(1)
            }).ToList());

    private static BroadcastInviteRequest CreateRequest(int? capacity) => new()
    {
        PartnerId = PartnerId,
        PartnerName = "Beauty Space",
        Specialization = "Бьюти",
        Activity = "Красота",
        Description = "Заходите",
        Capacity = capacity
    };

    [Fact]
    public async Task BroadcastAsync_CreatesPendingInvitesValidForTwentyFourHours()
    {
        var clients = new[] { CreateClient(), CreateClient() };
        GivenClients(clients);

        var count = await CreateSut().BroadcastAsync(CreateRequest(capacity: 10), CancellationToken.None);

        count.Should().Be(2);
        _created.Should().HaveCount(2);
        _created.Should().OnlyContain(i =>
            i.Status == UserInviteStatus.Pending &&
            i.IssuedAt == Now &&
            i.ExpiresAt == Now.AddHours(24) &&
            i.PartnerId == PartnerId);
        _created.Select(i => i.UserId).Should().BeEquivalentTo(clients.Select(c => c.Id));
    }

    [Fact]
    public async Task BroadcastAsync_MoreClientsThanCapacity_InvitesOnlyAsManyAsThereAreSeats()
    {
        GivenClients(Enumerable.Range(0, 5).Select(_ => CreateClient()).ToArray());

        var count = await CreateSut().BroadcastAsync(CreateRequest(capacity: 3), CancellationToken.None);

        count.Should().Be(3);
        _created.Should().HaveCount(3);
    }

    [Fact]
    public async Task BroadcastAsync_SomeSeatsAlreadyTaken_InvitesOnlyForFreeSeats()
    {
        var clients = Enumerable.Range(0, 5).Select(_ => CreateClient()).ToArray();
        GivenClients(clients);
        GivenActiveInvitesFor(clients[0].Id, clients[1].Id);

        var count = await CreateSut().BroadcastAsync(CreateRequest(capacity: 3), CancellationToken.None);

        count.Should().Be(1);
        _created.Should().ContainSingle();
        _created[0].UserId.Should().NotBe(clients[0].Id).And.NotBe(clients[1].Id);
    }

    [Fact]
    public async Task BroadcastAsync_NoFreeSeats_CreatesNoInvites()
    {
        var clients = Enumerable.Range(0, 4).Select(_ => CreateClient()).ToArray();
        GivenClients(clients);
        GivenActiveInvitesFor(clients[0].Id, clients[1].Id);

        var count = await CreateSut().BroadcastAsync(CreateRequest(capacity: 2), CancellationToken.None);

        count.Should().Be(0);
        _created.Should().BeEmpty();
    }

    [Fact]
    public async Task BroadcastAsync_ClientWithActiveInvite_DoesNotGetSecondOneOrSecondSeat()
    {
        var invitedClient = CreateClient();
        var newClient = CreateClient();
        GivenClients(invitedClient, newClient);
        GivenActiveInvitesFor(invitedClient.Id);

        var count = await CreateSut().BroadcastAsync(CreateRequest(capacity: 5), CancellationToken.None);

        count.Should().Be(1);
        _created.Should().ContainSingle().Which.UserId.Should().Be(newClient.Id);
    }

    [Fact]
    public async Task BroadcastAsync_WithoutCapacity_InvitesEveryMatchingClient()
    {
        GivenClients(Enumerable.Range(0, 4).Select(_ => CreateClient()).ToArray());

        var count = await CreateSut().BroadcastAsync(CreateRequest(capacity: null), CancellationToken.None);

        count.Should().Be(4);
        _inviteRepositoryMock.Verify(
            x => x.GetActiveByPartnerAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()),
            Times.Never);
    }
}
