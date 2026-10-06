using HomeLibrary.Application.Abstractions;
using HomeLibrary.Application.Books.Ports;
using HomeLibrary.Application.Books.Services;
using HomeLibrary.Domain.Books;

namespace HomeLibrary.Application.Books.UseCases.UpdateBook;

/// <summary>
/// Validates the input and saves the new attributes of an existing book.
/// </summary>
internal sealed class UpdateBookHandler(
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
