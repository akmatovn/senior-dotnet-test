using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using YouDoFaqBot.Interfaces;

namespace YouDoFaqBot.Telegram;

/// <summary>
/// Handles incoming updates from Telegram and processes user messages.
/// Responsible for searching the knowledge base and sending responses.
/// </summary>
public class UpdateHandler(IKnowledgeBaseService kbService, ILogger<UpdateHandler> logger) : IUpdateHandler
{
    private readonly IKnowledgeBaseService _kbService = kbService;
    private readonly ILogger<UpdateHandler> _logger = logger;

    /// <summary>
    /// Processes incoming updates from Telegram.
    /// If the update is a text message, searches the knowledge base and sends a response.
    /// </summary>
    /// <param name="botClient">The Telegram bot client.</param>
    /// <param name="update">The incoming update.</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Type != UpdateType.Message || update.Message?.Type != MessageType.Text)
                return;

            var chatId = update.Message.Chat.Id;
            var userText = update.Message.Text ?? string.Empty;

            _logger.LogInformation("Received message from chat {ChatId}: {Message}", chatId, userText);

            var articles = await _kbService.SearchAsync(userText, cancellationToken);
            if (articles.Count > 0)
            {
                var article = articles[0];
                var content = article.Content;
                if (content.Length > 3800)
                {
                    content = content.Substring(0, 3800) + "... (read more on website)";
                }
                var response = $"<b>{EscapeHtml(article.Title)}</b>\n\n{EscapeHtml(content)}";
                var sendRequest = new SendMessageRequest
                {
                    ChatId = chatId,
                    Text = response,
                    ParseMode = ParseMode.Html
                };
                await botClient.SendRequest(sendRequest, cancellationToken);
            }
            else
            {
                var notFoundRequest = new SendMessageRequest
                {
                    ChatId = chatId,
                    Text = "Sorry, nothing was found for your query."
                };
                await botClient.SendRequest(notFoundRequest, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");
        }
    }

    /// <summary>
    /// Handles errors that occur during polling.
    /// Logs the exception and source.
    /// </summary>
    /// <param name="botClient">The Telegram bot client.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="source">The source of the error.</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>A completed task.</returns>
    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Polling error occurred");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Escapes HTML special characters in the given text for safe Telegram message formatting.
    /// </summary>
    /// <param name="text">The input text to escape.</param>
    /// <returns>The escaped HTML string.</returns>
    private static string EscapeHtml(string text)
        => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
