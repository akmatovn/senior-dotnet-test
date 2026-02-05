using System.Collections.Concurrent;
using YouDoFaqBot.Core.Interfaces;

namespace YouDoFaqBot.Core.Services;

/// <summary>
/// Provides methods to manage and retrieve per-chat search state information, including the last search query and
/// number of visible items.
/// </summary>
/// <remarks>This service maintains search-related state for multiple chats in a thread-safe manner. Use this type
/// to store or access the current search query and visible item count associated with a specific chat. Designed for use
/// cases where temporary, per-chat search information needs to persist across user actions.</remarks>
public class SearchStateService : ISearchStateService
{
    private readonly ConcurrentDictionary<long, (string Query, int VisibleCount)> _state = new();

    ///<inheritdoc/>
    public bool TryGet(long chatId, out (string Query, int VisibleCount) state)
        => _state.TryGetValue(chatId, out state);

    ///<inheritdoc/>
    public void Set(long chatId, string query, int visibleCount)
        => _state[chatId] = (query, visibleCount);
}
