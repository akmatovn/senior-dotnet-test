using YouDoFaqBot.Core.Models;

namespace YouDoFaqBot.Core.Interfaces;

/// <summary>
/// Provides methods for loading and searching the FAQ knowledge base.
/// </summary>
public interface IKnowledgeBaseService
{
    /// <summary>
    /// Loads the knowledge base from a JSON file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the JSON file containing the knowledge base data.</param>
    /// <param name="cancellationToken">A cancellation token for graceful shutdown.</param>
    /// <returns>A task representing the asynchronous load operation.</returns>
    Task LoadFromFileAsync(string filePath, CancellationToken cancellationToken);

    /// <summary>
    /// Searches the knowledge base for articles matching the specified query.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="cancellationToken">A cancellation token for graceful shutdown.</param>
    /// <param name="limit">The maximum number of results to return. If null, returns all matches.</param>
    /// <returns>A task that returns a read-only list of matching articles.</returns>
    Task<IReadOnlyList<Article>> SearchAsync(string query, CancellationToken cancellationToken, int? limit = null);

    /// <summary>
    /// Retrieves an article by its unique slug identifier.
    /// </summary>
    /// <param name="slug">The unique slug of the article.</param>
    /// <param name="cancellationToken">A cancellation token for graceful shutdown.</param>
    /// <returns>A task that returns the article if found; otherwise, null.</returns>
    Task<Article?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the list of all categories in the knowledge base.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token for graceful shutdown.</param>
    /// <returns>A task that returns a list of categories.</returns>
    Task<List<Category>> GetCategoriesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the list of subcategories for a given category.
    /// </summary>
    /// <param name="categorySlug">The slug of the category.</param>
    /// <param name="cancellationToken">A cancellation token for graceful shutdown.</param>
    /// <returns>A task that returns a list of subcategories.</returns>
    Task<List<Subcategory>> GetSubcategoriesByCategoryAsync(string categorySlug, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the list of articles for a given subcategory.
    /// </summary>
    /// <param name="subcategorySlug">The slug of the subcategory.</param>
    /// <param name="cancellationToken">A cancellation token for graceful shutdown.</param>
    /// <returns>A task that returns a list of articles.</returns>
    Task<List<Article>> GetArticlesBySubcategoryAsync(string subcategorySlug, CancellationToken cancellationToken);
}
