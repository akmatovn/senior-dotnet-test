using Microsoft.Extensions.Logging;
using System.Text.Json;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;

namespace YouDoFaqBot.Core.Services;

/// <summary>
/// Provides methods for loading and searching the FAQ knowledge base.
/// Implements thread-safe loading and fast in-memory search.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KnowledgeBaseService"/> class.
/// </remarks>
/// <param name="logger">Optional logger for diagnostics and error reporting.</param>
public class KnowledgeBaseService(ILogger<KnowledgeBaseService>? logger = null) : IKnowledgeBaseService
{
    private KnowledgeBase? _knowledgeBase;
    public Task<List<Category>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        if (_knowledgeBase is null)
            throw new InvalidOperationException("Knowledge base not loaded.");
        var categories = _knowledgeBase.Articles
            .Select(a => a.Category)
            .GroupBy(c => c.Slug)
            .Select(g => g.First())
            .OrderBy(c => c.Title)
            .ToList();
        return Task.FromResult(categories);
    }

    public Task<List<Subcategory>> GetSubcategoriesByCategoryAsync(string categorySlug, CancellationToken cancellationToken)
    {
        if (_knowledgeBase is null)
            throw new InvalidOperationException("Knowledge base not loaded.");
        var subcategories = _knowledgeBase.Articles
            .Where(a => a.Category.Slug == categorySlug && a.Subcategory != null)
            .Select(a => a.Subcategory)
            .GroupBy(sc => sc.Slug)
            .Select(g => g.First())
            .OrderBy(sc => sc.Title)
            .ToList();
        return Task.FromResult(subcategories);
    }

    public Task<List<Article>> GetArticlesBySubcategoryAsync(string subcategorySlug, CancellationToken cancellationToken)
    {
        if (_knowledgeBase is null)
            throw new InvalidOperationException("Knowledge base not loaded.");
        var articles = _knowledgeBase.Articles
            .Where(a => a.Subcategory != null && a.Subcategory.Slug == subcategorySlug)
            .OrderBy(a => a.Title)
            .ToList();
        return Task.FromResult(articles);
    }
    private readonly object _lock = new();
    private readonly ILogger<KnowledgeBaseService>? _logger = logger;

    /// <summary>
    /// Asynchronously loads the knowledge base from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file containing the knowledge base.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown if deserialization fails.</exception>
    public async Task LoadFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
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

            _logger?.LogInformation("Knowledge base loaded. Articles count: {Count}", kb.Articles.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading knowledge base from {FilePath}", filePath);
            throw;
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
    public Task<IReadOnlyList<Article>> SearchAsync(string query, CancellationToken cancellationToken, int? limit = 6)
    {
        try
        {
            if (_knowledgeBase is null)
                throw new InvalidOperationException("Knowledge base not loaded.");
            if (string.IsNullOrWhiteSpace(query))
                return Task.FromResult<IReadOnlyList<Article>>(Array.Empty<Article>());

            var q = query.Trim();
            var resultsQuery = _knowledgeBase.Articles
                .Select(article => new
                {
                    Article = article,
                    Score = (article.Title?.Contains(q, StringComparison.OrdinalIgnoreCase) == true ? 2 : 0) +
                            (article.Content?.Contains(q, StringComparison.OrdinalIgnoreCase) == true ? 1 : 0)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score) // Most relevant first
                .ThenBy(x => x.Article.Title)    // Then alphabetically
                .Select(x => x.Article)
                .GroupBy(a => a.Slug)            // Group by unique slug
                .Select(g => g.First())          // Take only one instance per article
                ;

            var results = (limit is int l ? resultsQuery.Take(l) : resultsQuery)
                .ToList();

            _logger?.LogInformation("Search query: '{Query}', found: {Count}", query, results.Count);

            return Task.FromResult<IReadOnlyList<Article>>(results);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error searching knowledge base for query: {Query}", query);
            throw;
        }
    }

    /// <summary>
    /// Asynchronously retrieves an article by its slug.
    /// </summary>
    /// <param name="slug">The unique slug identifier of the article.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching <see cref="Article"/> if found; otherwise, <c>null</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the knowledge base is not loaded.</exception>
    public Task<Article?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        if (_knowledgeBase is null)
            throw new InvalidOperationException("Knowledge base not loaded.");
        var article = _knowledgeBase.Articles.FirstOrDefault(a => a.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        if (article is not null)
            _logger?.LogInformation("Article found by slug: {Slug}", slug);
        else
            _logger?.LogWarning("Article not found by slug: {Slug}", slug);

        return Task.FromResult(article);
    }
}
