using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YouDoFaqBot.Models;

namespace YouDoFaqBot.Services;

public interface IKnowledgeBaseService
{
    Task LoadFromFileAsync(string filePath, CancellationToken cancellationToken);
    Task<IReadOnlyList<Article>> SearchAsync(string query, CancellationToken cancellationToken);
}
