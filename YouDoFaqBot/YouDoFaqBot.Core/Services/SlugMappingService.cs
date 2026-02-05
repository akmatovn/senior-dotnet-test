using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using YouDoFaqBot.Core.Interfaces;

namespace YouDoFaqBot.Core.Services;

/// <summary>
/// Provides functionality to generate and retrieve unique slug identifiers for data strings, supporting bidirectional
/// mapping between data and slugs.
/// </summary>
/// <remarks>This service allows efficient creation of deterministic, collision-managed slugs for arbitrary input
/// data, ensuring each unique data string maps to a distinct slug. It supports retrieval of the original data given a
/// slug. The implementation is thread-safe and suitable for concurrent scenarios. Slugs are generated using a truncated
/// SHA-256 hash to ensure uniqueness and reduce collisions; in the rare event of a collision, a numeric suffix is
/// appended. All mappings are maintained in memory for fast lookup and are not persisted across application
/// restarts.</remarks>
public class SlugMappingService : ISlugMappingService
{
    private readonly ConcurrentDictionary<string, string> _dataToSlug = new();
    private readonly ConcurrentDictionary<string, string> _slugToData = new();

    ///<inheritdoc/>
    public string GetOrCreateSlug(string data)
    {
        if (_dataToSlug.TryGetValue(data, out var slug))
            return slug;
        slug = ComputeSlug(data);
        if (_slugToData.ContainsKey(slug))
        {
            // Collision: add a suffix
            var i = 1;
            var baseSlug = slug;
            while (_slugToData.ContainsKey(slug = baseSlug + i))
                i++;
        }
        _dataToSlug[data] = slug;
        _slugToData[slug] = data;
        return slug;
    }

    ///<inheritdoc/>
    public string? GetDataBySlug(string slug)
        => _slugToData.TryGetValue(slug, out var data) ? data : null;

    private static string ComputeSlug(string data)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash)[..16]; // 16 hex chars = 8 bytes
    }
}
