namespace HomeLibrary.Web.Tests;

/// <summary>
/// Shares one running application and one temporary database schema between all web test classes.
/// </summary>
[CollectionDefinition(NAME)]
public sealed class WebCollection : ICollectionFixture<HomeLibraryWebApplicationFactory>
{
    public const string NAME = "Web";
}
