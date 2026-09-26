namespace HomeLibrary.Infrastructure.Persistence;

/// <summary>
/// Custom SQLSTATE codes raised by the library stored procedures.
/// </summary>
internal static class DatabaseErrorCodes
{
    public const string BOOK_NOT_FOUND = "HL404";
    public const string BOOK_CONCURRENCY_CONFLICT = "HL409";
}
