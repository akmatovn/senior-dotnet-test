using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace YouDoFaqBot.Models;

public record Article(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("slug")] string Slug
);

public record KnowledgeBase(
    [property: JsonPropertyName("articles")] List<Article> Articles
);
