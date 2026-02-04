using System.Text.Json;
using YouDoFaqBot.Models;

namespace YouDoFaqBot.Services;

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private KnowledgeBase? _knowledgeBase;
    private readonly object _lock = new();

    public async Task LoadFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Knowledge base file not found: {filePath}");

        await using var stream = File.OpenRead(filePath);
        var kb = await JsonSerializer.DeserializeAsync<KnowledgeBase>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }, cancellationToken);
        if (kb is null)
            throw new InvalidOperationException("Failed to deserialize knowledge base.");
        lock (_lock)
        {
            _knowledgeBase = kb;
        }
    }

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
                Score = (article.Title?.ToLowerInvariant().Contains(q) == true ? 2 : 0) +
                        (article.Content?.ToLowerInvariant().Contains(q) == true ? 1 : 0)
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
