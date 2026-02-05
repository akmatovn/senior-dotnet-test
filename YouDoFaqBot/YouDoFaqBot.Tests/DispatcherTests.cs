using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using YouDoFaqBot.Core.Handlers;
using Xunit;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;

namespace YouDoFaqBot.Tests;

public class DispatcherTests
{
    [Fact]
    public async Task DispatchAsync_Callback_Should_Invoke_Matching_Handler()
    {
        var handler = new Mock<ICallbackHandler>(MockBehavior.Strict);
        handler.Setup(h => h.CanHandle("main_menu")).Returns(true);
        handler.Setup(h => h.HandleAsync(It.IsAny<CallbackContext>(), "main_menu", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

        var dispatcher = new UpdateDispatcher(
            NullLogger<UpdateDispatcher>.Instance,
            new[] { handler.Object },
            []);

        await dispatcher.DispatchCallbackAsync(new CallbackContext(1, 1, "cbq"), "main_menu", CancellationToken.None);

        handler.Verify();
    }

    [Fact]
    public async Task DispatchAsync_Callback_Should_Not_Invoke_When_No_Handler()
    {
        var handler = new Mock<ICallbackHandler>(MockBehavior.Strict);
        handler.Setup(h => h.CanHandle(It.IsAny<string>())).Returns(false);

        var dispatcher = new UpdateDispatcher(
            NullLogger<UpdateDispatcher>.Instance,
            new[] { handler.Object },
            []);

        await dispatcher.DispatchCallbackAsync(new CallbackContext(1, 1, "cbq"), "unknown", CancellationToken.None);

        handler.Verify(h => h.HandleAsync(It.IsAny<CallbackContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
