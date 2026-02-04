namespace YouDoFaqBot.Core.Settings;

/// <summary>
/// Configuration options for the Telegram bot.
/// </summary>
public class TelegramOptions
{
    /// <summary>
    /// The bot token used to authenticate with the Telegram Bot API.
    /// </summary>
    public string BotToken { get; set; } = string.Empty;
}
