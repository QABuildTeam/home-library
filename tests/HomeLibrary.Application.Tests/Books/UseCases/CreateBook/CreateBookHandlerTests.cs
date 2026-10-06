using HomeLibrary.Application.Books.Models;
using HomeLibrary.Application.Books.Ports;
using HomeLibrary.Application.Books.Services;
using HomeLibrary.Application.Books.UseCases.CreateBook;
using HomeLibrary.Domain.Books;
using HomeLibrary.Domain.Exceptions;
using NSubstitute;

namespace HomeLibrary.Application.Tests.Books.UseCases.CreateBook;

public sealed class CreateBookHandlerTests
{
    private const long NEW_BOOK_ID = 42L;

    private static readonly BookInput _input = new("Title", "Author", null, null, null, null, null, null, null);
    private static readonly BookDetails _details = new("Title", "Author", null, null, null, null, null, null, null);

    private readonly IBookDetailsFactory _factory = Substitute.For<IBookDetailsFactory>();
    private readonly IBookDetailsValidator _validator = Substitute.For<IBookDetailsValidator>();
    private readonly IBookWriteRepository _repository = Substitute.For<IBookWriteRepository>();

    public CreateBookHandlerTests()
    {
        _factory.Create(_input).Returns(_details);
    }

    [Fact]
    public async Task Handle_ValidInput_AddsBookAndReturnsId()
    {
        _repository.Add(_details, Arg.Any<CancellationToken>()).Returns(NEW_BOOK_ID);

        var id = await CreateHandler().Handle(new CreateBookCommand(_input), CancellationToken.None);

        Assert.Equal(NEW_BOOK_ID, id);
        _validator.Received(1).Validate(_details);
    }

    [Fact]
    public async Task Handle_InvalidInput_DoesNotTouchRepository()
    {
        _validator
            .When(validator => validator.Validate(_details))
            .Throw(new DomainValidationException(nameof(BookDetails.Title), "Title is required."));

        await Assert.ThrowsAsync<DomainValidationException>(
            () => CreateHandler().Handle(new CreateBookCommand(_input), CancellationToken.None));

        await _repository.DidNotReceiveWithAnyArgs().Add(default!, default);
    }

    private CreateBookHandler CreateHandler() => new(_factory, _validator, _repository);
}
