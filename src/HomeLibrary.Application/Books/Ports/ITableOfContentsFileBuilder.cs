using HomeLibrary.Application.Books.Models;
using HomeLibrary.Domain.Books;

namespace HomeLibrary.Application.Books.Ports;

/// <summary>
/// Builds the file with the exported table of contents of a book.
/// </summary>
public interface ITableOfContentsFileBuilder
{
    /// <summary>
    /// Builds the export file.
    /// </summary>
    /// <param name="book">Book whose table of contents is exported; used for the file name and metadata.</param>
    /// <param name="tableOfContents">Table of contents of the book.</param>
    TableOfContentsFile Build(Book book, TableOfContents tableOfContents);
}
