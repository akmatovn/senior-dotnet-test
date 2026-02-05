using Microsoft.Extensions.Logging;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Core.Handlers;

/// <summary>
/// Handles Telegram callback queries to restore and display previously hidden search results within a chat session.
/// </summary>
/// <remarks>This handler is intended for use within a Telegram bot's callback handling pipeline. It restores the
/// set of search results visible to the user based on a prior search operation, typically in response to a "show more
/// results" user action. The handler ensures continuity of search context and integrates with the user interface and
/// state tracking components.</remarks>
/// <param name="knowledgeBaseService">The service used to execute knowledge base search queries.</param>
/// <param name="slugMapping">The service that provides unique slugs for articles to be used in callback data.</param>
/// <param name="searchState">The service responsible for tracking and persisting the user's current search state.</param>
/// <param name="publisher">The service used to send responses and answer callback queries to the user.</param>
/// <param name="logger">The logger used for diagnostic and operational logging of handler activities.</param>
public class SearchRestoreHandler(
    IKnowledgeBaseService knowledgeBaseService,
    ISlugMappingService slugMapping,
    ISearchStateService searchState,
    IBotResponsePublisher publisher,
    ILogger<SearchRestoreHandler> logger) : ICallbackHandler
{
    ///<inheritdoc/>
    public bool CanHandle(string callbackData) => callbackData.StartsWith(CallbackPrefixes.SearchRestore);

    ///<inheritdoc/>
    public async Task HandleAsync(CallbackContext context, string callbackData, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Handling SearchRestore callback: {CallbackData} (chat {ChatId})", callbackData, context.ChatId);

            var visibleStr = callbackData.Substring(CallbackPrefixes.SearchRestore.Length);
            if (!int.TryParse(visibleStr, out var requestedVisible) || requestedVisible <= 0)
            {
                logger.LogWarning("Invalid SearchRestore payload: {VisibleStr} (chat {ChatId})", visibleStr, context.ChatId);
                await publisher.AnswerCallbackAsync(context.CallbackQueryId, null, cancellationToken);
                return;
            }

            if (!searchState.TryGet(context.ChatId, out var state))
            {
                // no state -> go to main menu
                logger.LogInformation("No search state found for chat {ChatId}", context.ChatId);
                await publisher.PublishAsync(context, new BotResponse("No search state."), cancellationToken);
                return;
            }

            // If requestedVisible > stored VisibleCount, update stored
            if (requestedVisible > state.VisibleCount)
            {
                logger.LogInformation("Increasing visible count for chat {ChatId} from {Old} to {New}", context.ChatId, state.VisibleCount, requestedVisible);
                searchState.Set(context.ChatId, state.Query, requestedVisible);
            }

            var all = await knowledgeBaseService.SearchAsync(state.Query, cancellationToken, limit: null);
            logger.LogInformation("Search performed for chat {ChatId}: query='{Query}', totalResults={Total}", context.ChatId, state.Query, all.Count);
            var clamped = Math.Min(all.Count, Math.Max(0, requestedVisible));
            var visible = all.Take(clamped).ToList();
            logger.LogDebug("Preparing {Visible} visible results (requested {Requested}) for chat {ChatId}", visible.Count, requestedVisible, context.ChatId);

            var articleButtons = visible
                .Select(a =>
                {
                    var articleHash = slugMapping.GetOrCreateSlug(a.Slug);
                    var data = CallbackPrefixes.ShowArticle + articleHash + CallbackPrefixes.Separator + "search" + CallbackPrefixes.Separator + requestedVisible;
                    return (Text: a.Title, CallbackData: data);
                })
                .ToList();

            var rows = articleButtons
                .Chunk(2)
                .Select(chunk => (IEnumerable<(string Text, string CallbackData)>)chunk)
                .ToList();

            if (all.Count > clamped)
                rows.Add(new[] { (UiTexts.MoreButton, CallbackPrefixes.SearchMore) });
            rows.Add(new[] { (UiTexts.MainMenuButton, CallbackPrefixes.MainMenu) });

            // edit existing message with restored results
            logger.LogInformation("Publishing restored search results to chat {ChatId} (edit={Edit})", context.ChatId, context.MessageId.HasValue);
            await publisher.PublishAsync(
                context,
                new BotResponse(
                    HtmlText: "<b>Search Results</b>\n\nI found these articles for your query:",
                    InlineKeyboard: rows,
                    EditMessage: context.MessageId.HasValue),
                cancellationToken);
        }
        finally
        {
            logger.LogDebug("Answering callback {CallbackQueryId} for chat {ChatId}", context.CallbackQueryId, context.ChatId);
            await publisher.AnswerCallbackAsync(context.CallbackQueryId, null, cancellationToken);
        }
    }
}
