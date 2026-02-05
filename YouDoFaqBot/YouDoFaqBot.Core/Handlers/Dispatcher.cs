using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;

namespace YouDoFaqBot.Core.Handlers;

/// <summary>
/// Dispatches incoming update events to the appropriate callback or message handlers.
/// </summary>
/// <remarks>The UpdateDispatcher coordinates the distribution of Telegram update events to registered handler
/// implementations. Multiple handler types can be provided to enable extensible processing of different update types.
/// This class is not thread safe and should not be shared across concurrently running update streams.</remarks>
/// <param name="logger">The logger used to record execution details, debug information, and warnings during dispatch operations.</param>
/// <param name="callbackHandlers">The collection of callback query handlers responsible for processing callback data from updates.</param>
/// <param name="messageHandlers">The collection of message handlers used to process text messages from updates.</param>
public class UpdateDispatcher(
    ILogger<UpdateDispatcher> logger,
    IEnumerable<ICallbackHandler> callbackHandlers,
    IEnumerable<IMessageHandler> messageHandlers)
{
    private readonly ILogger<UpdateDispatcher> _logger = logger;
    private readonly IEnumerable<ICallbackHandler> _callbackHandlers = callbackHandlers;
    private readonly IEnumerable<IMessageHandler> _messageHandlers = messageHandlers;

    /// <summary>
    /// Processes an incoming update by dispatching it to the appropriate message or callback handler asynchronously.
    /// </summary>
    /// <remarks>If the update contains a message with text, the message handler is invoked. If the update
    /// contains a callback query with data, the callback handler is invoked. Updates with unsupported types are ignored
    /// and logged for debugging purposes.</remarks>
    /// <param name="update">The update to process, containing either a message or a callback query. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous dispatch operation.</returns>
    public async Task DispatchAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery is { } callbackQuery)
        {
            var data = callbackQuery.Data;
            if (data is not null)
            {
                var chatId = callbackQuery.Message?.Chat.Id ?? 0;
                var messageId = callbackQuery.Message?.MessageId;
                var context = new CallbackContext(chatId, messageId, callbackQuery.Id);
                await DispatchCallbackAsync(context, data, cancellationToken);
            }
        }
        else if (update.Message is { } message)
        {
            var text = message.Text;
            if (text is not null)
            {
                var context = new MessageContext(message.Chat.Id, message.MessageId);
                await DispatchMessageAsync(context, text, cancellationToken);
            }
        }
        else
        {
            _logger.LogDebug("Update type not handled: {Type}", update.Type);
        }
    }

    /// <summary>
    /// Asynchronously dispatches a callback query to the first registered handler capable of processing the specified
    /// callback data.
    /// </summary>
    /// <remarks>If no handler is found that can process the specified callback data, the method completes
    /// without invoking any handler.</remarks>
    /// <param name="context">The context for the callback, providing information and services relevant to the current request.</param>
    /// <param name="callbackData">The data identifying the callback to be processed. This determines which handler will be invoked.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous dispatch operation.</returns>
    public async Task DispatchCallbackAsync(CallbackContext context, string callbackData, CancellationToken cancellationToken)
    {
        var handler = _callbackHandlers.FirstOrDefault(h => h.CanHandle(callbackData));
        if (handler != null)
        {
            _logger.LogInformation("Dispatching callback query to handler: {Handler}", handler.GetType().Name);
            await handler.HandleAsync(context, callbackData, cancellationToken);
        }
        else
        {
            _logger.LogWarning("No callback handler found for data: {Data}", callbackData);
        }
    }

    /// <summary>
    /// Dispatches an incoming message to the first registered handler that can process it, using the provided context
    /// and message text.
    /// </summary>
    /// <remarks>If no handler is found that can process the message, the method logs a warning and takes no
    /// further action. Only the first handler that qualifies will process the message.</remarks>
    /// <param name="context">The context of the message to be dispatched. Contains information relevant to message handling, such as sender,
    /// channel, and metadata.</param>
    /// <param name="messageText">The text of the message to be dispatched to a handler.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous dispatch operation.</returns>
    public async Task DispatchMessageAsync(MessageContext context, string messageText, CancellationToken cancellationToken)
    {
        var handler = _messageHandlers.FirstOrDefault(h => h.CanHandle(context, messageText));
        if (handler != null)
        {
            _logger.LogInformation("Dispatching message to handler: {Handler}", handler.GetType().Name);
            await handler.HandleAsync(context, messageText, cancellationToken);
        }
        else
        {
            _logger.LogWarning("No message handler found for message: {Text}", messageText);
        }
    }
}
