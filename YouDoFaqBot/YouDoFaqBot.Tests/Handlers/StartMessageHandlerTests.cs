using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using YouDoFaqBot.Core.Handlers.MessageHandlers;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Tests.Handlers;

public class StartMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_Start_Publishes_Welcome_With_BrowseButton()
    {
        var publisher = new Mock<IBotResponsePublisher>();
        BotResponse? published = null;
        publisher.Setup(p => p.PublishAsync(It.IsAny<CallbackContext>(), It.IsAny<BotResponse>(), It.IsAny<CancellationToken>()))
            .Callback<CallbackContext, BotResponse, CancellationToken>((c, r, t) => published = r)
            .Returns(Task.CompletedTask);

        var handler = new StartMessageHandler(publisher.Object, new Mock<ILogger<StartMessageHandler>>().Object);

        await handler.HandleAsync(new MessageContext(1, null), Commands.Start, CancellationToken.None);

        published.Should().NotBeNull();
        published!.InlineKeyboard.Should().NotBeNull();
        // check browse button present
        published.InlineKeyboard!.SelectMany(r => r).Should().Contain(b => b.Text.Contains("Browse FAQ") && b.CallbackData == CallbackPrefixes.MainMenu);
    }

    [Fact]
    public async Task HandleAsync_BrowseFaqButton_Publishes_OpenMainMenu_Button()
    {
        var publisher = new Mock<IBotResponsePublisher>();
        BotResponse? published = null;
        publisher.Setup(p => p.PublishAsync(It.IsAny<CallbackContext>(), It.IsAny<BotResponse>(), It.IsAny<CancellationToken>()))
            .Callback<CallbackContext, BotResponse, CancellationToken>((c, r, t) => published = r)
            .Returns(Task.CompletedTask);

        var handler = new StartMessageHandler(publisher.Object, new Mock<ILogger<StartMessageHandler>>().Object);

        await handler.HandleAsync(new MessageContext(1, null), UiTexts.BrowseFaqButton, CancellationToken.None);

        published.Should().NotBeNull();
        published!.InlineKeyboard.Should().NotBeNull();
        published.InlineKeyboard!.SelectMany(r => r).Should().Contain(b => b.CallbackData == CallbackPrefixes.MainMenu);
    }
}
