using Microsoft.Extensions.Logging;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Core.Handlers;

/// <summary>
/// Handles user rating callbacks and processes feedback responses for rated articles within the bot interface.
/// </summary>
/// <remarks>This handler responds to callbacks prefixed with rating commands and updates the user interface
/// accordingly. It integrates with the messaging system to provide acknowledgment and navigation buttons after feedback
/// is received. Thread safety depends on the injected services.</remarks>
/// <param name="slugMapping">The service used to resolve article slugs or hashes to their corresponding data representations.</param>
/// <param name="publisher">The publisher used to send or edit responses and callback replies to the user.</param>
/// <param name="logger">The logger used to record feedback actions and operational information for this handler.</param>
public class RatingHandler(
    ISlugMappingService slugMapping,
    IBotResponsePublisher publisher,
    ILogger<RatingHandler> logger) : ICallbackHandler
{
    ///<inheritdoc/>
    public bool CanHandle(string callbackData) => callbackData.StartsWith(CallbackPrefixes.Rate);

    ///<inheritdoc/>
    public async Task HandleAsync(CallbackContext context, string callbackData, CancellationToken cancellationToken)
    {
        try
        {
            // rate:up:hash | rate:down:hash
            var payload = callbackData[CallbackPrefixes.Rate.Length..];
            var parts = payload.Split(CallbackPrefixes.Separator, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return;

            var rating = parts[0];
            var slugOrHash = parts[1];
            // parts: [rating, slugOrHash, ...] rest can be either subHash or ("search", visible)
            string? subHash = null;
            int? searchVisible = null;
            if (parts.Length >= 3)
            {
                if (parts[2] == "search" && parts.Length >= 4 && int.TryParse(parts[3], out var vis))
                {
                    searchVisible = vis;
                }
                else
                {
                    subHash = parts[2];
                }
            }

            var slug = slugMapping.GetDataBySlug(slugOrHash) ?? slugOrHash;

            logger.LogInformation("USER_FEEDBACK: Article {Slug} rated as {Rating} (chat {ChatId})", slug, rating, context.ChatId);

            if (context.MessageId is int messageId)
            {
                var rows = new List<IEnumerable<(string Text, string CallbackData)>>
                {
                    ([(UiTexts.FeedbackAcceptedButton, CallbackPrefixes.Ignore)])
                };
                if (searchVisible is int visible)
                {
                    rows.Add([(UiTexts.BackButton, CallbackPrefixes.SearchRestore + visible)]);
                }
                else if (!string.IsNullOrEmpty(subHash))
                {
                    rows.Add([(UiTexts.BackButton, CallbackPrefixes.Subcategory + subHash)]);
                }
                rows.Add([(UiTexts.MainMenuButton, CallbackPrefixes.MainMenu)]);

                await publisher.EditReplyMarkupAsync(context.ChatId, messageId, rows, cancellationToken);
            }
        }
        finally
        {
            await publisher.AnswerCallbackAsync(context.CallbackQueryId, BotMessages.FeedbackThanks, cancellationToken);
        }
    }
}
