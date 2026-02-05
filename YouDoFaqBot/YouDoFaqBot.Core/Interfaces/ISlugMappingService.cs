namespace YouDoFaqBot.Core.Interfaces;

/// <summary>
/// Provides methods for generating and resolving slugs for data entities.
/// Used to map between original data and their unique slug representations for use in callback data or URLs.
/// </summary>
public interface ISlugMappingService
{
    /// <summary>
    /// Gets an existing slug for the specified data, or creates a new one if it does not exist.
    /// </summary>
    /// <param name="data">The original data string to generate or retrieve a slug for.</param>
    /// <returns>A unique slug string representing the data.</returns>
    string GetOrCreateSlug(string data);

    /// <summary>
    /// Retrieves the original data associated with the specified slug.
    /// </summary>
    /// <param name="slug">The slug to resolve back to its original data.</param>
    /// <returns>The original data string if found; otherwise, <c>null</c>.</returns>
    string? GetDataBySlug(string slug);
}
