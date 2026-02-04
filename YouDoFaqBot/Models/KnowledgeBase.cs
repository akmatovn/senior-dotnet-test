using System.Text.Json.Serialization;

namespace YouDoFaqBot.Models;

/// <summary>
/// Represents a knowledge base article with title, content, and slug.
/// </summary>
/// <param name="Title">The title of the article.</param>
/// <param name="Content">The main content of the article.</param>
/// <param name="Slug">A unique slug identifier for the article.</param>
public record Article(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("slug")] string Slug
);

/// <summary>
/// Represents the root knowledge base containing a list of articles.
/// </summary>
/// <param name="Articles">A list of articles available in the knowledge base.</param>
public record KnowledgeBase(
    [property: JsonPropertyName("articles")] List<Article> Articles
);
