using Dvld.TestSupport;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Infrastructure.IntegrationTests;

public sealed class SqlServerTestEnvironmentValidationTests
{
    private const string ExpectedInstance = "TESTHOST|MSSQLSERVER";

    [Fact]
    public void ValidateConfiguration_AcceptsNormalConnectionStringWithoutAttachedFile()
    {
        var target = SqlServerTestEnvironment.ValidateConfiguration(
            "Server=localhost,1433;Initial Catalog=master;Integrated Security=True;Encrypt=True",
            ExpectedInstance);

        Assert.Equal(ExpectedInstance, target.ExpectedInstanceIdentity);
        Assert.Equal(
            "master",
            new SqlConnectionStringBuilder(target.AdminConnectionString).InitialCatalog);
    }

    [Fact]
    public void GenericBuilder_ContainsOnlyExplicitlySuppliedConnectionStringKeys()
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = "Server=localhost;Integrated Security=True"
        };

        Assert.True(builder.ContainsKey("Server"));
        Assert.False(builder.ContainsKey("AttachDBFilename"));
    }

    [Fact]
    public void ValidateConfiguration_UsesMasterForLifecycleConnection()
    {
        var target = SqlServerTestEnvironment.ValidateConfiguration(
            "Server=localhost,1433;Initial Catalog=master;Integrated Security=True;Encrypt=True",
            ExpectedInstance);

        var builder = new SqlConnectionStringBuilder(
            target.AdminConnectionString);

        Assert.Equal("master", builder.InitialCatalog);
        Assert.Equal("localhost,1433", builder.DataSource);
        Assert.True(builder.IntegratedSecurity);
        Assert.False(builder.Enlist);
        Assert.Equal(ExpectedInstance, target.ExpectedInstanceIdentity);
    }

    [Fact]
    public void ValidateConfiguration_RequiresExplicitConnection()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateConfiguration(
                null,
                ExpectedInstance));

        Assert.Contains("DVLD_TEST_CONNECTION", exception.Message);
    }

    [Fact]
    public void ValidateConfiguration_RequiresExpectedInstance()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateConfiguration(
                "Server=localhost;Integrated Security=True",
                null));

        Assert.Contains("DVLD_TEST_EXPECTED_INSTANCE", exception.Message);
    }

    [Theory]
    [InlineData("AttachDBFilename")]
    [InlineData("attachdbfilename")]
    [InlineData("aTtAcHdBfIlEnAmE")]
    [InlineData("AttachDBFilename=")]
    public void ValidateConfiguration_RejectsAttachedDatabaseFile(
        string keyword)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateConfiguration(
                $"Server=localhost;Initial Catalog=master;Integrated Security=True;{keyword}=C:\\data\\source.mdf",
                ExpectedInstance));

        Assert.Contains("AttachDBFilename", exception.Message);
    }

    [Fact]
    public void ValidateConfiguration_RejectsSqlAuthenticationCredentials()
    {
        Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateConfiguration(
                "Server=localhost;Integrated Security=True;User ID=test-user;Password=must-not-be-retained",
                ExpectedInstance));
    }

    [Theory]
    [InlineData("DVLD")]
    [InlineData("dvldf")]
    public void ValidateConfiguration_RejectsProtectedApplicationCatalog(
        string catalog)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateConfiguration(
                $"Server=localhost;Initial Catalog={catalog};Integrated Security=True",
                ExpectedInstance));

        Assert.Contains("application database", exception.Message);
    }

    [Theory]
    [InlineData("Server=localhost;Integrated Security=True;User Instance=True")]
    [InlineData("Server=localhost;Integrated Security=True;Failover Partner=other")]
    public void ValidateConfiguration_RejectsRedirectOptions(
        string connectionString)
    {
        Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateConfiguration(
                connectionString,
                ExpectedInstance));
    }

    [Fact]
    public void BuildDatabaseName_ProducesUniqueGuidBasedNames()
    {
        var first = SqlServerTestEnvironment.BuildDatabaseName(
            "DVLD_IntegrationTests_Api_");
        var second = SqlServerTestEnvironment.BuildDatabaseName(
            "DVLD_IntegrationTests_Api_");

        Assert.NotEqual(first, second);
        Assert.StartsWith("DVLD_IntegrationTests_Api_", first);
        Assert.True(Guid.TryParseExact(first[^32..], "N", out _));
    }

    [Theory]
    [InlineData("DVLD_IntegrationTests_")]
    [InlineData("DVLD_IntegrationTests_Api_")]
    public void BuildDatabaseName_AcceptsOnlyApprovedPrefixes(string prefix)
    {
        var databaseName = SqlServerTestEnvironment.BuildDatabaseName(prefix);

        SqlServerTestEnvironment.ValidateGeneratedDatabaseName(databaseName);
    }

    [Theory]
    [InlineData("DVLD_Test_")]
    [InlineData("DVLD_IntegrationTests_Api_Extra_")]
    [InlineData("")]
    public void BuildDatabaseName_RejectsUnapprovedPrefixes(string prefix)
    {
        Assert.Throws<ArgumentException>(
            () => SqlServerTestEnvironment.BuildDatabaseName(prefix));
    }

    [Theory]
    [InlineData("DVLD_IntegrationTests_not-a-guid")]
    [InlineData("DVLD_IntegrationTests_Api_000000000000000000000000000000000")]
    [InlineData("DVLDf_00000000000000000000000000000000")]
    public void ValidateGeneratedDatabaseName_RejectsInvalidNames(
        string databaseName)
    {
        Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateGeneratedDatabaseName(
                databaseName));
    }

    [Fact]
    public void BuildDatabaseConnectionString_RejectsNonGeneratedNames()
    {
        var target = SqlServerTestEnvironment.ValidateConfiguration(
            "Server=localhost;Integrated Security=True",
            ExpectedInstance);

        Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.BuildDatabaseConnectionString(
                target,
                "DVLD"));
    }

    [Fact]
    public void BuildDatabaseConnectionString_UsesOnlyGeneratedTestDatabase()
    {
        var target = SqlServerTestEnvironment.ValidateConfiguration(
            "Server=localhost;Initial Catalog=master;Integrated Security=True",
            ExpectedInstance);
        var databaseName = SqlServerTestEnvironment.BuildDatabaseName(
            "DVLD_IntegrationTests_");

        var builder = new SqlConnectionStringBuilder(
            SqlServerTestEnvironment.BuildDatabaseConnectionString(
                target,
                databaseName));

        Assert.Equal(databaseName, builder.InitialCatalog);
        Assert.True(builder.IntegratedSecurity);
        Assert.Empty(builder.AttachDBFilename);
    }

    [Fact]
    public void ValidateGeneratedDatabaseName_RejectsNamesOverSqlServerLimit()
    {
        var tooLong =
            $"DVLD_IntegrationTests_{Guid.NewGuid():N}".PadRight(129, 'X');

        Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateGeneratedDatabaseName(
                tooLong));
    }
}
