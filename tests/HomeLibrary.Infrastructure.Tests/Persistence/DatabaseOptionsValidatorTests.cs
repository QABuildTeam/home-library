using HomeLibrary.Infrastructure.Persistence;

namespace HomeLibrary.Infrastructure.Tests.Persistence;

public sealed class DatabaseOptionsValidatorTests
{
    private const string CONNECTION_STRING = "Host=localhost;Database=test";

    private readonly DatabaseOptionsValidator _validator = new();

    [Theory]
    [InlineData("library")]
    [InlineData("library_test_0123456789abcdef")]
    [InlineData("_schema")]
    public void Validate_ValidOptions_Succeeds(string schema)
    {
        var result = _validator.Validate(null, new DatabaseOptions { ConnectionString = CONNECTION_STRING, Schema = schema });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Library")]
    [InlineData("1library")]
    [InlineData("library; DROP SCHEMA public")]
    [InlineData("library\"")]
    [InlineData("a_very_long_schema_name_that_exceeds_the_postgresql_identifier_limit")]
    public void Validate_UnsafeSchemaName_Fails(string schema)
    {
        var result = _validator.Validate(null, new DatabaseOptions { ConnectionString = CONNECTION_STRING, Schema = schema });

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_MissingConnectionString_Fails()
    {
        var result = _validator.Validate(null, new DatabaseOptions { ConnectionString = " " });

        Assert.True(result.Failed);
        Assert.Contains(nameof(DatabaseOptions.ConnectionString), result.FailureMessage);
    }
}
