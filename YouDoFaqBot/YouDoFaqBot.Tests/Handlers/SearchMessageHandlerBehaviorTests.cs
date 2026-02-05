using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using YouDoFaqBot.Core.Handlers.MessageHandlers;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Services;
using YouDoFaqBot.Core.Settings;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Tests.Handlers;

public class SearchMessageHandlerBehaviorTests
{
    [Fact]
    public async Task HandleAsync_WithResults_PublishesSearchResults_WithMoreAndMainMenu()
    {
        // arrange
        var kb = new Mock<IKnowledgeBaseService>();
        var articles = new List<Article>();
        for (int i = 0; i < 10; i++)
            articles.Add(new Article($"T{i}", $"C{i}", $"slug{i}", new Category("c", "Cat", null), new Subcategory("s", "Sub", null)));

        kb.Setup(k => k.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync((string q, CancellationToken ct, int? l) => (IReadOnlyList<Article>)articles);

        var slug = new SlugMappingService();
        var searchState = new SearchStateService();

        BotResponse? published = null;
        var publisher = new Mock<IBotResponsePublisher>();
        publisher.Setup(p => p.PublishAsync(It.IsAny<CallbackContext>(), It.IsAny<BotResponse>(), It.IsAny<CancellationToken>()))
            .Callback<CallbackContext, BotResponse, CancellationToken>((c, r, t) => published = r)
            .Returns(Task.CompletedTask);
        publisher.Setup(p => p.AnswerCallbackAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var options = new Mock<IOptions<KnowledgeBaseOptions>>();
        options.Setup(o => o.Value).Returns(new KnowledgeBaseOptions
        {
            SearchPageSize = 5
        });

        var searchMode = new SearchModeService();
        var handler = new SearchMessageHandler(kb.Object, slug, searchState, searchMode, publisher.Object, NullLogger<SearchMessageHandler>.Instance, options.Object);

        // act
        await handler.HandleAsync(new MessageContext(1, null), "query", CancellationToken.None);

        // assert
        published.Should().NotBeNull();
        published!.EditMessage.Should().BeFalse(); // since new message
        published.InlineKeyboard.Should().NotBeNull();

        var flat = published.InlineKeyboard!.SelectMany(r => r).ToList();
        flat.Any(b => b.CallbackData.StartsWith(CallbackPrefixes.ShowArticle)).Should().BeTrue();
        flat.Any(b => b.CallbackData == CallbackPrefixes.SearchMore).Should().BeTrue();
        flat.Any(b => b.CallbackData == CallbackPrefixes.MainMenu).Should().BeTrue();

        // search state set
        searchState.TryGet(1, out var state).Should().BeTrue();
        state.Query.Should().Be("query");
    }

    [Fact]
    public async Task HandleAsync_WithFewResults_NoMoreButton()
    {
        var kb = new Mock<IKnowledgeBaseService>();
        var articles = new List<Article>();
        for (int i = 0; i < 3; i++)
            articles.Add(new Article($"T{i}", $"C{i}", $"slug{i}", new Category("c", "Cat", null), new Subcategory("s", "Sub", null)));

        kb.Setup(k => k.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync((string q, CancellationToken ct, int? l) => (IReadOnlyList<Article>)articles);

        var slug = new SlugMappingService();
        var searchState = new SearchStateService();

        BotResponse? published = null;
        var publisher = new Mock<IBotResponsePublisher>();
        publisher.Setup(p => p.PublishAsync(It.IsAny<CallbackContext>(), It.IsAny<BotResponse>(), It.IsAny<CancellationToken>()))
            .Callback<CallbackContext, BotResponse, CancellationToken>((c, r, t) => published = r)
            .Returns(Task.CompletedTask);
        publisher.Setup(p => p.AnswerCallbackAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var options = new Mock<IOptions<KnowledgeBaseOptions>>();
        options.Setup(o => o.Value).Returns(new KnowledgeBaseOptions
        {
            SearchPageSize = 5
        });

        var searchMode = new SearchModeService();
        var handler = new SearchMessageHandler(kb.Object, slug, searchState, searchMode, publisher.Object, NullLogger<SearchMessageHandler>.Instance, options.Object);
        await handler.HandleAsync(new MessageContext(1, null), "q", CancellationToken.None);

        published.Should().NotBeNull();
        var flat = published!.InlineKeyboard!.SelectMany(r => r).ToList();
        flat.Any(b => b.CallbackData == CallbackPrefixes.SearchMore).Should().BeFalse();
        flat.Any(b => b.CallbackData == CallbackPrefixes.MainMenu).Should().BeTrue();
    }
}
