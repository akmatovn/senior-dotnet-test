using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;

namespace YouDoFaqBot.Core.Telegram;

/// <summary>
/// Handles incoming updates from Telegram and processes user messages.
/// Responsible for searching the knowledge base and sending responses.
/// </summary>
public class UpdateHandler(IKnowledgeBaseService kbService, ILogger<UpdateHandler> logger) : IUpdateHandler
{
    // In-memory mapping from hash to slug for callback_data
    private static readonly ConcurrentDictionary<string, string> _slugHashMap = new();

    private const int SearchPageSize = 6;
    private static readonly ConcurrentDictionary<long, (string Query, int VisibleCount)> _searchState = new();

    private static string GetSlugHash(string slug)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(slug));
        // Use first 8 bytes (16 hex chars) for compactness
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }

    private async Task SendSearchResultsAsync(ITelegramBotClient botClient, long chatId, int? messageId, string query, int visibleCount, CancellationToken cancellationToken)
    {
        var all = await _kbService.SearchAsync(query, cancellationToken, limit: null);
        var clampedVisibleCount = Math.Min(all.Count, Math.Max(0, visibleCount));
        var visible = all.Take(clampedVisibleCount).ToList();
        var rows = visible.Select(a =>
        {
            var hash = GetSlugHash(a.Slug);
            _slugHashMap[hash] = a.Slug;
            return InlineKeyboardButton.WithCallbackData(a.Title, $"show_art:{hash}:{clampedVisibleCount}");
        })
        .Chunk(2)
        .Select(r => r.ToArray())
        .ToList();

        if (all.Count > clampedVisibleCount)
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("Еще", "search_more") });
        else if (clampedVisibleCount > SearchPageSize)
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("Список полностью загружен", "ignore") });

        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🏠 Main Menu", "main_menu") });

        var keyboard = new InlineKeyboardMarkup(rows);
        var text = "<b>Search Results</b>\n\nI found these articles for your query:";

        if (messageId.HasValue)
        {
            var editRequest = new EditMessageTextRequest
            {
                ChatId = chatId,
                MessageId = messageId.Value,
                Text = text,
                ParseMode = ParseMode.Html,
                ReplyMarkup = keyboard
            };
            try
            {
                await botClient.SendRequest(editRequest, cancellationToken);
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 400 && ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("EditMessageTextRequest: message is not modified for chat {ChatId}, message {MessageId}", chatId, messageId);
            }
        }
        else
        {
            var request = new SendMessageRequest
            {
                ChatId = chatId,
                Text = text,
                ParseMode = ParseMode.Html,
                ReplyMarkup = keyboard
            };
            await botClient.SendRequest(request, cancellationToken);
        }
    }

    private readonly IKnowledgeBaseService _kbService = kbService;
    private readonly ILogger<UpdateHandler> _logger = logger;

    /// <summary>
    /// Processes incoming updates from Telegram.
    /// Handles both text messages and callback queries.
    /// </summary>
    /// <param name="botClient">The Telegram bot client.</param>
    /// <param name="update">The incoming update.</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message when update.Message?.Type == MessageType.Text:
                    await HandleMessageAsync(botClient, update.Message, cancellationToken);
                    break;
                case UpdateType.CallbackQuery:
                    await HandleCallbackQueryAsync(botClient, update.CallbackQuery!, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");
        }
    }

    /// <summary>
    /// Handles incoming text messages, searches the knowledge base, and sends results.
    /// If multiple articles are found, sends an inline keyboard for user selection.
    /// </summary>
    /// <param name="botClient">The Telegram bot client.</param>
    /// <param name="message">The incoming message.</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    private async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var userText = message.Text ?? string.Empty;
        _logger.LogInformation("Received message from chat {ChatId}: {Message}", chatId, userText);
        if (userText == "/start")
        {
            // Show persistent reply keyboard on /start
            var replyKeyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton("📚 Browse FAQ") })
            {
                ResizeKeyboard = true,
                IsPersistent = true
            };
            var request = new SendMessageRequest
            {
                ChatId = chatId,
                Text = "Welcome! Tap the button below to browse the FAQ.",
                ParseMode = ParseMode.Html,
                ReplyMarkup = replyKeyboard
            };
            await botClient.SendRequest(request, cancellationToken);
            return;
        }
        if (userText == "📚 Browse FAQ")
        {
            await ShowCategoriesMenuAsync(botClient, chatId, null, cancellationToken);
            return;
        }
        var results = await _kbService.SearchAsync(userText, cancellationToken, limit: null);
        _logger.LogInformation("User query: '{Query}', found articles: {Count}", userText, results.Count);
        if (results.Count == 0)
        {
            _logger.LogInformation("No articles found for query '{Query}' in chat {ChatId}", userText, chatId);
            var notFoundRequest = new SendMessageRequest
            {
                ChatId = chatId,
                Text = "Sorry, nothing was found for your query."
            };
            await botClient.SendRequest(notFoundRequest, cancellationToken);
            return;
        }

        _searchState[chatId] = (userText, SearchPageSize);
        await SendSearchResultsAsync(botClient, chatId, null, userText, SearchPageSize, cancellationToken);
    }

    private async Task ShowCategoriesMenuAsync(ITelegramBotClient botClient, long chatId, int? messageId, CancellationToken cancellationToken)
    {
        var categories = await _kbService.GetCategoriesAsync(cancellationToken);
        var catButtons = categories.Select(c => InlineKeyboardButton.WithCallbackData(c.Title, $"cat:{c.Slug}")).Chunk(2).Select(row => row.ToArray()).ToList();
        catButtons.Add(new[] { InlineKeyboardButton.WithCallbackData("🏠 Main Menu", "main_menu") });
        var keyboard = new InlineKeyboardMarkup(catButtons);
        var text = "<b>FAQ Categories</b>\n\nChoose a category:";
        var replyKeyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton("📚 Browse FAQ") })
        {
            ResizeKeyboard = true,
            IsPersistent = true
        };
        if (messageId.HasValue)
        {
            var editRequest = new EditMessageTextRequest
            {
                ChatId = chatId,
                MessageId = messageId.Value,
                Text = text,
                ParseMode = ParseMode.Html,
                ReplyMarkup = keyboard
            };
            try
            {
                await botClient.SendRequest(editRequest, cancellationToken);
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 400 && ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("EditMessageTextRequest: message is not modified for chat {ChatId}, message {MessageId}", chatId, messageId);
                // Do not throw or log as error
            }
        }
        else
        {
            // Only send the inline keyboard for the main menu, not both
            var request = new SendMessageRequest
            {
                ChatId = chatId,
                Text = text,
                ParseMode = ParseMode.Html,
                ReplyMarkup = keyboard
            };
            await botClient.SendRequest(request, cancellationToken);
        }
    }

    /// <summary>
    /// Handles callback queries from inline keyboard buttons.
    /// Retrieves the article by slug and sends its content to the user.
    /// </summary>
    /// <param name="botClient">The Telegram bot client.</param>
    /// <param name="callbackQuery">The callback query received from Telegram.</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        try
        {
            if (callbackQuery.Data is null)
                return;

            if (callbackQuery.Data == "main_menu")
            {
                await ShowCategoriesMenuAsync(botClient, chatId, messageId, cancellationToken);
            }
            else if (callbackQuery.Data == "search_more")
            {
                if (!_searchState.TryGetValue(chatId, out var state))
                {
                    await ShowCategoriesMenuAsync(botClient, chatId, messageId, cancellationToken);
                    return;
                }

                var nextVisible = state.VisibleCount + SearchPageSize;
                _searchState[chatId] = (state.Query, nextVisible);
                await SendSearchResultsAsync(botClient, chatId, messageId, state.Query, nextVisible, cancellationToken);
            }
            else if (callbackQuery.Data.StartsWith("search:"))
            {
                var visibleStr = callbackQuery.Data.Substring("search:".Length);
                if (!_searchState.TryGetValue(chatId, out var state))
                {
                    await ShowCategoriesMenuAsync(botClient, chatId, messageId, cancellationToken);
                    return;
                }

                if (!int.TryParse(visibleStr, out var requestedVisible) || requestedVisible <= 0)
                    requestedVisible = state.VisibleCount;

                // Restore list to the requested size, but don't reduce the stored max loaded size
                if (requestedVisible > state.VisibleCount)
                    _searchState[chatId] = (state.Query, requestedVisible);

                await SendSearchResultsAsync(botClient, chatId, messageId, state.Query, requestedVisible, cancellationToken);
            }
            else if (callbackQuery.Data.StartsWith("cat:"))
            {
                var catSlug = callbackQuery.Data.Substring("cat:".Length);
                var subcategories = await _kbService.GetSubcategoriesByCategoryAsync(catSlug, cancellationToken);
                var cat = (await _kbService.GetCategoriesAsync(cancellationToken)).FirstOrDefault(c => c.Slug == catSlug);
                var subButtons = subcategories.Select(sc =>
                {
                    var callbackData = $"sub:{sc.Slug}";
                    if (System.Text.Encoding.UTF8.GetByteCount(callbackData) > 64)
                    {
                        _logger.LogWarning("Callback data too long for subcategory slug: {Slug} (length: {Length} bytes)", sc.Slug, System.Text.Encoding.UTF8.GetByteCount(callbackData));
                        return null;
                    }
                    return InlineKeyboardButton.WithCallbackData(sc.Title, callbackData);
                })
                .Where(b => b != null)
                .Chunk(2)
                .Select(row => row.ToArray())
                .ToList();
                subButtons.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "main_menu") });
                subButtons.Add(new[] { InlineKeyboardButton.WithCallbackData("🏠 Main Menu", "main_menu") });
                var keyboard = new InlineKeyboardMarkup(subButtons);
                var text = $"<b>{cat?.Title ?? "Category"}</b>\n\nChoose a subcategory:";
                var editRequest = new EditMessageTextRequest
                {
                    ChatId = chatId,
                    MessageId = messageId,
                    Text = text,
                    ParseMode = ParseMode.Html,
                    ReplyMarkup = keyboard
                };
                try
                {
                    await botClient.SendRequest(editRequest, cancellationToken);
                }
                catch (ApiRequestException ex) when (ex.ErrorCode == 400 && ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("EditMessageTextRequest: message is not modified for chat {ChatId}, message {MessageId}", chatId, messageId);
                }
            }
            else if (callbackQuery.Data.StartsWith("sub:"))
            {
                var subId = callbackQuery.Data.Substring("sub:".Length);
                if (!_slugHashMap.TryGetValue(subId, out var subSlug))
                    subSlug = subId;

                var articles = await _kbService.GetArticlesBySubcategoryAsync(subSlug, cancellationToken);
                var sub = articles.FirstOrDefault()?.Subcategory;
                var cat = articles.FirstOrDefault()?.Category;
                var artButtons = articles.Select(a =>
                {
                    var hash = GetSlugHash(a.Slug);
                    _slugHashMap[hash] = a.Slug;
                    return InlineKeyboardButton.WithCallbackData(a.Title, $"show_art:{hash}");
                })
                .Chunk(2)
                .Select(row => row.ToArray())
                .ToList();

                // Navigation: Back to category + Home
                artButtons.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"cat:{cat?.Slug}") });
                artButtons.Add(new[] { InlineKeyboardButton.WithCallbackData("🏠 Main Menu", "main_menu") });
                var keyboard = new InlineKeyboardMarkup(artButtons);
                var text = $"<b>{cat?.Title ?? "Category"} > {sub?.Title ?? "Subcategory"}</b>\n\nChoose an article:";
                var editRequest = new EditMessageTextRequest
                {
                    ChatId = chatId,
                    MessageId = messageId,
                    Text = text,
                    ParseMode = ParseMode.Html,
                    ReplyMarkup = keyboard
                };
                try
                {
                    await botClient.SendRequest(editRequest, cancellationToken);
                }
                catch (ApiRequestException ex) when (ex.ErrorCode == 400 && ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("EditMessageTextRequest: message is not modified for chat {ChatId}, message {MessageId}", chatId, messageId);
                }
            }
            else if (callbackQuery.Data.StartsWith("show_art:"))
            {
                var parts = callbackQuery.Data.Split(':');
                if (parts.Length < 2)
                    return;

                var hash = parts[1];
                var fromSearchVisible = parts.Length >= 3 && int.TryParse(parts[2], out var v) ? v : (int?)null;

                if (!_slugHashMap.TryGetValue(hash, out var slug))
                {
                    _logger.LogWarning("No article slug found for callback hash: {Hash}", hash);
                    return;
                }
                _logger.LogInformation("CallbackQuery received for slug: {Slug} in chat {ChatId}", slug, chatId);
                var article = await _kbService.GetBySlugAsync(slug, cancellationToken);
                if (article is not null)
                {
                    try
                    {
                        await botClient.SendRequest(new DeleteMessageRequest { ChatId = chatId, MessageId = messageId }, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to delete previous message");
                    }
                    _logger.LogInformation("Sending article for slug {Slug} to chat {ChatId}", slug, chatId);
                    var breadcrumbs = $"<b>{article.Category.Title} > {article.Subcategory.Title}</b>\n\n";
                    var content = article.Content.Length > 3800 ? article.Content.Substring(0, 3800) + "... (read more on website)" : article.Content;
                    var response = breadcrumbs + $"<b>{EscapeHtml(article.Title)}</b>\n\n{EscapeHtml(content)}";
                    // Feedback buttons use hash for callback_data
                    var likeBtn = InlineKeyboardButton.WithCallbackData("👍 Helpful", $"rate:up:{hash}");
                    var dislikeBtn = InlineKeyboardButton.WithCallbackData("👎 Not helpful", $"rate:down:{hash}");
                    // Back to list (subcategory) with hash
                    var subSlug = article.Subcategory.Slug;
                    var subHash = GetSlugHash(subSlug);
                    _slugHashMap[subHash] = subSlug;
                    InlineKeyboardButton backBtn;
                    if (fromSearchVisible.HasValue && _searchState.ContainsKey(chatId))
                        backBtn = InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"search:{fromSearchVisible.Value}");
                    else
                        backBtn = InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"sub:{subHash}");
                    // Main Menu
                    var mainMenuBtn = InlineKeyboardButton.WithCallbackData("🏠 Main Menu", "main_menu");
                    var markup = new InlineKeyboardMarkup(new[] {
                        new[] { likeBtn, dislikeBtn },
                        new[] { backBtn },
                        new[] { mainMenuBtn }
                    });
                    var sendRequest = new SendMessageRequest
                    {
                        ChatId = chatId,
                        Text = response,
                        ParseMode = ParseMode.Html,
                        ReplyMarkup = markup
                    };
                    await botClient.SendRequest(sendRequest, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Article not found for slug {Slug} in chat {ChatId}", slug, chatId);
                    var notFoundRequest = new SendMessageRequest
                    {
                        ChatId = chatId,
                        Text = "Sorry, article not found."
                    };
                    await botClient.SendRequest(notFoundRequest, cancellationToken);
                }
            }
            else if (callbackQuery.Data.StartsWith("rate:"))
            {
                // Parse rating and hash
                var parts = callbackQuery.Data.Split(':');
                if (parts.Length == 3)
                {
                    var rating = parts[1] == "up" ? "up" : "down";
                    var hash = parts[2];
                    if (!_slugHashMap.TryGetValue(hash, out var slug))
                    {
                        _logger.LogWarning("No article slug found for feedback hash: {Hash}", hash);
                        return;
                    }
                    _logger.LogInformation("USER_FEEDBACK: Article {Slug} rated as {Rating}", slug, rating);
                    // Thank you message
                    try
                    {
                        var answerRequest = new AnswerCallbackQueryRequest { CallbackQueryId = callbackQuery.Id, Text = "Спасибо за ваш отзыв!" };
                        await botClient.SendRequest(answerRequest, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to answer callback query");
                    }
                    // Update UI: replace feedback buttons with confirmation and Back/Main Menu
                    var confirmBtn = InlineKeyboardButton.WithCallbackData("✅ Отзыв принят", "ignore");
                    // Back to list (subcategory)
                    var subSlug = string.Empty;
                    if (_slugHashMap.TryGetValue(hash, out var ratedSlug))
                    {
                        // Try to get subcategory slug from the article (if available)
                        var ratedArticle = await _kbService.GetBySlugAsync(ratedSlug, cancellationToken);
                        if (ratedArticle?.Subcategory?.Slug is string s)
                            subSlug = s;
                    }
                    var subHash = !string.IsNullOrEmpty(subSlug) ? GetSlugHash(subSlug) : string.Empty;
                    if (!string.IsNullOrEmpty(subHash)) _slugHashMap[subHash] = subSlug;
                    var backToListBtn = !string.IsNullOrEmpty(subHash) ? InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"sub:{subHash}") : null;
                    var mainMenuBtn = InlineKeyboardButton.WithCallbackData("🏠 Main Menu", "main_menu");
                    var rows = new List<InlineKeyboardButton[]> { new[] { confirmBtn } };
                    if (backToListBtn != null) rows.Add(new[] { backToListBtn });
                    rows.Add(new[] { mainMenuBtn });
                    var markup = new InlineKeyboardMarkup(rows);
                    try
                    {
                        var editMarkupRequest = new EditMessageReplyMarkupRequest
                        {
                            ChatId = callbackQuery.Message!.Chat.Id,
                            MessageId = callbackQuery.Message.MessageId,
                            ReplyMarkup = markup
                        };
                        await botClient.SendRequest(editMarkupRequest, cancellationToken);
                    }
                    catch (ApiRequestException ex) when (ex.ErrorCode == 400 && ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("EditMessageReplyMarkupRequest: message is not modified for chat {ChatId}, message {MessageId}", callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId);
                    }
                }
            }
        }
        finally
        {
            // Always answer the callback to remove the spinner
            if (callbackQuery.Id != null)
            {
                try
                {
                    var answerRequest = new AnswerCallbackQueryRequest { CallbackQueryId = callbackQuery.Id };
                    await botClient.SendRequest(answerRequest, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to answer callback query");
                }
            }
        }
    }

    /// <summary>
    /// Sends a formatted article to the specified chat.
    /// Escapes HTML and respects Telegram message length limits.
    /// </summary>
    /// <param name="botClient">The Telegram bot client.</param>
    /// <param name="chatId">The chat ID to send the article to.</param>
    /// <param name="article">The article to send.</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    private async Task SendArticleAsync(ITelegramBotClient botClient, long chatId, Article article, CancellationToken cancellationToken)
    {
        var content = article.Content;
        if (content.Length > 3800)
        {
            content = content.Substring(0, 3800) + "... (read more on website)";
        }
        var response = $"<b>{EscapeHtml(article.Title)}</b>\n\n{EscapeHtml(content)}";
        _logger.LogInformation("Sending article '{Title}' to chat {ChatId}", article.Title, chatId);
        var sendRequest = new SendMessageRequest
        {
            ChatId = chatId,
            Text = response,
            ParseMode = ParseMode.Html
        };
        await botClient.SendRequest(sendRequest, cancellationToken);
    }

    /// <summary>
    /// Handles errors that occur during polling.
    /// Logs the exception and source.
    /// </summary>
    /// <param name="botClient">The Telegram bot client.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="source">The source of the error.</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>A completed task.</returns>
    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Polling error occurred");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Escapes HTML special characters in the given text for safe Telegram message formatting.
    /// </summary>
    /// <param name="text">The input text to escape.</param>
    /// <returns>The escaped HTML string.</returns>
    private static string EscapeHtml(string text)
        => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
