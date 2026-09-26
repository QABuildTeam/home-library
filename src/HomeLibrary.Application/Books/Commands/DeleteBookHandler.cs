using HomeLibrary.Application.Abstractions;

namespace HomeLibrary.Application.Books.Commands;

/// <summary>
/// Deletes a book from the library.
/// </summary>
public sealed class DeleteBookHandler(IBookWriteRepository repository) : ICommandHandler<DeleteBookCommand>
{
    public Task Handle(DeleteBookCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return repository.Delete(command.Id, cancellationToken);
    }
}
