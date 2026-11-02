using System.Security.Cryptography;
using Application.Interfaces;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public sealed class PhysicalFileStorageService : IFileStorageService
{
    private readonly string _storageRoot;
    private readonly ILogger<PhysicalFileStorageService> _logger;

    public PhysicalFileStorageService(
        IOptions<StorageOptions> options,
        ILogger<PhysicalFileStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var configuredPath = options?.Value?.PersonImagesPath;
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            // Default storage directory outside wwwroot / deployment binaries
            _storageRoot = Path.GetFullPath(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DVLD",
                    "Storage"));
        }
        else if (Path.IsPathRooted(configuredPath))
        {
            _storageRoot = Path.GetFullPath(configuredPath);
        }
        else
        {
            _storageRoot = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, configuredPath));
        }

        try
        {
            Directory.CreateDirectory(_storageRoot);
            _logger.LogInformation("PhysicalFileStorageService initialized at storage root: {StorageRoot}", _storageRoot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create or access storage directory: {StorageRoot}", _storageRoot);
        }
    }

    public async Task<string> SaveAsync(
        Stream contentStream,
        string fileExtension,
        string subDirectory = "people",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contentStream);

        var ext = NormalizeExtension(fileExtension);
        var subDir = SanitizeSubdirectory(subDirectory);

        // Generate unpredictable unique key
        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var relativeKey = string.IsNullOrEmpty(subDir)
            ? uniqueName
            : $"{subDir}/{uniqueName}";

        var targetFullPath = ResolveFullPath(relativeKey);
        var targetDir = Path.GetDirectoryName(targetFullPath);
        if (!string.IsNullOrEmpty(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        if (contentStream.CanSeek)
        {
            contentStream.Position = 0;
        }

        await using (var fileStream = new FileStream(
            targetFullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true))
        {
            await contentStream.CopyToAsync(fileStream, cancellationToken);
        }

        return relativeKey;
    }

    public Task<Stream?> GetAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return Task.FromResult<Stream?>(null);
        }

        try
        {
            var fullPath = ResolveFullPath(storageKey);
            if (!File.Exists(fullPath))
            {
                return Task.FromResult<Stream?>(null);
            }

            Stream stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);

            return Task.FromResult<Stream?>(stream);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid storage key attempted: {StorageKey}", storageKey);
            return Task.FromResult<Stream?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file for storage key: {StorageKey}", storageKey);
            return Task.FromResult<Stream?>(null);
        }
    }

    public Task<bool> DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return Task.FromResult(true);
        }

        try
        {
            var fullPath = ResolveFullPath(storageKey);
            if (!File.Exists(fullPath))
            {
                return Task.FromResult(true); // Safe idempotent handling
            }

            File.Delete(fullPath);
            return Task.FromResult(true);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid storage key for deletion: {StorageKey}", storageKey);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file for storage key: {StorageKey}", storageKey);
            return Task.FromResult(false);
        }
    }

    public Task<bool> ExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return Task.FromResult(false);
        }

        try
        {
            var fullPath = ResolveFullPath(storageKey);
            return Task.FromResult(File.Exists(fullPath));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private string ResolveFullPath(string relativeKey)
    {
        if (string.IsNullOrWhiteSpace(relativeKey))
        {
            throw new ArgumentException("Storage key cannot be empty.", nameof(relativeKey));
        }

        // Prevent path traversal sequences
        if (relativeKey.Contains("..") || relativeKey.Contains(':'))
        {
            throw new ArgumentException("Path traversal or volume separators are not permitted.", nameof(relativeKey));
        }

        // Normalize separators
        var normalizedKey = relativeKey.Replace('/', Path.DirectorySeparatorChar)
                                       .Replace('\\', Path.DirectorySeparatorChar)
                                       .TrimStart(Path.DirectorySeparatorChar);

        var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, normalizedKey));

        // Ensure full path resides inside storage root
        if (!fullPath.StartsWith(_storageRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Path traversal outside storage root is not permitted.", nameof(relativeKey));
        }

        return fullPath;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return ".webp";
        }

        var ext = extension.Trim().ToLowerInvariant();
        return ext.StartsWith('.') ? ext : $".{ext}";
    }

    private static string SanitizeSubdirectory(string subDirectory)
    {
        if (string.IsNullOrWhiteSpace(subDirectory))
        {
            return string.Empty;
        }

        var cleaned = subDirectory.Replace('\\', '/').Trim('/', ' ');
        return cleaned.Contains("..") ? "people" : cleaned;
    }
}
