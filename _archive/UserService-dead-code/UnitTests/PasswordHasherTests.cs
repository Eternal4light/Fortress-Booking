using FluentAssertions;
using UserService.Services;
using Xunit;

namespace UserService.UnitTests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var hash = _hasher.Hash("Sup3rSecret!");

        _hasher.Verify("Sup3rSecret!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hash = _hasher.Hash("Sup3rSecret!");

        _hasher.Verify("WrongPassword", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_ProducesDifferentHashes_ForSamePassword()
    {
        var hash1 = _hasher.Hash("Sup3rSecret!");
        var hash2 = _hasher.Hash("Sup3rSecret!");

        hash1.Should().NotBe(hash2);
    }

    [Theory]
    [InlineData("not-a-valid-hash")]
    [InlineData("1.2")]
    [InlineData("")]
    public void Verify_ReturnsFalse_ForMalformedHash(string malformedHash)
    {
        _hasher.Verify("anything", malformedHash).Should().BeFalse();
    }
}
