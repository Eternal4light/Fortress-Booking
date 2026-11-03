using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UserService.Controllers;
using UserService.Domain;
using UserService.Dtos;
using UserService.Repositories;
using UserService.Services.Keycloak;
using Xunit;

namespace UserService.UnitTests;

public class AuthControllerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IKeycloakClient> _keycloakMock = new();

    private AuthController CreateSut() =>
        new(_userRepositoryMock.Object, _keycloakMock.Object);

    [Fact]
    public async Task Register_CreatesClientAndReturnsToken()
    {
        var keycloakId = Guid.NewGuid();

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _keycloakMock
            .Setup(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), UserRole.Client, It.IsAny<CancellationToken>()))
            .ReturnsAsync(keycloakId);
        _keycloakMock
            .Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-123");

        User? addedUser = null;
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => addedUser = u)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.Register(new RegisterRequest
        {
            Email = "new-client@example.com",
            Password = "Password123!",
            FirstName = "Клиент",
            LastName = "Тестовый",
            Activity = "Красота"
        });

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<AuthResponse>().Subject;

        response.Token.Should().Be("token-123");
        addedUser.Should().NotBeNull();
        addedUser!.Id.Should().Be(keycloakId);
        addedUser.Role.Should().Be(UserRole.Client);
        addedUser.Activity.Should().Be("Красота");
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenEmailAlreadyExists()
    {
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new User { Email = "taken@example.com" });

        var sut = CreateSut();

        var result = await sut.Register(new RegisterRequest { Email = "taken@example.com", Password = "x" });

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_ForWrongPassword()
    {
        _keycloakMock
            .Setup(x => x.LoginAsync("user@example.com", "wrong", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var sut = CreateSut();

        var result = await sut.Login(new LoginRequest { Email = "user@example.com", Password = "wrong" });

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_ReturnsToken_ForCorrectPassword()
    {
        var user = new User { Email = "user@example.com", IsActive = true, Role = UserRole.Manager };

        _keycloakMock
            .Setup(x => x.LoginAsync("user@example.com", "correct", It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-abc");
        _userRepositoryMock.Setup(x => x.GetByEmailAsync("user@example.com")).ReturnsAsync(user);

        var sut = CreateSut();

        var result = await sut.Login(new LoginRequest { Email = "user@example.com", Password = "correct" });

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<AuthResponse>().Subject;
        response.Token.Should().Be("token-abc");
    }
}
