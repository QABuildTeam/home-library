namespace HomeLibrary.Domain.Tests.Books;

/// <summary>
/// Time provider that always returns the same moment.
/// </summary>
/// <param name="now">Moment returned as the current UTC time.</param>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
