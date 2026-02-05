using Microsoft.Extensions.Logging;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Core.Handlers;

/// <summary>
/// Handles callback queries that should be ignored, typically by acknowledging them without performing any additional
/// action.
/// </summary>
/// <remarks>This handler is intended for cases where callback queries do not require any action beyond
/// acknowledgment. It ensures that the callback is answered to satisfy platform requirements, preventing timeouts or
/// repeated delivery.</remarks>
/// <param name="publisher">The publisher used to send responses to callback queries.</param>
/// <param name="logger">The logger used to record informational messages during callback handling.</param>
public class IgnoreHandler(
    IBotResponsePublisher publisher, 
    ILogger<IgnoreHandler> logger) : ICallbackHandler
{
    ///<inheritdoc/>
    public bool CanHandle(string callbackData) => callbackData == CallbackPrefixes.Ignore;

    ///<inheritdoc/>
    public async Task HandleAsync(CallbackContext context, string callbackData, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Ignore callback received");
        }
        finally
        {
            await publisher.AnswerCallbackAsync(context.CallbackQueryId, null, cancellationToken);
        }
    }
}
