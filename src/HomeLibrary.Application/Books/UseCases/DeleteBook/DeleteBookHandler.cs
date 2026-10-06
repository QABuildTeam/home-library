using HomeLibrary.Application.Abstractions;
using HomeLibrary.Application.Books.Ports;

namespace HomeLibrary.Application.Books.UseCases.DeleteBook;

/// <summary>
/// Deletes a book from the library.
/// </summary>
internal sealed class DeleteBookHandler(IBookWriteRepository repository) : ICommandHandler<DeleteBookCommand>
{
    public Task Handle(DeleteBookCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return repository.Delete(command.Id, cancellationToken);
    }
}
