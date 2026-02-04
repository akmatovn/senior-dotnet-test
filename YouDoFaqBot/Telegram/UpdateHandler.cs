using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using YouDoFaqBot.Services;
using Microsoft.Extensions.Logging;

namespace YouDoFaqBot.Telegram;

public class UpdateHandler(IKnowledgeBaseService kbService, ILogger<UpdateHandler> logger) : IUpdateHandler
{
    private readonly IKnowledgeBaseService _kbService = kbService;
    private readonly ILogger<UpdateHandler> _logger = logger;

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
                    Text = "Извините, ничего не найдено по вашему запросу."
                };
                await botClient.SendRequest(notFoundRequest, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Polling error occurred");
        return Task.CompletedTask;
    }

    private static string EscapeHtml(string text)
        => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
