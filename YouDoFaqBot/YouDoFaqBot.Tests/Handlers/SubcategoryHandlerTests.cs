using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using YouDoFaqBot.Core.Handlers;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Services;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Tests.Handlers;

public class SubcategoryHandlerTests
{
    [Fact]
    public async Task SubcategoryHandler_Publishes_Articles()
    {
        var kb = new Mock<IKnowledgeBaseService>();
        var art = new Article("T", "C", "slug-a", new Category("c", "Cat", null), new Subcategory("s", "Sub", null));
        kb.Setup(k => k.GetArticlesBySubcategoryAsync("sub-slug", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Article> { art });

        var slug = new SlugMappingService();
        var publisher = new Mock<IBotResponsePublisher>();
        BotResponse? resp = null;
        publisher.Setup(p => p.PublishAsync(It.IsAny<CallbackContext>(), It.IsAny<BotResponse>(), It.IsAny<CancellationToken>()))
            .Callback<CallbackContext, BotResponse, CancellationToken>((c, r, t) => resp = r)
            .Returns(Task.CompletedTask);
        publisher.Setup(p => p.AnswerCallbackAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var h = new SubcategoryHandler(kb.Object, slug, publisher.Object, NullLogger<SubcategoryHandler>.Instance);
        // use hashed sub slug
        var subHash = slug.GetOrCreateSlug("sub-slug");
        await h.HandleAsync(new CallbackContext(1, 1, "cbq"), CallbackPrefixes.Subcategory + subHash + ":catHash", CancellationToken.None);

        resp.Should().NotBeNull();
        resp!.InlineKeyboard.Should().NotBeNull();
        resp.InlineKeyboard!.SelectMany(r => r).Any(b => b.CallbackData.StartsWith(CallbackPrefixes.ShowArticle)).Should().BeTrue();
    }
}
