using System.Text.Json.Serialization;

namespace YouDoFaqBot.Core.Models;

/// <summary>
/// Represents a knowledge base article with title, content, and slug.
/// </summary>
/// <param name="Title">The title of the article.</param>
/// <param name="Content">The main content of the article.</param>
/// <param name="Slug">A unique slug identifier for the article.</param>
public record Category(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string? Description
);

public record Subcategory(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string? Description
);

public record Article(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("category")] Category Category,
    [property: JsonPropertyName("subcategory")] Subcategory Subcategory
);

/// <summary>
/// Represents the root knowledge base containing a list of articles.
/// </summary>
/// <param name="Articles">A list of articles available in the knowledge base.</param>
public record KnowledgeBase(
    [property: JsonPropertyName("articles")] List<Article> Articles
);
