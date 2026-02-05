using YouDoFaqBot.Core.Models;

namespace YouDoFaqBot.Core.Interfaces;

/// <summary>
/// Defines a contract for publishing bot responses and managing message interactions in a chat platform.
/// Implementations are responsible for sending, editing, deleting messages, and answering callback queries.
/// </summary>
public interface IBotResponsePublisher
{
    /// <summary>
    /// Publishes a bot response to the specified chat, either by editing an existing message or sending a new one.
    /// </summary>
    /// <param name="context">The callback context containing chat and message information.</param>
    /// <param name="response">The bot response to publish.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    Task PublishAsync(CallbackContext context, BotResponse response, CancellationToken cancellationToken);

    /// <summary>
    /// Edits the inline keyboard markup of a specific message.
    /// </summary>
    /// <param name="chatId">The chat identifier.</param>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="inlineKeyboard">The new inline keyboard layout.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>A task representing the asynchronous edit operation.</returns>
    Task EditReplyMarkupAsync(long chatId, int messageId, IEnumerable<IEnumerable<(string Text, string CallbackData)>> inlineKeyboard, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a message from a chat.
    /// </summary>
    /// <param name="chatId">The chat identifier.</param>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    Task DeleteMessageAsync(long chatId, int messageId, CancellationToken cancellationToken);

    /// <summary>
    /// Answers a callback query, optionally displaying a notification to the user.
    /// </summary>
    /// <param name="callbackQueryId">The callback query identifier.</param>
    /// <param name="text">The notification text to display (optional).</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>A task representing the asynchronous answer operation.</returns>
    Task AnswerCallbackAsync(string callbackQueryId, string? text, CancellationToken cancellationToken);
}
