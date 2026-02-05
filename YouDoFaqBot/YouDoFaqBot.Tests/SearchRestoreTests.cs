using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using YouDoFaqBot.Core.Handlers;
using YouDoFaqBot.Core.Handlers.MessageHandlers;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Services;
using YouDoFaqBot.Core.Settings;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Tests;

public class SearchRestoreTests
{
    [Fact]
    public async Task Search_OpenArticle_Back_RestoresSearchResults()
    {
        // arrange
        var kb = new Mock<IKnowledgeBaseService>();
        var articles = new List<Article>
        {
            new Article("Title1", "Content1", "slug1", new Category("c","Cat", null), new Subcategory("s","Sub", null)),
            new Article("Title2", "Content2", "slug2", new Category("c","Cat", null), new Subcategory("s","Sub", null)),
            new Article("Title3", "Content3", "slug3", new Category("c","Cat", null), new Subcategory("s","Sub", null)),
        };
        kb.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync((string q, CancellationToken ct, int? l) => (IReadOnlyList<Article>)articles);
        kb.Setup(x => x.GetBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string s, CancellationToken ct) => articles.FirstOrDefault(a => a.Slug == s));

        var slugService = new SlugMappingService();
        var searchState = new SearchStateService();

        var published = new List<(CallbackContext Context, BotResponse Response)>();
        var publisher = new Mock<IBotResponsePublisher>();
        publisher.Setup(p => p.PublishAsync(It.IsAny<CallbackContext>(), It.IsAny<BotResponse>(), It.IsAny<CancellationToken>()))
            .Callback<CallbackContext, BotResponse, CancellationToken>((ctx, resp, ct) => published.Add((ctx, resp)))
            .Returns(Task.CompletedTask);
        publisher.Setup(p => p.AnswerCallbackAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new Mock<IOptions<KnowledgeBaseOptions>>();
        options.Setup(o => o.Value).Returns(new KnowledgeBaseOptions
        {
            SearchPageSize = 5
        });

        var searchMode = new SearchModeService();
        var searchHandler = new SearchMessageHandler(kb.Object, slugService, searchState, searchMode: searchMode, publisher.Object, NullLogger<SearchMessageHandler>.Instance, options.Object);
        var articleHandler = new ArticleHandler(kb.Object, slugService, publisher.Object, NullLogger<ArticleHandler>.Instance);
        var restoreHandler = new SearchRestoreHandler(kb.Object, slugService, searchState, publisher.Object, NullLogger<SearchRestoreHandler>.Instance);

        var chatId = 1L;
        var query = "test";

        // act: perform search
        await searchHandler.HandleAsync(new MessageContext(chatId, null), query, CancellationToken.None);

        // simulate message id that Telegram would attach to callback message
        var messageId = 100;

        // pick an article and open it as from search
        var articleHash = slugService.GetOrCreateSlug(articles[0].Slug);
        var visible = 6;
        var showPayload = CallbackPrefixes.ShowArticle + articleHash + CallbackPrefixes.Separator + "search" + CallbackPrefixes.Separator + visible;

        await articleHandler.HandleAsync(new CallbackContext(chatId, messageId, "cbq"), showPayload, CancellationToken.None);

        // now press back (restore)
        var restorePayload = CallbackPrefixes.SearchRestore + visible;
        await restoreHandler.HandleAsync(new CallbackContext(chatId, messageId, "cbq2"), restorePayload, CancellationToken.None);

        // assert: last publish should be the restored search results and be an edit
        published.Should().NotBeEmpty();
        var last = published.Last();
        last.Context.ChatId.Should().Be(chatId);
        last.Response.Should().NotBeNull();
        last.Response.EditMessage.Should().BeTrue();
        last.Response.InlineKeyboard.Should().NotBeNull();

        // first keyboard row should contain a show_art callback
        var firstRow = last.Response.InlineKeyboard!.FirstOrDefault();
        firstRow.Should().NotBeNull();
        firstRow.Any(b => b.CallbackData.StartsWith(CallbackPrefixes.ShowArticle)).Should().BeTrue();
    }
}
