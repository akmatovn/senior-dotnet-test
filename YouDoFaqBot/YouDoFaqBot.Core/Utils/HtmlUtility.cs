using System.Net;

namespace YouDoFaqBot.Core.Utils;

/// <summary>
/// Provides utility methods for working with HTML content.
/// </summary>
public static class HtmlUtility
{
    /// <summary>
    /// Formats an article title and content as HTML, encoding each to ensure safe display.
    /// </summary>
    /// <param name="title">The article title to include in the formatted HTML. Cannot be null.</param>
    /// <param name="content">The article content to include in the formatted HTML. Cannot be null.</param>
    /// <returns>A string containing the formatted HTML with the title in bold, followed by the content. Both are HTML-encoded.</returns>
    public static string FormatArticleHtml(string title, string content)
        => $"<b>{WebUtility.HtmlEncode(title)}</b>\n{WebUtility.HtmlEncode(content)}";
}
