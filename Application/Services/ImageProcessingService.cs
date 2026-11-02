using System.Text;
using Application.Common.Results;
using Application.Interfaces;
using SkiaSharp;

namespace Application.Services;

public sealed class ImageProcessingService : IImageProcessingService
{
    public const long MaxUploadSizeBytes = 5 * 1024 * 1024; // 5 MB
    public const int MaxDimension = 4096;
    public const long MaxTotalPixels = 16_000_000;
    public const int TargetMaxWidth = 600;
    public const int TargetMaxHeight = 800;
    public const int EncodeQuality = 85;

    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] RiffMagic = "RIFF"u8.ToArray();
    private static readonly byte[] WebpMagic = "WEBP"u8.ToArray();

    public Result<ProcessedImageResult> ProcessPersonPhoto(
        Stream inputStream,
        long length,
        string? declaredContentType = null)
    {
        if (inputStream is null)
        {
            return Result<ProcessedImageResult>.FromValidationFailure(
                "Image data stream is required.");
        }

        if (length <= 0)
        {
            return Result<ProcessedImageResult>.FromValidationFailure(
                "The selected image file is empty.");
        }

        if (length > MaxUploadSizeBytes)
        {
            return Result<ProcessedImageResult>.FromValidationFailure(
                $"Image file size exceeds the maximum limit of {MaxUploadSizeBytes / (1024 * 1024)} MB.");
        }

        // Read stream into memory safely
        byte[] rawBytes;
        if (inputStream is MemoryStream ms && ms.TryGetBuffer(out var segment))
        {
            rawBytes = segment.ToArray();
        }
        else
        {
            using var copyMs = new MemoryStream();
            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }
            inputStream.CopyTo(copyMs);
            rawBytes = copyMs.ToArray();
        }

        if (rawBytes.Length == 0)
        {
            return Result<ProcessedImageResult>.FromValidationFailure(
                "The selected image file is empty.");
        }

        if (rawBytes.Length > MaxUploadSizeBytes)
        {
            return Result<ProcessedImageResult>.FromValidationFailure(
                $"Image file size exceeds the maximum limit of {MaxUploadSizeBytes / (1024 * 1024)} MB.");
        }

        // Magic byte & format signature validation
        var formatValidation = ValidateMagicBytes(rawBytes);
        if (formatValidation.IsFailure)
        {
            return Result<ProcessedImageResult>.FromValidationFailure(
                formatValidation.Error);
        }

        // Create Skia codec from data to inspect headers without decoding all pixels
        using var skData = SKData.CreateCopy(rawBytes);
        using var codec = SKCodec.Create(skData);
        if (codec is null)
        {
            return Result<ProcessedImageResult>.FromValidationFailure(
                "The file is not a valid or supported image.");
        }

        var format = codec.EncodedFormat;
        if (format != SKEncodedImageFormat.Jpeg &&
            format != SKEncodedImageFormat.Png &&
            format != SKEncodedImageFormat.Webp)
        {
            return Result<ProcessedImageResult>.FromValidationFailure(
                "Unsupported image format. Only JPEG, PNG, and WebP images are allowed.");
        }

        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0)
        {
            return Result<ProcessedImageResult>.FromValidationFailure(
                "The image has invalid dimensions.");
        }

        if (info.Width > MaxDimension || info.Height > MaxDimension)
        {
            return Result<ProcessedImageResult>.FromValidationFailure(
                $"Image dimensions exceed the allowed maximum of {MaxDimension}x{MaxDimension} pixels.");
        }

        long totalPixels = (long)info.Width * info.Height;
        if (totalPixels > MaxTotalPixels)
        {
            return Result<ProcessedImageResult>.FromValidationFailure(
                "Image pixel count exceeds the safety limit (possible decompression bomb).");
        }

        // Decode full bitmap
        using var originalBitmap = SKBitmap.Decode(codec);
        if (originalBitmap is null)
        {
            return Result<ProcessedImageResult>.FromValidationFailure(
                "Failed to decode image data. The file may be corrupt.");
        }

        // Normalize orientation based on EXIF origin
        using var orientedBitmap = ApplyExifOrientation(originalBitmap, codec.EncodedOrigin);

        // Resize while preserving aspect ratio
        using var finalBitmap = ResizePreservingAspect(
            orientedBitmap,
            TargetMaxWidth,
            TargetMaxHeight);

        // Re-encode to clean, standardized WebP (or fallback to JPEG if WebP encoder unavailable)
        using var image = SKImage.FromBitmap(finalBitmap);
        var encodedData = image.Encode(SKEncodedImageFormat.Webp, EncodeQuality);
        string contentType = "image/webp";
        string extension = ".webp";

        if (encodedData is null)
        {
            encodedData = image.Encode(SKEncodedImageFormat.Jpeg, EncodeQuality);
            contentType = "image/jpeg";
            extension = ".jpg";
        }

        if (encodedData is null)
        {
            return Result<ProcessedImageResult>.FromFailure(
                "Failed to re-encode the image to a standardized format.");
        }

        var outputBytes = encodedData.ToArray();
        encodedData.Dispose();

        var outputStream = new MemoryStream(outputBytes, writable: false);

        return Result<ProcessedImageResult>.Success(
            new ProcessedImageResult(
                ImageStream: outputStream,
                ContentType: contentType,
                Extension: extension,
                Width: finalBitmap.Width,
                Height: finalBitmap.Height,
                SizeBytes: outputBytes.Length));
    }

    private static Result ValidateMagicBytes(byte[] bytes)
    {
        if (bytes.Length < 12)
        {
            return Result.ValidationFailure("File is too small to be a valid image.");
        }

        // Check for common malicious or non-image signatures
        if (bytes[0] == 0x4D && bytes[1] == 0x5A) // MZ (Windows PE executable)
        {
            return Result.ValidationFailure("Executables cannot be uploaded as images.");
        }

        if (bytes[0] == 0x7F && bytes[1] == 0x45 && bytes[2] == 0x4C && bytes[3] == 0x46) // ELF
        {
            return Result.ValidationFailure("Executables cannot be uploaded as images.");
        }

        // Check for SVG, XML, or HTML tags in the preamble
        int preambleLength = Math.Min(bytes.Length, 512);
        string preambleText = Encoding.ASCII.GetString(bytes, 0, preambleLength).ToLowerInvariant();

        if (preambleText.Contains("<svg") ||
            preambleText.Contains("<?xml") ||
            preambleText.Contains("<!doctype html") ||
            preambleText.Contains("<html") ||
            preambleText.Contains("<script"))
        {
            return Result.ValidationFailure("SVG, XML, and HTML files are not accepted as person photos.");
        }

        if (preambleText.StartsWith("gif87a") || preambleText.StartsWith("gif89a"))
        {
            return Result.ValidationFailure("GIF images are not accepted. Only JPEG, PNG, and WebP are allowed.");
        }

        if (bytes[0] == 0x42 && bytes[1] == 0x4D) // BM (BMP)
        {
            return Result.ValidationFailure("BMP images are not accepted. Only JPEG, PNG, and WebP are allowed.");
        }

        // Check allowed image signatures
        // JPEG: FF D8 FF
        bool isJpeg = bytes[0] == JpegMagic[0] &&
                      bytes[1] == JpegMagic[1] &&
                      bytes[2] == JpegMagic[2];

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        bool isPng = bytes.Length >= 8 &&
                     bytes[0] == PngMagic[0] &&
                     bytes[1] == PngMagic[1] &&
                     bytes[2] == PngMagic[2] &&
                     bytes[3] == PngMagic[3] &&
                     bytes[4] == PngMagic[4] &&
                     bytes[5] == PngMagic[5] &&
                     bytes[6] == PngMagic[6] &&
                     bytes[7] == PngMagic[7];

        // WebP: 'RIFF' .... 'WEBP'
        bool isWebp = bytes.Length >= 12 &&
                      bytes[0] == RiffMagic[0] &&
                      bytes[1] == RiffMagic[1] &&
                      bytes[2] == RiffMagic[2] &&
                      bytes[3] == RiffMagic[3] &&
                      bytes[8] == WebpMagic[0] &&
                      bytes[9] == WebpMagic[1] &&
                      bytes[10] == WebpMagic[2] &&
                      bytes[11] == WebpMagic[3];

        if (!isJpeg && !isPng && !isWebp)
        {
            return Result.ValidationFailure(
                "Invalid image file signature. Only JPEG, PNG, and WebP formats are supported.");
        }

        return Result.Success();
    }

    private static SKBitmap ApplyExifOrientation(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        switch (origin)
        {
            case SKEncodedOrigin.BottomRight: // 180 degrees
                return RotateBitmap(bitmap, 180);

            case SKEncodedOrigin.RightTop: // 90 CW
                return RotateBitmap(bitmap, 90);

            case SKEncodedOrigin.LeftBottom: // 270 CW (90 CCW)
                return RotateBitmap(bitmap, 270);

            case SKEncodedOrigin.TopLeft:
            default:
                // No rotation needed, return a clone to allow clean disposal
                return bitmap.Copy();
        }
    }

    private static SKBitmap RotateBitmap(SKBitmap bitmap, float degrees)
    {
        bool is90or270 = Math.Abs(degrees - 90) < 0.1f || Math.Abs(degrees - 270) < 0.1f;
        int newWidth = is90or270 ? bitmap.Height : bitmap.Width;
        int newHeight = is90or270 ? bitmap.Width : bitmap.Height;

        var rotated = new SKBitmap(newWidth, newHeight);
        using var canvas = new SKCanvas(rotated);
        canvas.Clear(SKColors.Transparent);

        canvas.Translate(newWidth / 2f, newHeight / 2f);
        canvas.RotateDegrees(degrees);
        canvas.Translate(-bitmap.Width / 2f, -bitmap.Height / 2f);

        using var img = SKImage.FromBitmap(bitmap);
        canvas.DrawImage(img, 0, 0, SKSamplingOptions.Default);
        return rotated;
    }

    private static SKBitmap ResizePreservingAspect(
        SKBitmap bitmap,
        int maxWidth,
        int maxHeight)
    {
        if (bitmap.Width <= maxWidth && bitmap.Height <= maxHeight)
        {
            return bitmap.Copy();
        }

        float scale = Math.Min((float)maxWidth / bitmap.Width, (float)maxHeight / bitmap.Height);
        int targetWidth = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
        int targetHeight = Math.Max(1, (int)Math.Round(bitmap.Height * scale));

        var resizedInfo = new SKImageInfo(targetWidth, targetHeight, bitmap.ColorType, bitmap.AlphaType);
        var resized = new SKBitmap(resizedInfo);

        if (!bitmap.ScalePixels(resized, SKSamplingOptions.Default))
        {
            // Fallback to copy if scaling fails
            return bitmap.Copy();
        }

        return resized;
    }
}
