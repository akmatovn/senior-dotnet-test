using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Settings;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Core.Handlers;

/// <summary>
/// Handles the 'Show more results' callback in the search workflow, retrieving and displaying additional search results
/// to the user when requested.
/// </summary>
/// <remarks>This handler is typically used in conjunction with a Telegram bot's inline keyboard, allowing users
/// to request and browse search results incrementally. It maintains user search progress and modifies the user
/// interface to reflect additional results when available.</remarks>
/// <param name="knowledgeBaseService">The service used to perform article searches within the knowledge base.</param>
/// <param name="slugMapping">The service responsible for generating and managing slugs for articles to map to callback data.</param>
/// <param name="searchState">The service used to store and retrieve the current state of user searches, such as visible result count and search
/// query.</param>
/// <param name="publisher">The service responsible for sending responses and messages to the user through the bot.</param>
/// <param name="logger">The logger instance used for logging diagnostic and operational messages.</param>
/// <param name="options">The options containing configuration settings for the knowledge base, including the search page size.</param>
public class SearchMoreHandler(
    IKnowledgeBaseService knowledgeBaseService,
    ISlugMappingService slugMapping,
    ISearchStateService searchState,
    IBotResponsePublisher publisher,
    ILogger<SearchMoreHandler> logger,
    IOptions<KnowledgeBaseOptions> options) : ICallbackHandler
{
    private readonly int SearchPageSize = options.Value.SearchPageSize;

    ///<inheritdoc/>
    public bool CanHandle(string callbackData) => callbackData == CallbackPrefixes.SearchMore;

    ///<inheritdoc/>
    public async Task HandleAsync(CallbackContext context, string callbackData, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Search more callback received");

            if (!searchState.TryGet(context.ChatId, out var state))
                return;

            var nextVisible = state.VisibleCount + SearchPageSize;
            searchState.Set(context.ChatId, state.Query, nextVisible);

            var all = await knowledgeBaseService.SearchAsync(state.Query, cancellationToken, limit: null);
            var clamped = Math.Min(all.Count, Math.Max(0, nextVisible));
            var visible = all.Take(clamped).ToList();

            var articleButtons = visible
                .Select(a =>
                {
                    var articleHash = slugMapping.GetOrCreateSlug(a.Slug);
                    var data = CallbackPrefixes.ShowArticle + articleHash + CallbackPrefixes.Separator + "search" + CallbackPrefixes.Separator + clamped;
                    return (Text: a.Title, CallbackData: data);
                })
                .ToList();

            var rows = articleButtons
                .Chunk(2)
                .Select(chunk => (IEnumerable<(string Text, string CallbackData)>)chunk)
                .ToList();

            if (all.Count > clamped)
                rows.Add([(UiTexts.MoreButton, CallbackPrefixes.SearchMore)]);
            rows.Add([(UiTexts.MainMenuButton, CallbackPrefixes.MainMenu)]);

            await publisher.PublishAsync(
                context,
                new BotResponse(
                    HtmlText: BotMessages.SearchResultsHeader,
                    InlineKeyboard: rows,
                    EditMessage: context.MessageId.HasValue),
                cancellationToken);
        }
        finally
        {
            await publisher.AnswerCallbackAsync(context.CallbackQueryId, null, cancellationToken);
        }
    }
}
