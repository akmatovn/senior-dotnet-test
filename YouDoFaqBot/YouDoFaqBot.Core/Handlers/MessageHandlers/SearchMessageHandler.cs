using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Settings;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Core.Handlers.MessageHandlers;

/// <summary>
/// Initializes a new instance of the <see cref="SearchMessageHandler"/> class.
/// Handles user search queries, performs search in the knowledge base, and publishes results.
/// </summary>
/// <param name="knowledgeBaseService">Service for searching the knowledge base.</param>
/// <param name="slugMapping">Service for mapping and generating slugs for articles.</param>
/// <param name="searchState">Service for managing search state per user.</param>
/// <param name="publisher">Service for publishing bot responses to Telegram.</param>
/// <param name="logger">Logger for diagnostic and operational messages.</param>
/// <param name="options">Options for knowledge base configuration.</param>
public class SearchMessageHandler(
    IKnowledgeBaseService knowledgeBaseService,
    ISlugMappingService slugMapping,
    ISearchStateService searchState,
    ISearchModeService searchMode,
    IBotResponsePublisher publisher,
    ILogger<SearchMessageHandler> logger,
    IOptions<KnowledgeBaseOptions> options) : IMessageHandler
{
    private readonly int SearchPageSize = options.Value.SearchPageSize;

    ///<inheritdoc/>
    public bool CanHandle(MessageContext context, string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText) || messageText == Commands.Start || messageText == UiTexts.BrowseFaqButton)
            return false;

        // Allow search if chat is in search mode or user used explicit /search command
        if (messageText.StartsWith(Commands.SearchPrefix))
            return true;

        if (searchMode.TryGet(context.ChatId, out var awaiting) && awaiting)
            return true;

        return false;
    }

    ///<inheritdoc/>
    public async Task HandleAsync(MessageContext context, string messageText, CancellationToken cancellationToken)
    {
        // strip command prefix when user sent "/search <query>"
        var query = messageText.StartsWith(Commands.SearchPrefix) ? messageText.Substring(Commands.SearchPrefix.Length).Trim() : messageText.Trim();
        logger.LogInformation("Search query from chat {ChatId}: {Query}", context.ChatId, query);

        var results = await knowledgeBaseService.SearchAsync(query, cancellationToken, limit: SearchPageSize);
        if (results.Count == 0)
        {
            await publisher.PublishAsync(
                new CallbackContext(context.ChatId, context.MessageId, string.Empty),
                new BotResponse(BotMessages.SearchNotFound),
                cancellationToken);
            // exit search mode if active
            searchMode.Clear(context.ChatId);
            return;
        }

        searchState.Set(context.ChatId, query, SearchPageSize);
        // exit search mode after performing search
        searchMode.Clear(context.ChatId);
        // When user sends a text message, publish results as a new message (do not try to edit the user's message)
        await PublishSearchResultsAsync(context.ChatId, null, query, SearchPageSize, cancellationToken);
    }

    /// <summary>
    /// Publishes a list of search result articles to a chat, sending a new message or editing an existing one as
    /// appropriate.
    /// </summary>
    /// <remarks>If there are more search results than can be shown, a 'More' button is included to allow
    /// users to request additional results. A main menu button is always added to the response.</remarks>
    /// <param name="chatId">The unique identifier for the chat where the search results will be published.</param>
    /// <param name="messageId">The identifier of the message to edit, or null to send a new message.</param>
    /// <param name="query">The search query used to retrieve articles from the knowledge base.</param>
    /// <param name="visibleCount">The maximum number of search result articles to display in the response. Must be zero or greater.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    private async Task PublishSearchResultsAsync(long chatId, int? messageId, string query, int visibleCount, CancellationToken cancellationToken)
    {
        var all = await knowledgeBaseService.SearchAsync(query, cancellationToken, limit: null);
        var clamped = Math.Min(all.Count, Math.Max(0, visibleCount));
        var visible = all.Take(clamped).ToList();

        var articleButtons = visible
            .Select(a =>
            {
                var articleHash = slugMapping.GetOrCreateSlug(a.Slug);
                var data = CallbackPrefixes.ShowArticle + articleHash + CallbackPrefixes.Separator + "search" + CallbackPrefixes.Separator + visibleCount;
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

        // If messageId is null we should send a new message, otherwise attempt to edit
        await publisher.PublishAsync(
            new CallbackContext(chatId, messageId, string.Empty),
            new BotResponse(
                HtmlText: BotMessages.SearchResultsHeader,
                InlineKeyboard: rows,
                EditMessage: messageId.HasValue),
            cancellationToken);
    }
}
