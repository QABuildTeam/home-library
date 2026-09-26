using HomeLibrary.Application.Books;
using HomeLibrary.Application.Books.Commands;
using HomeLibrary.Application.Exceptions;
using HomeLibrary.Domain.Books;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace HomeLibrary.Application.Tests.Books.Commands;

public sealed class UpdateBookHandlerTests
{
    private const long BOOK_ID = 7L;
    private const int VERSION = 3;

    private static readonly BookInput _input = new("Title", "Author", null, null, null, null, null, null, null);
    private static readonly BookDetails _details = new("Title", "Author", null, null, null, null, null, null, null);

    private readonly IBookDetailsFactory _factory = Substitute.For<IBookDetailsFactory>();
    private readonly IBookDetailsValidator _validator = Substitute.For<IBookDetailsValidator>();
    private readonly IBookWriteRepository _repository = Substitute.For<IBookWriteRepository>();

    public UpdateBookHandlerTests()
    {
        _factory.Create(_input).Returns(_details);
    }

    [Fact]
    public async Task Handle_ValidInput_UpdatesBookWithExpectedVersion()
    {
        await CreateHandler().Handle(new UpdateBookCommand(BOOK_ID, VERSION, _input), CancellationToken.None);

        _validator.Received(1).Validate(_details);
        await _repository.Received(1).Update(BOOK_ID, VERSION, _details, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConcurrencyConflict_IsPropagated()
    {
        _repository
            .Update(BOOK_ID, VERSION, _details, Arg.Any<CancellationToken>())
            .ThrowsAsync(new BookConcurrencyException(BOOK_ID));

        await Assert.ThrowsAsync<BookConcurrencyException>(
            () => CreateHandler().Handle(new UpdateBookCommand(BOOK_ID, VERSION, _input), CancellationToken.None));
    }

    private UpdateBookHandler CreateHandler() => new(_factory, _validator, _repository);
}
