namespace YouDoFaqBot.Core.Models;

/// <summary>
/// Represents the context for a message, including the chat identifier and an optional message identifier.
/// </summary>
/// <param name="ChatId">The unique identifier for the chat associated with the message.</param>
/// <param name="MessageId">The identifier of the specific message within the chat, or null if not applicable.</param>
public record MessageContext(long ChatId, int? MessageId);
