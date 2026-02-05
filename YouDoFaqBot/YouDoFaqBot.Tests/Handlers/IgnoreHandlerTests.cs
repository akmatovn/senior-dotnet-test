using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using YouDoFaqBot.Core.Handlers;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Utils;

namespace YouDoFaqBot.Tests.Handlers;

public class IgnoreHandlerTests
{
    [Fact]
    public async Task IgnoreHandler_Answers_Callback()
    {
        var publisher = new Mock<IBotResponsePublisher>();
        publisher.Setup(p => p.AnswerCallbackAsync("cbq", null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

        var h = new IgnoreHandler(publisher.Object, NullLogger<IgnoreHandler>.Instance);
        await h.HandleAsync(new CallbackContext(1, null, "cbq"), CallbackPrefixes.Ignore, CancellationToken.None);

        publisher.Verify();
    }
}
