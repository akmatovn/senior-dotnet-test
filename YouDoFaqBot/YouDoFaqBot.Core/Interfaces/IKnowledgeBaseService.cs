using YouDoFaqBot.Core.Models;

namespace YouDoFaqBot.Core.Interfaces;

/// <summary>
/// Provides methods for loading and searching the FAQ knowledge base.
/// </summary>
public interface IKnowledgeBaseService
{
    Task LoadFromFileAsync(string filePath, CancellationToken cancellationToken);
    Task<IReadOnlyList<Article>> SearchAsync(string query, CancellationToken cancellationToken, int? limit = 6);
    Task<Article?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<List<Category>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<List<Subcategory>> GetSubcategoriesByCategoryAsync(string categorySlug, CancellationToken cancellationToken);
    Task<List<Article>> GetArticlesBySubcategoryAsync(string subcategorySlug, CancellationToken cancellationToken);
}
