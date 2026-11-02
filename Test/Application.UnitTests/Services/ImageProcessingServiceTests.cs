using System.Text;
using Application.Services;
using SkiaSharp;

namespace Application.UnitTests.Services;

public class ImageProcessingServiceTests
{
    private readonly ImageProcessingService _sut = new();

    private static byte[] CreateTestJpeg(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Blue);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    private static byte[] CreateTestPng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Green);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    [Fact]
    public void ProcessPersonPhoto_ValidJpeg_SucceedsAndProducesWebP()
    {
        // Arrange
        var jpegBytes = CreateTestJpeg(200, 200);
        using var stream = new MemoryStream(jpegBytes);

        // Act
        var result = _sut.ProcessPersonPhoto(stream, jpegBytes.Length, "image/jpeg");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("image/webp", result.Value.ContentType);
        Assert.Equal(".webp", result.Value.Extension);
        Assert.Equal(200, result.Value.Width);
        Assert.Equal(200, result.Value.Height);
        Assert.True(result.Value.SizeBytes > 0);
    }

    [Fact]
    public void ProcessPersonPhoto_ValidPng_SucceedsAndProducesWebP()
    {
        // Arrange
        var pngBytes = CreateTestPng(300, 400);
        using var stream = new MemoryStream(pngBytes);

        // Act
        var result = _sut.ProcessPersonPhoto(stream, pngBytes.Length, "image/png");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("image/webp", result.Value.ContentType);
        Assert.Equal(300, result.Value.Width);
        Assert.Equal(400, result.Value.Height);
    }

    [Fact]
    public void ProcessPersonPhoto_LargeDimensions_ResizesPreservingAspectRatio()
    {
        // Arrange: 1200 x 1600 (aspect ratio 3:4)
        var jpegBytes = CreateTestJpeg(1200, 1600);
        using var stream = new MemoryStream(jpegBytes);

        // Act
        var result = _sut.ProcessPersonPhoto(stream, jpegBytes.Length, "image/jpeg");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        // Max constraint is 600x800 -> 1200x1600 scaled by 0.5 becomes 600x800
        Assert.True(result.Value.Width <= ImageProcessingService.TargetMaxWidth);
        Assert.True(result.Value.Height <= ImageProcessingService.TargetMaxHeight);
        Assert.Equal(600, result.Value.Width);
        Assert.Equal(800, result.Value.Height);
    }

    [Fact]
    public void ProcessPersonPhoto_EmptyStream_ReturnsValidationFailure()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var result = _sut.ProcessPersonPhoto(stream, 0);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("empty", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProcessPersonPhoto_OversizedFile_ReturnsValidationFailure()
    {
        // Arrange
        const long size = ImageProcessingService.MaxUploadSizeBytes + 10;
        using var stream = new MemoryStream([1, 2, 3]);

        // Act
        var result = _sut.ProcessPersonPhoto(stream, size);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("exceeds the maximum limit", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProcessPersonPhoto_ExecutableSignatureMZ_ReturnsValidationFailure()
    {
        // Arrange
        byte[] mzData = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF];
        using var stream = new MemoryStream(mzData);

        // Act
        var result = _sut.ProcessPersonPhoto(stream, mzData.Length);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Executables cannot be uploaded", result.Error);
    }

    [Fact]
    public void ProcessPersonPhoto_SvgContent_ReturnsValidationFailure()
    {
        // Arrange
        byte[] svgData = Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'><script>alert(1)</script></svg>");
        using var stream = new MemoryStream(svgData);

        // Act
        var result = _sut.ProcessPersonPhoto(stream, svgData.Length);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("SVG", result.Error);
    }

    [Fact]
    public void ProcessPersonPhoto_HtmlContent_ReturnsValidationFailure()
    {
        // Arrange
        byte[] htmlData = Encoding.UTF8.GetBytes("<!DOCTYPE html><html><body><h1>Fake Image</h1></body></html>");
        using var stream = new MemoryStream(htmlData);

        // Act
        var result = _sut.ProcessPersonPhoto(stream, htmlData.Length);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("HTML", result.Error);
    }

    [Fact]
    public void ProcessPersonPhoto_CorruptImageBytes_ReturnsValidationFailure()
    {
        // Arrange: Starts with JPEG magic but contains random junk
        byte[] junkData = [0xFF, 0xD8, 0xFF, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88];
        using var stream = new MemoryStream(junkData);

        // Act
        var result = _sut.ProcessPersonPhoto(stream, junkData.Length);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
