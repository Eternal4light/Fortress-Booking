using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using UserService.Domain;
using UserService.Services;
using Xunit;

namespace UserService.UnitTests;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateSut() =>
        new(Options.Create(new JwtOptions
        {
            Key = "unit-test-signing-key-should-be-long-enough",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiresMinutes = 60
        }));

    [Fact]
    public void CreateToken_ProducesTokenWithExpectedClaims()
    {
        var sut = CreateSut();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "manager@example.com",
            FirstName = "Мария",
            LastName = "Менеджерова",
            Role = UserRole.Manager
        };

        var token = sut.CreateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Value == "Manager");
    }
}
