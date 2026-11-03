using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using UserService.Controllers;
using UserService.Dtos;
using UserService.Repositories;
using UserService.Services;
using UserService.Services.Keycloak;
using Xunit;

namespace UserService.UnitTests;

public class UsersControllerOccupancyTests
{
    private const string InternalKey = "internal-key";

    private readonly Mock<IUserInviteService> _inviteServiceMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();

    private UsersController CreateSut(string? keyHeader)
    {
        _configurationMock.Setup(x => x["InternalApi:Key"]).Returns(InternalKey);

        var sut = new UsersController(new Mock<IUserRepository>().Object, new Mock<IKeycloakClient>().Object);

        var httpContext = new DefaultHttpContext();
        if (keyHeader is not null)
        {
            httpContext.Request.Headers["X-Internal-Api-Key"] = keyHeader;
        }

        sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return sut;
    }

    [Fact]
    public async Task GetOccupancy_WithValidKey_ReturnsOccupiedSeats()
    {
        var partnerId = Guid.NewGuid();
        _inviteServiceMock
            .Setup(x => x.GetOccupiedSeatsAsync(partnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var result = await CreateSut(InternalKey).GetOccupancy(
            partnerId, _inviteServiceMock.Object, _configurationMock.Object, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<OccupancyResponse>().Which.OccupiedSeats.Should().Be(7);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-key")]
    public async Task GetOccupancy_WithoutValidKey_ReturnsUnauthorized(string? keyHeader)
    {
        var result = await CreateSut(keyHeader).GetOccupancy(
            Guid.NewGuid(), _inviteServiceMock.Object, _configurationMock.Object, CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        _inviteServiceMock.Verify(
            x => x.GetOccupiedSeatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
