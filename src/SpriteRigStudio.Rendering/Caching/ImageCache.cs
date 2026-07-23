using System.Collections.Concurrent;
using SpriteRigStudio.Rendering.Abstractions;

namespace SpriteRigStudio.Rendering.Caching;

/// <summary>
/// Simple dictionary-based cache for decoded images.
/// Thread-safe for concurrent read/write access.
/// </summary>
public class ImageCache
{
    private readonly ConcurrentDictionary<string, DecodedImageInfo> _cache = new(StringComparer.OrdinalIgnoreCase);

    private readonly IImageDecoder _decoder;

    /// <summary>Maximum number of images to cache. 0 = unlimited.</summary>
    public int MaxCapacity { get; }

    /// <summary>Current number of cached images.</summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageCache"/> class.
    /// </summary>
    /// <param name="decoder">The image decoder to use for loading images.</param>
    /// <param name="maxCapacity">Maximum cache capacity. 0 or negative = unlimited.</param>
    public ImageCache(IImageDecoder decoder, int maxCapacity = 0)
    {
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        MaxCapacity = maxCapacity > 0 ? maxCapacity : 0;
    }

    /// <summary>
    /// Gets a decoded image from the cache, or decodes and caches it if not present.
    /// </summary>
    /// <param name="path">File path to the image.</param>
    /// <returns>The decoded image info, or null if decoding failed.</returns>
    public async Task<DecodedImageInfo?> GetOrDecodeAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (_cache.TryGetValue(path, out var cached))
            return cached;

        var result = await _decoder.DecodeAsync(path);
        if (result.IsFailure || result.Value is null)
            return null;

        var decoded = result.Value;

        // Enforce capacity limit by removing oldest entry if needed
        if (MaxCapacity > 0 && _cache.Count >= MaxCapacity)
        {
            var keyToRemove = _cache.Keys.FirstOrDefault();
            if (keyToRemove is not null)
                _cache.TryRemove(keyToRemove, out _);
        }

        _cache[path] = decoded;
        return decoded;
    }

    /// <summary>
    /// Tries to get a cached image without decoding.
    /// </summary>
    /// <param name="path">File path of the image.</param>
    /// <param name="image">The cached decoded image info, if found.</param>
    /// <returns>True if the image was found in the cache.</returns>
    public bool TryGet(string path, out DecodedImageInfo? image)
    {
        return _cache.TryGetValue(path, out image);
    }

    /// <summary>
    /// Explicitly caches a decoded image.
    /// </summary>
    /// <param name="path">Key/path to associate with the image.</param>
    /// <param name="image">The decoded image info to cache.</param>
    public void Cache(string path, DecodedImageInfo image)
    {
        if (string.IsNullOrEmpty(path))
            return;

        _cache[path] = image;
    }

    /// <summary>
    /// Removes a specific image from the cache.
    /// </summary>
    /// <param name="path">File path of the image to remove.</param>
    /// <returns>True if the image was removed.</returns>
    public bool Remove(string path)
    {
        return _cache.TryRemove(path, out _);
    }

    /// <summary>
    /// Clears all cached images.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Checks whether a specific image is cached.
    /// </summary>
    public bool Contains(string path)
    {
        return _cache.ContainsKey(path);
    }
}
