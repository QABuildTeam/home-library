using HomeLibrary.Domain.Books;
using HomeLibrary.Domain.Exceptions;

namespace HomeLibrary.Domain.Tests.Books;

public sealed class BookDetailsValidatorTests
{
    private const int CURRENT_YEAR = 2026;

    private static readonly DateTimeOffset _now = new(CURRENT_YEAR, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly BookDetails _validDetails = new(
        Title: "The Master and Margarita",
        Author: "Mikhail Bulgakov",
        PublicationYear: 1967,
        Isbn: "978-5-17-090630-7",
        Publisher: "AST",
        PageCount: 480,
        Genre: "Novel",
        Notes: "Line one\nLine two\tindented",
        TableOfContents: null);

    private readonly BookDetailsValidator _validator = new(new FixedTimeProvider(_now));

    [Fact]
    public void Validate_ValidDetails_DoesNotThrow()
    {
        _validator.Validate(_validDetails);
    }

    [Fact]
    public void Validate_OnlyRequiredFields_DoesNotThrow()
    {
        _validator.Validate(new BookDetails("Title", "Author", null, null, null, null, null, null, null));
    }

    [Fact]
    public void Validate_MissingTitleAndAuthor_ReportsBothFields()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => _validator.Validate(_validDetails with { Title = " ", Author = string.Empty }));

        Assert.Equal(
            [
                nameof(BookDetails.Author),
                nameof(BookDetails.Title)
            ],
            exception.Errors.Keys.Order());
    }

    [Fact]
    public void Validate_TooLongTitle_ReportsTitle()
    {
        var details = _validDetails with { Title = new string('a', BookConstraints.TITLE_MAX_LENGTH + 1) };

        var exception = Assert.Throws<DomainValidationException>(() => _validator.Validate(details));

        Assert.Contains(nameof(BookDetails.Title), exception.Errors.Keys);
    }

    [Theory]
    [InlineData("Title\u0001")]
    [InlineData("Title\0")]
    public void Validate_ControlCharacters_AreRejected(string title)
    {
        var exception = Assert.Throws<DomainValidationException>(() => _validator.Validate(_validDetails with { Title = title }));

        Assert.Contains(nameof(BookDetails.Title), exception.Errors.Keys);
    }

    [Fact]
    public void Validate_LoneSurrogate_IsRejected()
    {
        // Built in code: xUnit replaces a lone surrogate in InlineData with U+FFFD, which is a valid character.
        const char HIGH_SURROGATE = '\ud83d';

        var details = _validDetails with { Title = "Broken surrogate " + HIGH_SURROGATE };

        var exception = Assert.Throws<DomainValidationException>(() => _validator.Validate(details));

        Assert.Contains(nameof(BookDetails.Title), exception.Errors.Keys);
    }

    [Fact]
    public void Validate_CharactersOutsideBasicPlane_AreAllowed()
    {
        _validator.Validate(_validDetails with { Title = "Books 📚" });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(CURRENT_YEAR + 1)]
    public void Validate_PublicationYearOutOfRange_ReportsYear(int year)
    {
        var exception = Assert.Throws<DomainValidationException>(() => _validator.Validate(_validDetails with { PublicationYear = year }));

        Assert.Contains(nameof(BookDetails.PublicationYear), exception.Errors.Keys);
    }

    [Fact]
    public void Validate_CurrentYear_IsAllowed()
    {
        _validator.Validate(_validDetails with { PublicationYear = CURRENT_YEAR });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(BookConstraints.MAX_PAGE_COUNT + 1)]
    public void Validate_PageCountOutOfRange_ReportsPageCount(int pageCount)
    {
        var exception = Assert.Throws<DomainValidationException>(() => _validator.Validate(_validDetails with { PageCount = pageCount }));

        Assert.Contains(nameof(BookDetails.PageCount), exception.Errors.Keys);
    }

    [Theory]
    [InlineData("5-17-090630-X")]
    [InlineData("5 17 090630 x")]
    [InlineData("9785170906307")]
    public void Validate_ValidIsbn_DoesNotThrow(string isbn)
    {
        _validator.Validate(_validDetails with { Isbn = isbn });
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("978-5-17-09063X-7")]
    [InlineData("ISBN 5170906307")]
    public void Validate_InvalidIsbn_ReportsIsbn(string isbn)
    {
        var exception = Assert.Throws<DomainValidationException>(() => _validator.Validate(_validDetails with { Isbn = isbn }));

        Assert.Contains(nameof(BookDetails.Isbn), exception.Errors.Keys);
    }
}
