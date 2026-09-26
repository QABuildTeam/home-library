using HomeLibrary.Application.Abstractions;
using HomeLibrary.Domain.Books;

namespace HomeLibrary.Application.Books.Commands;

/// <summary>
/// Validates the input and saves the new attributes of an existing book.
/// </summary>
public sealed class UpdateBookHandler(
    IBookDetailsFactory detailsFactory,
    IBookDetailsValidator validator,
    IBookWriteRepository repository) : ICommandHandler<UpdateBookCommand>
{
    public Task Handle(UpdateBookCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var details = detailsFactory.Create(command.Input);

        validator.Validate(details);

        return repository.Update(command.Id, command.Version, details, cancellationToken);
    }
}
