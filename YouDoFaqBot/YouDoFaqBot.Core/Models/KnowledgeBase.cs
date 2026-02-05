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

/// <summary>
/// Represents a subcategory with a unique slug, a display title, and an optional description.
/// </summary>
/// <param name="Slug">The unique, URL-friendly identifier for the subcategory.</param>
/// <param name="Title">The display name of the subcategory.</param>
/// <param name="Description">An optional description providing additional details about the subcategory, or null if no description is provided.</param>
public record Subcategory(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string? Description
);

/// <summary>
/// Represents a published article, including its title, content, unique slug, category, and subcategory.
/// </summary>
/// <remarks>Each property is mapped to a corresponding JSON field name for serialization. This record is
/// typically used for storing and transferring article data between application layers or APIs.</remarks>
/// <param name="Title">The title of the article. Cannot be null.</param>
/// <param name="Content">The full textual content of the article. Cannot be null.</param>
/// <param name="Slug">The unique, URL-friendly identifier for the article. Cannot be null or empty; used to reference the article in links
/// and APIs.</param>
/// <param name="Category">The primary category to which the article belongs.</param>
/// <param name="Subcategory">The subcategory within the primary category for additional classification.</param>
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
