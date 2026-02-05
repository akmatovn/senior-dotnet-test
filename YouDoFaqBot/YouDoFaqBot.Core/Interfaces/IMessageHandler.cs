namespace YouDoFaqBot.Core.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using YouDoFaqBot.Core.Models;

/// <summary>
/// Defines a contract for handlers that process incoming text messages.
/// Implementations decide whether they can handle a given message and perform the handling logic.
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// Determines whether this handler can process the specified message.
    /// </summary>
    /// <param name="context">The <see cref="MessageContext"/> containing chat and message details.</param>
    /// <param name="messageText">The text of the message to evaluate.</param>
    /// <returns><c>true</c> if the handler can process the message; otherwise <c>false</c>.</returns>
    bool CanHandle(MessageContext context, string messageText);

    /// <summary>
    /// Handles the incoming message asynchronously.
    /// Implementations should respect the provided <paramref name="cancellationToken"/> for graceful cancellation.
    /// </summary>
    /// <param name="context">The <see cref="MessageContext"/> containing chat and message details.</param>
    /// <param name="messageText">The text of the message to handle.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous handling operation.</returns>
    Task HandleAsync(MessageContext context, string messageText, CancellationToken cancellationToken);
}
