using Application.Options;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Application.UnitTests.Services;

public class PhysicalFileStorageServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly PhysicalFileStorageService _sut;

    public PhysicalFileStorageServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "DVLD_StorageTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        var options = Microsoft.Extensions.Options.Options.Create(new StorageOptions
        {
            PersonImagesPath = _tempDirectory
        });

        var loggerMock = new Mock<ILogger<PhysicalFileStorageService>>();
        _sut = new PhysicalFileStorageService(options, loggerMock.Object);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup in tests
        }
    }

    [Fact]
    public async Task SaveAsync_ValidStream_SavesFileAndReturnsKey()
    {
        // Arrange
        byte[] data = [1, 2, 3, 4, 5];
        using var stream = new MemoryStream(data);

        // Act
        var key = await _sut.SaveAsync(stream, ".webp", "people");

        // Assert
        Assert.NotNull(key);
        Assert.StartsWith("people/", key);
        Assert.EndsWith(".webp", key);

        var exists = await _sut.ExistsAsync(key);
        Assert.True(exists);

        await using var retrievedStream = await _sut.GetAsync(key);
        Assert.NotNull(retrievedStream);
        using var memoryStream = new MemoryStream();
        await retrievedStream.CopyToAsync(memoryStream);
        Assert.Equal(data, memoryStream.ToArray());
    }

    [Fact]
    public async Task DeleteAsync_ExistingFile_DeletesFileAndReturnsTrue()
    {
        // Arrange
        byte[] data = [10, 20, 30];
        using var stream = new MemoryStream(data);
        var key = await _sut.SaveAsync(stream, ".webp", "people");

        // Act
        var result = await _sut.DeleteAsync(key);

        // Assert
        Assert.True(result);
        var exists = await _sut.ExistsAsync(key);
        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingFile_ReturnsTrueIdempotently()
    {
        // Act
        var result = await _sut.DeleteAsync("people/nonexistent.webp");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetAsync_NonExistingFile_ReturnsNull()
    {
        // Act
        var stream = await _sut.GetAsync("people/doesnotexist.webp");

        // Assert
        Assert.Null(stream);
    }

    [Theory]
    [InlineData("../evil.webp")]
    [InlineData("../../windows/system32/cmd.exe")]
    [InlineData("people/../../secret.txt")]
    [InlineData("C:/secret.txt")]
    public async Task GetAsync_PathTraversalKey_ReturnsNullOrThrows(string maliciousKey)
    {
        // Act
        var stream = await _sut.GetAsync(maliciousKey);

        // Assert
        Assert.Null(stream);
    }

    [Theory]
    [InlineData("../evil.webp")]
    [InlineData("../../windows/system32/cmd.exe")]
    [InlineData("people/../../secret.txt")]
    [InlineData("C:/secret.txt")]
    public async Task DeleteAsync_PathTraversalKey_ReturnsFalse(string maliciousKey)
    {
        // Act
        var result = await _sut.DeleteAsync(maliciousKey);

        // Assert
        Assert.False(result);
    }
}
