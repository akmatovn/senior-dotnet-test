using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using YouDoFaqBot.Core.Handlers;
using YouDoFaqBot.Core.Services;
using Xunit;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Utils;
using YouDoFaqBot.Core.Models;

namespace YouDoFaqBot.Tests.Handlers;

public class RatingHandlerTests
{
    [Fact]
    public async Task RatingHandler_Edits_Markup_And_Answers()
    {
        var slug = new SlugMappingService();
        var publisher = new Mock<IBotResponsePublisher>();
        publisher.Setup(p => p.EditReplyMarkupAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<IEnumerable<IEnumerable<(string, string)>>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        publisher.Setup(p => p.AnswerCallbackAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

        var h = new RatingHandler(slug, publisher.Object, NullLogger<RatingHandler>.Instance);
        var articleHash = slug.GetOrCreateSlug("slug-a");
        var payload = CallbackPrefixes.Rate + "up:" + articleHash + ":subhash";

        await h.HandleAsync(new CallbackContext(1, 123, "cbq"), payload, CancellationToken.None);

        publisher.Verify(p => p.AnswerCallbackAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
