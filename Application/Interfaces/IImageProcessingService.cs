using Application.Common.Results;

namespace Application.Interfaces;

public sealed record ProcessedImageResult(
    Stream ImageStream,
    string ContentType,
    string Extension,
    int Width,
    int Height,
    long SizeBytes);

public interface IImageProcessingService
{
    /// <summary>
    /// Validates, decodes, normalizes orientation, resizes, and encodes a person photograph.
    /// Rejects non-images, executables, scripts, SVGs, GIFs, BMPs, corrupted images,
    /// oversized files (> 5MB), and decompression bombs.
    /// </summary>
    Result<ProcessedImageResult> ProcessPersonPhoto(
        Stream inputStream,
        long length,
        string? declaredContentType = null);
}
