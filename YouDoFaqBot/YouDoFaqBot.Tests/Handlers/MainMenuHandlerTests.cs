using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Services;
using YouDoFaqBot.Core.Handlers;
using YouDoFaqBot.Core.Handlers.MessageHandlers;
using YouDoFaqBot.Core.Utils;
using Xunit;

namespace YouDoFaqBot.Tests.Handlers;

public class MainMenuHandlerTests
{
    [Fact]
    public async Task MainMenuHandler_Publishes_Categories()
    {
        var kb = new Mock<IKnowledgeBaseService>();
        kb.Setup(k => k.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Category> { new Category("s","T", null) });

        var publisher = new Mock<IBotResponsePublisher>();
        BotResponse? resp = null;
        publisher.Setup(p => p.PublishAsync(It.IsAny<CallbackContext>(), It.IsAny<BotResponse>(), It.IsAny<CancellationToken>()))
            .Callback<CallbackContext, BotResponse, CancellationToken>((c, r, t) => resp = r)
            .Returns(Task.CompletedTask);
        publisher.Setup(p => p.AnswerCallbackAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var h = new MainMenuHandler(kb.Object, publisher.Object, NullLogger<MainMenuHandler>.Instance);
        await h.HandleAsync(new CallbackContext(1, 1, "cbq"), CallbackPrefixes.MainMenu, CancellationToken.None);

        resp.Should().NotBeNull();
        resp!.InlineKeyboard.Should().NotBeNull();
        resp.InlineKeyboard!.SelectMany(r => r).Any(b => b.CallbackData.StartsWith(CallbackPrefixes.Category)).Should().BeTrue();
    }
}
