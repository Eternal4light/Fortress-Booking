namespace UserService.UnitTests;

/// <summary>Управляемые часы для тестов: время меняется только вручную.</summary>
internal sealed class TestTimeProvider : TimeProvider
{
    public TestTimeProvider(DateTime utcNow)
    {
        UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }

    public DateTime UtcNow { get; set; }

    public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
}
