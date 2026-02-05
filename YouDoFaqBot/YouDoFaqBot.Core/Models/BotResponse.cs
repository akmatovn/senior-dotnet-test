namespace YouDoFaqBot.Core.Models;

/// <summary>
/// Represents a response from the bot, including formatted text and an optional inline keyboard layout.
/// </summary>
/// <remarks>Use this type to structure bot responses that include message text and optional interactive elements.
/// The inline keyboard enables users to interact with the message through on-screen buttons, each capable of triggering
/// specific callbacks when pressed.</remarks>
/// <param name="HtmlText">The HTML-formatted text message to display in the response. Must not be null or empty.</param>
/// <param name="InlineKeyboard">An optional collection describing the inline keyboard layout, where each inner collection represents a row of
/// buttons. Each button is defined by a tuple containing its display text and associated callback data. If null, no
/// inline keyboard is included.</param>
/// <param name="EditMessage">true to indicate that the existing message should be edited with the provided content; otherwise, false to send a
/// new message.</param>
public record BotResponse(string HtmlText, IEnumerable<IEnumerable<(string Text, string CallbackData)>>? InlineKeyboard = null, bool EditMessage = false);
