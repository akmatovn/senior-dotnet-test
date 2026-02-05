namespace YouDoFaqBot.Core.Utils;

/// <summary>
/// Common bot message templates and constant texts used across handlers.
/// </summary>
public static class BotMessages
{
    public const string SearchPrompt = "Please enter your search query in the search field.";

    public const string SearchResultsHeader = "<b>Search Results</b>\n\nI found these articles for your query:";

    public const string SearchNotFound = "Sorry, nothing was found for your query.";

    public const string NoSearchState = "No search state.";

    public const string ArticleNotFound = "Article not found";

    public const string ReadMoreSuffix = "... (read more on website)";

    public const string FeedbackThanks = "Thanks for your feedback!";

    public const string WelcomeTemplate = "Welcome! Tap <b>{0}</b> to browse the FAQ.";

    public const string OpeningMainMenu = "Opening main menu...";
    
    public const string MainMenuHeader = "<b>FAQ Categories</b>\n\nChoose a category:";

    public const string SubcategoriesHeader = "<b>Subcategories</b>\n\nChoose a subcategory:";
    
    public const string ArticlesHeader = "<b>Articles</b>\n\nChoose an article:";
}
