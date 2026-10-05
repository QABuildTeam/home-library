using HomeLibrary.Application.Abstractions;
using HomeLibrary.Application.Books.Ports;
using HomeLibrary.Application.Books.Services;
using HomeLibrary.Domain.Books;

namespace HomeLibrary.Application.Books.UseCases.CreateBook;

/// <summary>
/// Validates the input and adds a new book. Returns the identifier of the new book.
/// </summary>
public sealed class CreateBookHandler(
    IBookDetailsFactory detailsFactory,
    IBookDetailsValidator validator,
    IBookWriteRepository repository) : ICommandHandler<CreateBookCommand, long>
{
    public Task<long> Handle(CreateBookCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var details = detailsFactory.Create(command.Input);

        validator.Validate(details);

        return repository.Add(details, cancellationToken);
    }
}
