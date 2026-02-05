using System.Collections.Concurrent;
using YouDoFaqBot.Core.Interfaces;

namespace YouDoFaqBot.Core.Services;

/// <summary>
/// Provides thread-safe management of per-chat search mode state, indicating whether a chat is currently
/// awaiting a search query from the user. Used to control when the bot should interpret incoming messages as search queries.
/// </summary>
public class SearchModeService : ISearchModeService
{
    /// <summary>
    /// Stores the search mode state for each chat. The key is the chat ID, the value is true if awaiting a search query.
    /// </summary>
    private readonly ConcurrentDictionary<long, bool> _modes = new();

    /// <summary>
    /// Attempts to retrieve the search mode state for the specified chat.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat.</param>
    /// <param name="awaiting">
    /// When this method returns, contains true if the chat is awaiting a search query; otherwise, false.
    /// </param>
    /// <returns>
    /// true if the search mode state was found for the chat; otherwise, false.
    /// </returns>
    public bool TryGet(long chatId, out bool awaiting) => _modes.TryGetValue(chatId, out awaiting);

    /// <summary>
    /// Sets or updates the search mode state for the specified chat.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat.</param>
    /// <param name="awaiting">true to indicate the chat is awaiting a search query; otherwise, false.</param>
    public void Set(long chatId, bool awaiting) => _modes[chatId] = awaiting;

    /// <summary>
    /// Clears the search mode state for the specified chat, indicating it is no longer awaiting a search query.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat.</param>
    public void Clear(long chatId) => _modes.TryRemove(chatId, out _);
}
