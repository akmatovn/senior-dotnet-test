using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;

namespace YouDoFaqBot.App.Telegram;

/// <summary>
/// Publishes responses and manages message interactions for the Telegram bot.
/// </summary>
/// <param name="botClient">The Telegram bot client instance.</param>
public class TelegramBotResponsePublisher(ITelegramBotClient botClient) : IBotResponsePublisher
{
    /// <summary>
    /// Publishes a bot response to the specified chat, either by editing an existing message or sending a new one.
    /// </summary>
    /// <param name="context">The callback context containing chat and message information.</param>
    /// <param name="response">The bot response to publish.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    public async Task PublishAsync(CallbackContext context, BotResponse response, CancellationToken cancellationToken)
    {
        var replyMarkup = response.InlineKeyboard is null
            ? null
            : new InlineKeyboardMarkup(
                response.InlineKeyboard.Select(row =>
                    row.Select(b => InlineKeyboardButton.WithCallbackData(b.Text, TruncateCallbackData(b.CallbackData))).ToArray()));

        if (response.EditMessage && context.MessageId is int messageId)
        {
            var editRequest = new EditMessageTextRequest
            {
                ChatId = context.ChatId,
                MessageId = messageId,
                Text = response.HtmlText,
                ParseMode = ParseMode.Html,
                ReplyMarkup = replyMarkup
            };

            try
            {
                await botClient.SendRequest(editRequest, cancellationToken);
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 400 && ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
            {
                // nothing to change
                return;
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 400 && ex.Message.Contains("message can't be edited", StringComparison.OrdinalIgnoreCase))
            {
                // Fallback: if message can't be edited (deleted/old), send a new message instead
                var sendRequestFallback = new SendMessageRequest
                {
                    ChatId = context.ChatId,
                    Text = response.HtmlText,
                    ParseMode = ParseMode.Html,
                    ReplyMarkup = replyMarkup
                };
                await botClient.SendRequest(sendRequestFallback, cancellationToken);
                return;
            }

            return;
        }

        var sendRequest = new SendMessageRequest
        {
            ChatId = context.ChatId,
            Text = response.HtmlText,
            ParseMode = ParseMode.Html,
            ReplyMarkup = replyMarkup
        };

        await botClient.SendRequest(sendRequest, cancellationToken);
    }

    /// <summary>
    /// Edits the inline keyboard markup of a specific message.
    /// </summary>
    /// <param name="chatId">The chat identifier.</param>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="inlineKeyboard">The new inline keyboard layout.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    public async Task EditReplyMarkupAsync(long chatId, int messageId, IEnumerable<IEnumerable<(string Text, string CallbackData)>> inlineKeyboard, CancellationToken cancellationToken)
    {
        var replyMarkup = new InlineKeyboardMarkup(
            inlineKeyboard.Select(row =>
                row.Select(b => InlineKeyboardButton.WithCallbackData(b.Text, TruncateCallbackData(b.CallbackData))).ToArray()));

        var req = new EditMessageReplyMarkupRequest
        {
            ChatId = chatId,
            MessageId = messageId,
            ReplyMarkup = replyMarkup
        };

        try
        {
            await botClient.SendRequest(req, cancellationToken);
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 400 && ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
        {
            // ignore
        }
    }

    /// <summary>
    /// Answers a callback query, optionally displaying a notification to the user.
    /// </summary>
    /// <param name="callbackQueryId">The callback query identifier.</param>
    /// <param name="text">The notification text to display (optional).</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    public async Task AnswerCallbackAsync(string callbackQueryId, string? text, CancellationToken cancellationToken)
    {
        var req = new AnswerCallbackQueryRequest { CallbackQueryId = callbackQueryId, Text = text };
        try
        {
            await botClient.SendRequest(req, cancellationToken);
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// Deletes a message from a chat.
    /// </summary>
    /// <param name="chatId">The chat identifier.</param>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    public async Task DeleteMessageAsync(long chatId, int messageId, CancellationToken cancellationToken)
    {
        try
        {
            await botClient.SendRequest(new DeleteMessageRequest { ChatId = chatId, MessageId = messageId }, cancellationToken);
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 400)
        {
            // ignore deletion errors
        }
    }

    /// <summary>
    /// Truncates callback data to ensure it does not exceed Telegram's 64-byte limit.
    /// </summary>
    /// <param name="data">The callback data string.</param>
    /// <returns>The truncated callback data string.</returns>
    private static string TruncateCallbackData(string data)
    {
        if (Encoding.UTF8.GetByteCount(data) <= 64)
            return data;

        return data[..Math.Min(data.Length, 64)];
    }
}
