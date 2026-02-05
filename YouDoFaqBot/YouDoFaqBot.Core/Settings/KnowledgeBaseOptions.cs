namespace YouDoFaqBot.Core.Settings;

/// <summary>
/// Represents configuration options for the FAQ knowledge base.
/// </summary>
public class KnowledgeBaseOptions
{
    /// <summary>
    /// Gets or sets the file path to the knowledge base JSON file.
    /// </summary>
    public string FilePath { get; set; } = "knowledge_base.json";

    /// <summary>
    /// Gets or sets the number of search results to display per page.
    /// </summary>
    public int SearchPageSize { get; set; } = 6;
}
