using Presentation.Services.Api;
using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media.Imaging;

namespace Presentation.Services;

public sealed class PersonPhotoCache
{
    private readonly ConcurrentDictionary<int, BitmapImage> _images = new();
    private readonly ConcurrentDictionary<int, byte> _missing = new();
    private readonly ConcurrentDictionary<int, Lazy<Task<BitmapImage?>>> _pending = new();
    private readonly ConcurrentDictionary<int, int> _versions = new();

    public async Task<BitmapImage?> GetAsync(
        int personId,
        IPeopleApiClient peopleApiClient,
        CancellationToken cancellationToken = default)
    {
        if (personId <= 0)
            return null;

        ArgumentNullException.ThrowIfNull(peopleApiClient);
        if (_images.TryGetValue(personId, out var cached))
            return cached;
        if (_missing.ContainsKey(personId))
            return null;

        var version = _versions.GetOrAdd(personId, 0);

        var request = _pending.GetOrAdd(
            personId,
            id => new Lazy<Task<BitmapImage?>>(
                () => LoadAsync(id, peopleApiClient, version),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            var image = await request.Value.WaitAsync(cancellationToken);
            if (_versions.GetOrAdd(personId, 0) != version)
            {
                _pending.TryRemove(personId, out _);
                return await GetAsync(personId, peopleApiClient, cancellationToken);
            }
            return image;
        }
        finally
        {
            if (request.IsValueCreated && request.Value.IsCompleted &&
                _pending.TryGetValue(personId, out var current) && ReferenceEquals(current, request))
            {
                _pending.TryRemove(personId, out _);
            }
        }
    }

    public void Invalidate(int personId)
    {
        if (personId > 0)
        {
            _versions.AddOrUpdate(personId, 1, (_, current) => current + 1);
            _images.TryRemove(personId, out _);
            _missing.TryRemove(personId, out _);
            _pending.TryRemove(personId, out _);
        }
    }

    private async Task<BitmapImage?> LoadAsync(int personId, IPeopleApiClient peopleApiClient, int version)
    {
        var result = await peopleApiClient.GetPhotoAsync(personId);
        if (result.IsFailure)
        {
            if (result.StatusCode == System.Net.HttpStatusCode.NotFound &&
                _versions.GetOrAdd(personId, 0) == version)
                _missing[personId] = 0;
            return null;
        }
        if (result.Value is not { Length: > 0 } bytes)
            return null;

        try
        {
            using var stream = new MemoryStream(bytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            if (_versions.GetOrAdd(personId, 0) == version)
                _images[personId] = bitmap;
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
