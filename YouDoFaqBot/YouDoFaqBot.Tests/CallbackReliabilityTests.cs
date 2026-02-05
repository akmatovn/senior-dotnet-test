using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using YouDoFaqBot.Core.Handlers;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Services;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Tests;

public class CallbackReliabilityTests
{
    [Fact]
    public async Task MainMenuHandler_Should_Always_Answer_Callback_Even_On_Exception()
    {
        var kb = new Mock<IKnowledgeBaseService>();
        kb.Setup(x => x.GetCategoriesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));

        var publisher = new Mock<IBotResponsePublisher>();

        publisher
            .Setup(p => p.AnswerCallbackAsync("cbq", null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var handler = new MainMenuHandler(kb.Object, publisher.Object, NullLogger<MainMenuHandler>.Instance);

        var act = async () => await handler.HandleAsync(new CallbackContext(1, 1, "cbq"), CallbackPrefixes.MainMenu, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        publisher.Verify();
    }

    [Fact]
    public async Task ArticleHandler_BackButton_Should_Target_Subcategory_When_Provided()
    {
        var kb = new Mock<IKnowledgeBaseService>();
        kb.Setup(x => x.GetBySlugAsync("article-slug", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Article(
                Title: "T",
                Content: "C",
                Slug: "article-slug",
                Category: new Category("c", "cat", null),
                Subcategory: new Subcategory("s", "sub", null)));

        var slug = new SlugMappingService();
        var articleHash = slug.GetOrCreateSlug("article-slug");
        var subHash = slug.GetOrCreateSlug("sub-slug");

        BotResponse? published = null;
        var publisher = new Mock<IBotResponsePublisher>();
        publisher.Setup(p => p.PublishAsync(It.IsAny<CallbackContext>(), It.IsAny<BotResponse>(), It.IsAny<CancellationToken>()))
            .Callback<CallbackContext, BotResponse, CancellationToken>((_, r, _) => published = r)
            .Returns(Task.CompletedTask);
        publisher.Setup(p => p.AnswerCallbackAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ArticleHandler(kb.Object, slug, publisher.Object, NullLogger<ArticleHandler>.Instance);

        await handler.HandleAsync(new CallbackContext(1, 1, "cbq"), $"{CallbackPrefixes.ShowArticle}{articleHash}{CallbackPrefixes.Separator}{subHash}", CancellationToken.None);

        published.Should().NotBeNull();
        var backRow = published!.InlineKeyboard!.Skip(1).First();
        var back = backRow.First();
        back.CallbackData.Should().Be(CallbackPrefixes.Subcategory + subHash);
    }
}
