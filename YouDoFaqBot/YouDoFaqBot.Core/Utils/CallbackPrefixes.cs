namespace YouDoFaqBot.Core.Utils;

/// <summary>
/// Contains constant string prefixes used for identifying and parsing callback data
/// in Telegram inline keyboard interactions. These prefixes help route callback queries
/// to the appropriate handler and encode context for actions such as category selection,
/// article display, rating, and search operations.
/// </summary>
public static class CallbackPrefixes
{
    /// <summary>
    /// Prefix for the main menu callback.
    /// </summary>
    public const string MainMenu = "main_menu";

    /// <summary>
    /// Prefix for ignored callbacks (no action).
    /// </summary>
    public const string Ignore = "ignore";

    /// <summary>
    /// Prefix for category selection callbacks.
    /// </summary>
    public const string Category = "cat:";

    /// <summary>
    /// Prefix for subcategory selection callbacks.
    /// </summary>
    public const string Subcategory = "sub:";

    /// <summary>
    /// Prefix for showing an article.
    /// </summary>
    public const string ShowArticle = "show_art:";

    /// <summary>
    /// Prefix for rating an article.
    /// </summary>
    public const string Rate = "rate:";

    /// <summary>
    /// Separator used to split parts of callback data.
    /// </summary>
    public const string Separator = ":";

    /// <summary>
    /// Prefix for requesting more search results.
    /// </summary>
    public const string SearchMore = "search_more";

    /// <summary>
    /// Prefix for restoring a previous search.
    /// </summary>
    public const string SearchRestore = "search:";

    /// <summary>
    /// Prefix for initiating a search (shows prompt to enter query).
    /// </summary>
    public const string SearchStart = "search_start";
}
