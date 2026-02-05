namespace YouDoFaqBot.Core.Utils;

/// <summary>
/// Command constants used by the bot (Telegram commands and command prefixes).
/// </summary>
public static class Commands
{
    /// <summary>
    /// The standard start command.
    /// </summary>
    public const string Start = "/start";

    /// <summary>
    /// The search command base (without trailing space).
    /// Use <see cref="SearchPrefix"/> when checking for a typed search with an argument.
    /// </summary>
    public const string Search = "/search";

    /// <summary>
    /// The search command prefix including a trailing space used when matching "/search &lt;query&gt;".
    /// </summary>
    public const string SearchPrefix = "/search ";
}
