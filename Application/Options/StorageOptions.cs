namespace Application.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Root path for storing uploaded person images.
    /// Can be an absolute path or relative to the application base directory.
    /// </summary>
    public string PersonImagesPath { get; set; } = string.Empty;
}
