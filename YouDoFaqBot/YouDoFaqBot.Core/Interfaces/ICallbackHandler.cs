using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Handlers;

namespace YouDoFaqBot.Core.Interfaces;

/// <summary>
/// Defines a handler capable of determining whether it can process a given callback and performing asynchronous
/// processing of callback data within a specific context.
/// </summary>
/// <remarks>Implementations of this interface are typically used to dispatch and handle callback events, such as
/// those received from external systems, messaging platforms, or user interactions. Implementors should ensure thread
/// safety if their handlers will be invoked concurrently.</remarks>
public interface ICallbackHandler
{
    /// <summary>
    /// Determines whether the handler is able to process the specified callback data.
    /// </summary>
    /// <param name="callbackData">The callback data to evaluate. Cannot be null.</param>
    /// <returns>true if the handler can process the specified callback data; otherwise, false.</returns>
    bool CanHandle(string callbackData);

    /// <summary>
    /// Processes a callback asynchronously based on the provided context and callback data.
    /// </summary>
    /// <param name="context">The context information associated with the callback. Cannot be null.</param>
    /// <param name="callbackData">The data associated with the callback to handle. Cannot be null or empty.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous callback handling operation.</returns>
    Task HandleAsync(CallbackContext context, string callbackData, CancellationToken cancellationToken);
}
