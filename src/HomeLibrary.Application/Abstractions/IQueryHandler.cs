namespace HomeLibrary.Application.Abstractions;

/// <summary>
/// Handles a query that reads the application state without changing it.
/// </summary>
/// <typeparam name="TQuery">Query type.</typeparam>
/// <typeparam name="TResult">Result type.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);
}
