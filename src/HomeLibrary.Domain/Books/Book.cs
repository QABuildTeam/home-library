namespace HomeLibrary.Domain.Books;

/// <summary>
/// Book stored in the home library.
/// </summary>
/// <param name="id">Database identifier.</param>
/// <param name="details">Descriptive attributes.</param>
/// <param name="version">Row version used for optimistic concurrency control.</param>
/// <param name="createdAt">Creation timestamp.</param>
/// <param name="updatedAt">Last modification timestamp.</param>
public sealed class Book(long id, BookDetails details, int version, DateTimeOffset createdAt, DateTimeOffset updatedAt)
{
    public long Id { get; } = id;

    public BookDetails Details { get; } = details;

    public int Version { get; } = version;

    public DateTimeOffset CreatedAt { get; } = createdAt;

    public DateTimeOffset UpdatedAt { get; } = updatedAt;
}
