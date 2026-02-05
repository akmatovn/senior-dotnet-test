using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using YouDoFaqBot.Core.Handlers;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Services;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Tests.Handlers;

public class ArticleHandlerTests
{
    [Fact]
    public async Task ArticleHandler_FromSearch_EditMessage_And_Back_Targets_SearchRestore()
    {
        var kb = new Mock<IKnowledgeBaseService>();
        var article = new Article("T", "C", "slug-a", new Category("c", "Cat", null), new Subcategory("s", "Sub", null));
        kb.Setup(k => k.GetBySlugAsync("slug-a", It.IsAny<CancellationToken>())).ReturnsAsync(article);

        var slug = new SlugMappingService();
        var publisher = new Mock<IBotResponsePublisher>();
        BotResponse? resp = null;
        publisher.Setup(p => p.PublishAsync(It.IsAny<CallbackContext>(), It.IsAny<BotResponse>(), It.IsAny<CancellationToken>()))
            .Callback<CallbackContext, BotResponse, CancellationToken>((c, r, t) => resp = r)
            .Returns(Task.CompletedTask);
        publisher.Setup(p => p.AnswerCallbackAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var h = new ArticleHandler(kb.Object, slug, publisher.Object, NullLogger<ArticleHandler>.Instance);
        var articleHash = slug.GetOrCreateSlug("slug-a");
        var payload = CallbackPrefixes.ShowArticle + articleHash + CallbackPrefixes.Separator + "search" + CallbackPrefixes.Separator + 6;

        await h.HandleAsync(new CallbackContext(10, 200, "cbq"), payload, CancellationToken.None);

        resp.Should().NotBeNull();
        resp!.EditMessage.Should().BeTrue();
        resp.HtmlText.Should().Contain("<b>T</b>");
        // back button should point to search restore
        var back = resp.InlineKeyboard!.SelectMany(r => r).FirstOrDefault(b => b.Text.Contains("Back"));
        back.CallbackData.Should().StartWith(CallbackPrefixes.SearchRestore);
    }
}
