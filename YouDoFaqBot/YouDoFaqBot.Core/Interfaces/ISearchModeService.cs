namespace YouDoFaqBot.Core.Interfaces;

/// <summary>
/// Tracks whether a chat is currently awaiting a search query entered into the search field.
/// </summary>
public interface ISearchModeService
{
    /// <summary>
    /// Attempts to retrieve the awaiting status for the specified chat identifier.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat for which to retrieve the awaiting status.</param>
    /// <param name="awaiting">When this method returns, contains <see langword="true"/> if the chat is awaiting; otherwise, <see
    /// langword="false"/>. This parameter is passed uninitialized.</param>
    /// <returns><see langword="true"/> if the awaiting status was found for the specified chat identifier; otherwise, <see
    /// langword="false"/>.</returns>
    bool TryGet(long chatId, out bool awaiting);

    /// <summary>
    /// Sets the awaiting status for the specified chat.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat for which to set the awaiting status.</param>
    /// <param name="awaiting">A value indicating whether the chat is awaiting input. Set to <see langword="true"/> to mark the chat as
    /// awaiting; otherwise, <see langword="false"/>.</param>
    void Set(long chatId, bool awaiting);

    /// <summary>
    /// Removes all messages or data associated with the specified chat.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat whose data is to be cleared.</param>
    void Clear(long chatId);
}
