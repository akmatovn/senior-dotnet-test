namespace YouDoFaqBot.Core.Models;

/// <summary>
/// Represents the context of a callback event, including identifiers used to reference the chat, related message, and
/// callback query.
/// </summary>
/// <remarks>Use this record to encapsulate the necessary information when handling or responding to callback
/// events, such as those in messaging or bot APIs. This context facilitates operations like editing messages or
/// answering callback queries within the relevant chat or message scope.</remarks>
/// <param name="ChatId">The unique identifier for the chat in which the callback event originated.</param>
/// <param name="MessageId">The identifier of the message associated with the callback event, or null if the callback is not tied to a specific
/// message.</param>
/// <param name="CallbackQueryId">The unique identifier for the callback query that triggered the event.</param>
public record CallbackContext(long ChatId, int? MessageId, string CallbackQueryId);
