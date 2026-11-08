using System.Reflection;
using Application.Common.Exceptions;
using Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.IntegrationTests;

public sealed class UnitOfWorkConstraintTranslationTests
{
    [Fact]
    public void TranslateAndRethrowIfKnownConstraint_WhenSqlExceptionIs2601ForUserName_ThrowsDuplicateUsernameException()
    {
        // Arrange
        var sqlEx = CreateSqlException(
            2601,
            "Cannot insert duplicate key row in object 'dbo.Users' with unique index 'IX_Users_UserName'. The duplicate key value is (testuser).");
        var dbUpdateException = new DbUpdateException(
            "An error occurred while saving the entity changes.",
            sqlEx);

        // Act & Assert
        var ex = Assert.Throws<DuplicateUsernameException>(
            () => UnitOfWork.TranslateAndRethrowIfKnownConstraint(dbUpdateException));

        Assert.Equal("Username is already in use.", ex.Message);
        Assert.Same(dbUpdateException, ex.InnerException);
    }

    [Fact]
    public void TranslateAndRethrowIfKnownConstraint_WhenSqlExceptionIs2627ForUserName_ThrowsDuplicateUsernameException()
    {
        // Arrange
        var sqlEx = CreateSqlException(
            2627,
            "Violation of UNIQUE KEY constraint 'IX_Users_UserName'. Cannot insert duplicate key in object 'dbo.Users'.");
        var dbUpdateException = new DbUpdateException(
            "An error occurred while saving the entity changes.",
            sqlEx);

        // Act & Assert
        var ex = Assert.Throws<DuplicateUsernameException>(
            () => UnitOfWork.TranslateAndRethrowIfKnownConstraint(dbUpdateException));

        Assert.Equal("Username is already in use.", ex.Message);
        Assert.Same(dbUpdateException, ex.InnerException);
    }

    [Fact]
    public void TranslateAndRethrowIfKnownConstraint_WhenSqlExceptionIs2601ForPersonId_ThrowsDuplicatePersonUserException()
    {
        // Arrange
        var sqlEx = CreateSqlException(
            2601,
            "Cannot insert duplicate key row in object 'dbo.Users' with unique index 'IX_Users_PersonId'. The duplicate key value is (101).");
        var dbUpdateException = new DbUpdateException(
            "An error occurred while saving the entity changes.",
            sqlEx);

        // Act & Assert
        var ex = Assert.Throws<DuplicatePersonUserException>(
            () => UnitOfWork.TranslateAndRethrowIfKnownConstraint(dbUpdateException));

        Assert.Equal("This person is already associated with a user account.", ex.Message);
        Assert.Same(dbUpdateException, ex.InnerException);
    }

    [Fact]
    public void TranslateAndRethrowIfKnownConstraint_WhenSqlExceptionIs2627ForPersonId_ThrowsDuplicatePersonUserException()
    {
        // Arrange
        var sqlEx = CreateSqlException(
            2627,
            "Violation of UNIQUE KEY constraint 'IX_Users_PersonId'. Cannot insert duplicate key in object 'dbo.Users'.");
        var dbUpdateException = new DbUpdateException(
            "An error occurred while saving the entity changes.",
            sqlEx);

        // Act & Assert
        var ex = Assert.Throws<DuplicatePersonUserException>(
            () => UnitOfWork.TranslateAndRethrowIfKnownConstraint(dbUpdateException));

        Assert.Equal("This person is already associated with a user account.", ex.Message);
        Assert.Same(dbUpdateException, ex.InnerException);
    }

    [Fact]
    public void TranslateAndRethrowIfKnownConstraint_WhenSqlErrorNumberIsNot2601Or2627_DoesNotThrow()
    {
        // Arrange - error number 547 (Foreign Key violation) even if message somehow references index
        var sqlEx = CreateSqlException(
            547,
            "The statement conflicted with the constraint 'IX_Users_UserName'.");
        var dbUpdateException = new DbUpdateException(
            "An error occurred while saving the entity changes.",
            sqlEx);

        // Act & Assert (does not throw custom exception; caller will rethrow original)
        var exception = Record.Exception(
            () => UnitOfWork.TranslateAndRethrowIfKnownConstraint(dbUpdateException));

        Assert.Null(exception);
    }

