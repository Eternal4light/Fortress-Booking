using FluentAssertions;
using UserService.Domain;
using UserService.Repositories;
using Xunit;

namespace UserService.UnitTests;

public class InMemoryUserRepositoryTests
{
    private static User NewUser(UserRole role, string? activity = null, string? email = null) => new()
    {
        Id = Guid.NewGuid(),
        Email = email ?? $"{Guid.NewGuid():N}@example.com",
        FirstName = "Тест",
        LastName = "Тестов",
        Role = role,
        Activity = activity,
        IsActive = true
    };

    [Fact]
    public async Task GetAllAsync_FiltersByRoleAndActivity()
    {
        var beautyClient = NewUser(UserRole.Client, "Красота");
        var itClient = NewUser(UserRole.Client, "IT");
        var manager = NewUser(UserRole.Manager);

        var repository = new InMemoryUserRepository(new[] { beautyClient, itClient, manager });

        var result = await repository.GetAllAsync(UserRole.Client, "Красота");

        result.Should().ContainSingle(u => u.Id == beautyClient.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_IsCaseInsensitive()
    {
        var user = NewUser(UserRole.Client, email: "Client@Example.com");
        var repository = new InMemoryUserRepository(new[] { user });

        var result = await repository.GetByEmailAsync("client@example.com");

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsAddedUser()
    {
        var repository = new InMemoryUserRepository(Enumerable.Empty<User>());
        var user = NewUser(UserRole.Client);

        await repository.AddAsync(user);
        var result = await repository.GetByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser()
    {
        var user = NewUser(UserRole.Client);
        var repository = new InMemoryUserRepository(new[] { user });

        await repository.DeleteAsync(user.Id);
        var result = await repository.GetByIdAsync(user.Id);

        result.Should().BeNull();
    }
}
