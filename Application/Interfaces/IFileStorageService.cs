namespace Application.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Saves the provided file stream under an unpredictable, uniquely generated storage key.
    /// </summary>
    /// <param name="contentStream">Stream containing file bytes to save.</param>
    /// <param name="fileExtension">File extension including or excluding leading dot (e.g. ".webp").</param>
    /// <param name="subDirectory">Logical subfolder name (e.g. "people").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A normalized relative storage key (e.g. "people/a1b2c3d4...webp").</returns>
    Task<string> SaveAsync(
        Stream contentStream,
        string fileExtension,
        string subDirectory = "people",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a file stream for the given relative storage key. Returns null if not found.
    /// </summary>
    Task<Stream?> GetAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Safely deletes the file associated with the given relative storage key.
    /// Does not throw if the file does not exist.
    /// </summary>
    /// <returns>True if deleted or already absent; false if a deletion error occurred.</returns>
    Task<bool> DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a file exists for the given relative storage key.
    /// </summary>
    Task<bool> ExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}
