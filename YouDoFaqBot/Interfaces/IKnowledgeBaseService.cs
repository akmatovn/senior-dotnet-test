using YouDoFaqBot.Models;

namespace YouDoFaqBot.Interfaces;

/// <summary>
/// Provides methods for loading and searching the FAQ knowledge base.
/// </summary>
public interface IKnowledgeBaseService
{
    /// <summary>
    /// Asynchronously loads the knowledge base from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file containing the knowledge base.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LoadFromFileAsync(string filePath, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously searches for articles matching the specified query.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of matching articles.</returns>
    Task<IReadOnlyList<Article>> SearchAsync(string query, CancellationToken cancellationToken);
}
