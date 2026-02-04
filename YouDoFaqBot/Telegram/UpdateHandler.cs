
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using YouDoFaqBot.Models;
using YouDoFaqBot.Services;

namespace YouDoFaqBot.Telegram;

public class UpdateHandler(IKnowledgeBaseService kbService)
{
    private readonly IKnowledgeBaseService _kbService = kbService;

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message?.Type != MessageType.Text)
            return;

        var chatId = update.Message.Chat.Id;
        var userText = update.Message.Text ?? string.Empty;

        var articles = await _kbService.SearchAsync(userText, cancellationToken);
        if (articles.Count > 0)
        {
            var article = articles[0];
            var response = $"<b>{EscapeHtml(article.Title)}</b>\n\n{EscapeHtml(article.Content)}";
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: response,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "????????, ?????? ?? ??????? ?? ?????? ???????.",
                cancellationToken: cancellationToken);
        }
    }

    private static string EscapeHtml(string text)
        => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
