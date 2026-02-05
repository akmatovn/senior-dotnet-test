namespace YouDoFaqBot.Core.Settings;

/// <summary>
/// Represents configuration options for the Telegram bot integration.
/// </summary>
public class TelegramOptions
{
    /// <summary>
    /// Gets or sets the Telegram bot token used for authenticating with the Telegram Bot API.
    /// </summary>
    public string BotToken { get; set; } = string.Empty;
}
