namespace YouDoFaqBot.Core.Interfaces;

/// <summary>
/// Provides methods for managing the search state of users in the chat,
/// such as storing and retrieving the last search query and visible result count per chat.
/// </summary>
public interface ISearchStateService
{
    /// <summary>
    /// Attempts to retrieve the search state (query and visible count) for the specified chat.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat.</param>
    /// <param name="state">
    /// When this method returns, contains a tuple with the last search query and visible count,
    /// if the chat has a stored state; otherwise, the default value.
    /// </param>
    /// <returns>
    /// <c>true</c> if the search state was found for the chat; otherwise, <c>false</c>.
    /// </returns>
    bool TryGet(long chatId, out (string Query, int VisibleCount) state);

    /// <summary>
    /// Sets or updates the search state for the specified chat.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat.</param>
    /// <param name="query">The search query to store.</param>
    /// <param name="visibleCount">The number of visible search results to store.</param>
    void Set(long chatId, string query, int visibleCount);
}