    [Fact]
    public void TranslateAndRethrowIfKnownConstraint_WhenDifferentUniqueConstraintName_DoesNotThrow()
    {
        // Arrange - error number 2601 but for a different unique index
        var sqlEx = CreateSqlException(
            2601,
            "Cannot insert duplicate key row in object 'dbo.People' with unique index 'IX_People_NationalNo'. The duplicate key value is (N123).");
        var dbUpdateException = new DbUpdateException(
            "An error occurred while saving the entity changes.",
            sqlEx);

        // Act & Assert
        var exception = Record.Exception(
            () => UnitOfWork.TranslateAndRethrowIfKnownConstraint(dbUpdateException));

        Assert.Null(exception);
    }

    [Fact]
    public void TranslateAndRethrowIfKnownConstraint_WhenNonSqlExceptionInnerExceptionContainsIndexName_DoesNotThrow()
    {
        // Arrange - inner exception is generic Exception, NOT SqlException, even if message contains IX_Users_UserName
        var genericInner = new InvalidOperationException(
            "Cannot insert duplicate key row in object 'dbo.Users' with unique index 'IX_Users_UserName'. The duplicate key value is (testuser).");
        var dbUpdateException = new DbUpdateException(
            "An error occurred while saving the entity changes.",
            genericInner);

        // Act & Assert - must NOT translate because provider evidence is missing
        var exception = Record.Exception(
            () => UnitOfWork.TranslateAndRethrowIfKnownConstraint(dbUpdateException));

        Assert.Null(exception);
    }

    [Fact]
    public void TranslateAndRethrowIfKnownConstraint_WhenGenericDbUpdateExceptionMessageContainsIndexName_DoesNotThrow()
    {
        // Arrange - outer DbUpdateException.Message contains index name, but inner exception is null
        var dbUpdateException = new DbUpdateException(
            "Cannot insert duplicate key row in object 'dbo.Users' with unique index 'IX_Users_UserName'.");

        // Act & Assert - must NOT translate because SqlException is missing
        var exception = Record.Exception(
            () => UnitOfWork.TranslateAndRethrowIfKnownConstraint(dbUpdateException));

        Assert.Null(exception);
    }

    [Fact]
    public void TranslateAndRethrowIfKnownConstraint_WhenGenericDbUpdateExceptionWithoutInnerException_DoesNotThrow()
    {
        // Arrange
        var dbUpdateException = new DbUpdateException(
            "Simulated database connection failure or deadlock.");

        // Act & Assert
        var exception = Record.Exception(
            () => UnitOfWork.TranslateAndRethrowIfKnownConstraint(dbUpdateException));

        Assert.Null(exception);
    }

    private static SqlException CreateSqlException(int errorNumber, string errorMessage)
    {
        var errorCollection = (SqlErrorCollection)Activator.CreateInstance(
            typeof(SqlErrorCollection),
            nonPublic: true)!;

        var sqlErrorCtor = typeof(SqlError).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[]
            {
                typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int), typeof(Exception)
            },
            modifiers: null)!;

        var sqlError = sqlErrorCtor.Invoke(new object?[]
        {
            errorNumber, (byte)0, (byte)14, "localhost", errorMessage, "procedure", 1, null
        });

        var addMethod = typeof(SqlErrorCollection).GetMethod(
            "Add",
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(SqlError) },
            modifiers: null)!;

        addMethod.Invoke(errorCollection, new[] { sqlError });

        var createExceptionMethod = typeof(SqlException).GetMethod(
            "CreateException",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(SqlErrorCollection), typeof(string) },
            modifiers: null)!;

        return (SqlException)createExceptionMethod.Invoke(null, new object?[] { errorCollection, "16.0.0" })!;
    }
}
