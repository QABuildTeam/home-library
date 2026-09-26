namespace HomeLibrary.Application.Books;

/// <summary>
/// Exported table of contents ready to be sent to the user as a file.
/// </summary>
/// <param name="FileName">Suggested file name.</param>
/// <param name="ContentType">MIME type of the file.</param>
/// <param name="Content">File content.</param>
public sealed record TableOfContentsFile(string FileName, string ContentType, byte[] Content);
