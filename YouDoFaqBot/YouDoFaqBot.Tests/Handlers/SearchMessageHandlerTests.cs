using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using YouDoFaqBot.Core.Handlers.MessageHandlers;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Services;
using YouDoFaqBot.Core.Settings;

namespace YouDoFaqBot.Tests.Handlers;

public class SearchMessageHandlerTests
{
    [Fact]
    public async Task SearchMessageHandler_NoResults_Publishes_NotFound()
    {
        var kb = new Mock<IKnowledgeBaseService>();
        kb.Setup(k => k.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int?>())).ReturnsAsync(new List<Article>());

        var slug = new SlugMappingService();
        var searchState = new SearchStateService();
        var publisher = new Mock<IBotResponsePublisher>();
        BotResponse? resp = null;
        publisher.Setup(p => p.PublishAsync(It.IsAny<CallbackContext>(), It.IsAny<BotResponse>(), It.IsAny<CancellationToken>()))
            .Callback<CallbackContext, BotResponse, CancellationToken>((c, r, t) => resp = r)
            .Returns(Task.CompletedTask);
        publisher.Setup(p => p.AnswerCallbackAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var options = new Mock<IOptions<KnowledgeBaseOptions>>();
        options.Setup(o => o.Value).Returns(new KnowledgeBaseOptions
        {
            SearchPageSize = 5
        });

        var searchMode = new SearchModeService();
        var h = new SearchMessageHandler(kb.Object, slug, searchState, searchMode, publisher.Object, NullLogger<SearchMessageHandler>.Instance, options.Object);
        await h.HandleAsync(new MessageContext(1, null), "q", CancellationToken.None);

        resp.Should().NotBeNull();
        resp!.HtmlText.Should().Contain("Sorry");
    }
}
