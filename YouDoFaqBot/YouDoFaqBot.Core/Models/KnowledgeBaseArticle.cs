namespace YouDoFaqBot.Core.Models;

/// <summary>
/// Represents an article in the knowledge base, including its identifier, title, content, and category.
/// </summary>
/// <param name="Id">The unique identifier for the article. Cannot be null or empty.</param>
/// <param name="Title">The title of the article. Cannot be null or empty.</param>
/// <param name="Content">The main content of the article. Cannot be null; may be empty if no content is provided.</param>
/// <param name="Category">The category or topic under which the article is grouped. Cannot be null or empty.</param>
public record KnowledgeBaseArticle(string Id, string Title, string Content, string Category);
