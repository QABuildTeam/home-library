using HomeLibrary.Application.Books.Exceptions;
using HomeLibrary.Application.Books.Ports;
using HomeLibrary.Application.Books.UseCases.DeleteBook;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace HomeLibrary.Application.Tests.Books.UseCases.DeleteBook;

public sealed class DeleteBookHandlerTests
{
    private const long BOOK_ID = 9L;

    private readonly IBookWriteRepository _repository = Substitute.For<IBookWriteRepository>();

    [Fact]
    public async Task Handle_DeletesBookWithGivenCancellationToken()
    {
        using var cancellation = new CancellationTokenSource();

        await CreateHandler().Handle(new DeleteBookCommand(BOOK_ID), cancellation.Token);

        await _repository.Received(1).Delete(BOOK_ID, cancellation.Token);
    }

    [Fact]
    public async Task Handle_MissingBook_IsPropagated()
    {
        var notFound = new BookNotFoundException(BOOK_ID);

        _repository
            .Delete(BOOK_ID, Arg.Any<CancellationToken>())
            .ThrowsAsync(notFound);

        var exception = await Assert.ThrowsAsync<BookNotFoundException>(
            () => CreateHandler().Handle(new DeleteBookCommand(BOOK_ID), CancellationToken.None));

        // The same instance: the handler must not catch and replace the exception.
        Assert.Same(notFound, exception);
    }

    [Fact]
    public async Task Handle_NullCommand_ThrowsWithoutTouchingRepository()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateHandler().Handle(null!, CancellationToken.None));

        await _repository.DidNotReceiveWithAnyArgs().Delete(default, default);
    }

    private DeleteBookHandler CreateHandler() => new(_repository);
}
