using System.Text.Json;
using YouDoFaqBot.Interfaces;
using YouDoFaqBot.Models;

namespace YouDoFaqBot.Services;

/// <summary>
/// Service for loading and searching the FAQ knowledge base.
/// Implements thread-safe loading and fast in-memory search.
/// </summary>
public class KnowledgeBaseService : IKnowledgeBaseService
{
    private KnowledgeBase? _knowledgeBase;
    private readonly object _lock = new();

    /// <summary>
    /// Asynchronously loads the knowledge base from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file containing the knowledge base.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown if deserialization fails.</exception>
    public async Task LoadFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Knowledge base file not found: {filePath}");

        await using var stream = File.OpenRead(filePath);
        var kb = await JsonSerializer.DeserializeAsync<KnowledgeBase>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }, cancellationToken).ConfigureAwait(false);

        if (kb is null)
            throw new InvalidOperationException("Failed to deserialize knowledge base.");

        lock (_lock)
        {
            _knowledgeBase = kb;
        }
    }

    /// <summary>
    /// Asynchronously searches for articles matching the specified query.
    /// Performs case-insensitive search in both title and content.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of matching articles, ordered by relevance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the knowledge base is not loaded.</exception>
    public Task<IReadOnlyList<Article>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (_knowledgeBase is null)
            throw new InvalidOperationException("Knowledge base not loaded.");
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult<IReadOnlyList<Article>>(Array.Empty<Article>());

        var q = query.Trim().ToLowerInvariant();
        var results = _knowledgeBase.Articles
            .Select(article => new
            {
                Article = article,
                Score = (article.Title?.ToLowerInvariant().Contains(q, StringComparison.InvariantCultureIgnoreCase) == true ? 2 : 0) +
                        (article.Content?.ToLowerInvariant().Contains(q, StringComparison.InvariantCultureIgnoreCase) == true ? 1 : 0)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Article.Title)
            .Take(3)
            .Select(x => x.Article)
            .ToList();
        return Task.FromResult<IReadOnlyList<Article>>(results);
    }
}
