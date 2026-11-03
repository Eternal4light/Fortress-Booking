using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using PartnerService.Controllers;
using PartnerService.Core.Abstractions.Repositories;
using PartnerService.Core.Domain;
using PartnerService.WebHost.Messaging;
using PartnerService.WebHost.Models;
using PartnerService.WebHost.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PartnerService.UnitTests
{
    public class InvitesControllerTests
    {
        private readonly Fixture _fixture = new();
        private readonly Mock<IRepository<Partner>> _partnersRepositoryMock = new();
        private readonly Mock<IRepository<Activity>> _activitiesRepositoryMock = new();
        private readonly Mock<IRepository<Invite>> _invitesRepositoryMock = new();
        private readonly Mock<IInvitePublisher> _invitePublisherMock = new();
        private readonly Mock<IUserServiceClient> _userServiceClientMock = new();
        private readonly Mock<IConfiguration> _configurationMock = new();

        public InvitesControllerTests()
        {
            // по умолчанию UserService "не ответил" — ранняя проверка вместимости пропускается
            _userServiceClientMock
                .Setup(x => x.GetOccupiedSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int?)null);
        }

        private InvitesController CreateSut(string? apiKeyHeader = null)
        {
            _configurationMock.Setup(x => x["PartnerApi:ApiKey"]).Returns("secret-key");

            var sut = new InvitesController(
                _partnersRepositoryMock.Object,
                _activitiesRepositoryMock.Object,
                _invitesRepositoryMock.Object,
                _invitePublisherMock.Object,
                _userServiceClientMock.Object,
                _configurationMock.Object);

            var httpContext = new DefaultHttpContext();
            if (apiKeyHeader is not null)
            {
                httpContext.Request.Headers["X-Api-Key"] = apiKeyHeader;
            }

            sut.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return sut;
        }

        [Fact]
        public async Task IssueInviteAsync_ReturnsUnauthorized_WithoutApiKeyOrJwt()
        {
            var sut = CreateSut();

            var result = await sut.IssueInviteAsync(Guid.NewGuid(), new IssueInviteRequest(), CancellationToken.None);

            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task IssueInviteAsync_IssuesInvite_WithValidApiKey()
        {
            var partner = _fixture.Build<Partner>().Without(x => x.Invites).Create();
            var activity = _fixture.Create<Activity>();

            _partnersRepositoryMock.Setup(x => x.GetByIdAsync(partner.Id)).ReturnsAsync(partner);
            _activitiesRepositoryMock.Setup(x => x.GetByIdAsync(activity.Id)).ReturnsAsync(activity);
            _invitePublisherMock
                .Setup(x => x.PublishAsync(
                    It.Is<InviteBroadcastMessage>(m =>
                        m.PartnerId == partner.Id &&
                        m.PartnerName == partner.Name &&
                        m.Activity == activity.Name),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut(apiKeyHeader: "secret-key");

            var request = new IssueInviteRequest { ActivityId = activity.Id, Description = "Заходите к нам" };

            var result = await sut.IssueInviteAsync(partner.Id, request, CancellationToken.None);

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<InviteResponse>().Subject;

            response.QueuedForDelivery.Should().BeTrue();
            response.PartnerId.Should().Be(partner.Id);
            response.ActivityId.Should().Be(activity.Id);

            _invitesRepositoryMock.Verify(
                x => x.AddAsync(It.Is<Invite>(i => i.PartnerId == partner.Id && i.ActivityId == activity.Id)),
                Times.Once);
        }

        [Fact]
        public async Task IssueInviteAsync_ReturnsNotFound_WhenPartnerMissing()
        {
            _partnersRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Partner)null!);

            var sut = CreateSut(apiKeyHeader: "secret-key");

            var result = await sut.IssueInviteAsync(Guid.NewGuid(), new IssueInviteRequest(), CancellationToken.None);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        private Partner CreatePartnerWithCapacity(int capacity) =>
            _fixture.Build<Partner>().Without(x => x.Invites).With(x => x.Capacity, capacity).Create();

        [Theory]
        [InlineData(5, 5)]
        [InlineData(5, 8)]
        public async Task IssueInviteAsync_ReturnsConflict_WhenNoFreeSeats(int capacity, int occupied)
        {
            var partner = CreatePartnerWithCapacity(capacity);
            var activity = _fixture.Create<Activity>();

            _partnersRepositoryMock.Setup(x => x.GetByIdAsync(partner.Id)).ReturnsAsync(partner);
            _activitiesRepositoryMock.Setup(x => x.GetByIdAsync(activity.Id)).ReturnsAsync(activity);
            _userServiceClientMock
                .Setup(x => x.GetOccupiedSeatsAsync(partner.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(occupied);

            var sut = CreateSut(apiKeyHeader: "secret-key");

            var result = await sut.IssueInviteAsync(
                partner.Id, new IssueInviteRequest { ActivityId = activity.Id }, CancellationToken.None);

            result.Result.Should().BeOfType<ConflictObjectResult>();
            _invitesRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Invite>()), Times.Never);
            _invitePublisherMock.Verify(
                x => x.PublishAsync(It.IsAny<InviteBroadcastMessage>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task IssueInviteAsync_IssuesInvite_WhenThereIsStillAFreeSeat()
        {
            var partner = CreatePartnerWithCapacity(5);
            var activity = _fixture.Create<Activity>();

            _partnersRepositoryMock.Setup(x => x.GetByIdAsync(partner.Id)).ReturnsAsync(partner);
            _activitiesRepositoryMock.Setup(x => x.GetByIdAsync(activity.Id)).ReturnsAsync(activity);
            _userServiceClientMock
                .Setup(x => x.GetOccupiedSeatsAsync(partner.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(4);
            _invitePublisherMock
                .Setup(x => x.PublishAsync(It.IsAny<InviteBroadcastMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut(apiKeyHeader: "secret-key");

            var result = await sut.IssueInviteAsync(
                partner.Id, new IssueInviteRequest { ActivityId = activity.Id }, CancellationToken.None);

            result.Result.Should().BeOfType<OkObjectResult>();
            _invitesRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Invite>()), Times.Once);
        }

        [Fact]
        public async Task IssueInviteAsync_PublishesPartnerCapacityWithTheMessage()
        {
            var partner = CreatePartnerWithCapacity(7);
            var activity = _fixture.Create<Activity>();

            _partnersRepositoryMock.Setup(x => x.GetByIdAsync(partner.Id)).ReturnsAsync(partner);
            _activitiesRepositoryMock.Setup(x => x.GetByIdAsync(activity.Id)).ReturnsAsync(activity);
            _invitePublisherMock
                .Setup(x => x.PublishAsync(It.IsAny<InviteBroadcastMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut(apiKeyHeader: "secret-key");

            await sut.IssueInviteAsync(
                partner.Id, new IssueInviteRequest { ActivityId = activity.Id }, CancellationToken.None);

            _invitePublisherMock.Verify(
                x => x.PublishAsync(
                    It.Is<InviteBroadcastMessage>(m => m.Capacity == 7 && m.PartnerId == partner.Id),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task IssueInviteAsync_StillIssuesInvite_WhenOccupancyIsUnknown()
        {
            // UserService не ответил: ранняя проверка пропускается, окончательный лимит
            // применит сам UserService при рассылке
            var partner = CreatePartnerWithCapacity(1);
            var activity = _fixture.Create<Activity>();

            _partnersRepositoryMock.Setup(x => x.GetByIdAsync(partner.Id)).ReturnsAsync(partner);
            _activitiesRepositoryMock.Setup(x => x.GetByIdAsync(activity.Id)).ReturnsAsync(activity);
            _invitePublisherMock
                .Setup(x => x.PublishAsync(It.IsAny<InviteBroadcastMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut(apiKeyHeader: "secret-key");

            var result = await sut.IssueInviteAsync(
                partner.Id, new IssueInviteRequest { ActivityId = activity.Id }, CancellationToken.None);

            result.Result.Should().BeOfType<OkObjectResult>();
        }
    }
}
