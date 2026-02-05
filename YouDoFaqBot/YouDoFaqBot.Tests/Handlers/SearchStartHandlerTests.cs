using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using YouDoFaqBot.Core.Handlers;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Utils;
using Xunit;
using System.Linq;

namespace YouDoFaqBot.Tests.Handlers;

public class SearchStartHandlerTests
{
    [Fact]
    public void CanHandle_ReturnsTrue_ForSearchStartPrefix()
    {
        var handler = new SearchStartHandler(new Mock<ISearchModeService>().Object, new Mock<IBotResponsePublisher>().Object, new NullLogger<SearchStartHandler>());

        handler.CanHandle(CallbackPrefixes.SearchStart).Should().BeTrue();
        handler.CanHandle("something_else").Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_SetsSearchMode_AndPublishesPrompt_AndAnswersCallback()
    {
        // arrange
        var searchMode = new Mock<ISearchModeService>(MockBehavior.Strict);
        searchMode.Setup(s => s.Set(It.IsAny<long>(), true)).Verifiable();

        var publisher = new Mock<IBotResponsePublisher>(MockBehavior.Strict);
        BotResponse? published = null;
        publisher.Setup(p => p.PublishAsync(It.IsAny<CallbackContext>(), It.IsAny<BotResponse>(), It.IsAny<CancellationToken>()))
            .Callback<CallbackContext, BotResponse, CancellationToken>((c, r, t) => published = r)
            .Returns(Task.CompletedTask)
            .Verifiable();
        publisher.Setup(p => p.AnswerCallbackAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var logger = new NullLogger<SearchStartHandler>();
        var handler = new SearchStartHandler(searchMode.Object, publisher.Object, logger);

        var context = new CallbackContext(123, null, "cb-query-id");

        // act
        await handler.HandleAsync(context, CallbackPrefixes.SearchStart, CancellationToken.None);

        // assert
        searchMode.Verify(s => s.Set(123, true), Times.Once);
        published.Should().NotBeNull();
        published!.HtmlText.Should().Be(BotMessages.SearchPrompt);
        // keyboard must contain Main Menu button
        published.InlineKeyboard.Should().NotBeNull();
        var flat = published.InlineKeyboard!.SelectMany(r => r).ToList();
        flat.Should().Contain(b => b.CallbackData == CallbackPrefixes.MainMenu);

        publisher.Verify(p => p.AnswerCallbackAsync("cb-query-id", null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
