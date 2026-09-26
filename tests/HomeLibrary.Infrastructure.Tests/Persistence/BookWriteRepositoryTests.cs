using HomeLibrary.Application.Books;
using HomeLibrary.Application.Exceptions;
using HomeLibrary.Domain.Books;
using Microsoft.Extensions.DependencyInjection;

namespace HomeLibrary.Infrastructure.Tests.Persistence;

[Collection(DatabaseCollection.NAME)]
public sealed class BookWriteRepositoryTests(DatabaseFixture fixture) : IDisposable
{
    private const long MISSING_BOOK_ID = long.MaxValue;
    private const int FIRST_VERSION = 1;
    private const int SECOND_VERSION = 2;

    private readonly IServiceScope _scope = fixture.Services.CreateScope();

    private IBookWriteRepository Writer => _scope.ServiceProvider.GetRequiredService<IBookWriteRepository>();

    private IBookReadRepository Reader => _scope.ServiceProvider.GetRequiredService<IBookReadRepository>();

    public void Dispose() => _scope.Dispose();

    [Fact]
    public async Task Add_StoresAllAttributes()
    {
        var details = new BookDetails(
            Title: "The Master and Margarita",
            Author: "Mikhail Bulgakov",
            PublicationYear: 1967,
            Isbn: "978-5-17-090630-7",
            Publisher: "AST",
            PageCount: 480,
            Genre: "Novel",
            Notes: "A gift",
            TableOfContents: new TableOfContents(
                "<toc><ol><li><strong>Chapter 1.</strong> <em>Never Talk to Strangers</em></li></ol></toc>"));

        var id = await Writer.Add(details, CancellationToken.None);
        var book = await Reader.Get(id, CancellationToken.None);

        Assert.NotNull(book);
        Assert.Equal(id, book.Id);
        Assert.Equal(FIRST_VERSION, book.Version);
        Assert.Equal(details, book.Details);
    }

    [Fact]
    public async Task Update_ChangesAttributesAndIncrementsVersion()
    {
        var id = await Writer.Add(BookSamples.Create("Old title"), CancellationToken.None);

        await Writer.Update(id, FIRST_VERSION, BookSamples.Create("New title"), CancellationToken.None);

        var book = await Reader.Get(id, CancellationToken.None);

        Assert.NotNull(book);
        Assert.Equal("New title", book.Details.Title);
        Assert.Equal(SECOND_VERSION, book.Version);
        Assert.True(book.UpdatedAt >= book.CreatedAt);
    }

    [Fact]
    public async Task Update_WithStaleVersion_ThrowsConcurrencyException()
    {
        var id = await Writer.Add(BookSamples.Create("Concurrent"), CancellationToken.None);

        await Writer.Update(id, FIRST_VERSION, BookSamples.Create("First editor"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<BookConcurrencyException>(
            () => Writer.Update(id, FIRST_VERSION, BookSamples.Create("Second editor"), CancellationToken.None));

        Assert.Equal(id, exception.BookId);
    }

    [Fact]
    public async Task Update_MissingBook_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<BookNotFoundException>(
            () => Writer.Update(MISSING_BOOK_ID, FIRST_VERSION, BookSamples.Create("Missing"), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_RemovesBook()
    {
        var id = await Writer.Add(BookSamples.Create("To delete"), CancellationToken.None);

        await Writer.Delete(id, CancellationToken.None);

        Assert.Null(await Reader.Get(id, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_MissingBook_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<BookNotFoundException>(() => Writer.Delete(MISSING_BOOK_ID, CancellationToken.None));
    }
}
