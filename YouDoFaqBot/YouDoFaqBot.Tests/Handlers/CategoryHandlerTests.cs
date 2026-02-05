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
using YouDoFaqBot.Core.Utils;
using Xunit;

namespace YouDoFaqBot.Tests.Handlers;

public class CategoryHandlerTests
{
    [Fact]
    public async Task CategoryHandler_Publishes_Subcategories()
    {
        var kb = new Mock<IKnowledgeBaseService>();
        kb.Setup(k => k.GetSubcategoriesByCategoryAsync("cat-slug", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Subcategory> { new Subcategory("s","Sub", null) });

        var slug = new SlugMappingService();
        var publisher = new Mock<IBotResponsePublisher>();
        BotResponse? resp = null;
        publisher.Setup(p => p.PublishAsync(It.IsAny<CallbackContext>(), It.IsAny<BotResponse>(), It.IsAny<CancellationToken>()))
            .Callback<CallbackContext, BotResponse, CancellationToken>((c, r, t) => resp = r)
            .Returns(Task.CompletedTask);
        publisher.Setup(p => p.AnswerCallbackAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var h = new CategoryHandler(kb.Object, slug, publisher.Object, NullLogger<CategoryHandler>.Instance);
        await h.HandleAsync(new CallbackContext(1, 1, "cbq"), CallbackPrefixes.Category + "cat-slug", CancellationToken.None);

        resp.Should().NotBeNull();
        resp!.InlineKeyboard.Should().NotBeNull();
        resp.InlineKeyboard!.SelectMany(r => r).Any(b => b.CallbackData.StartsWith(CallbackPrefixes.Subcategory)).Should().BeTrue();
    }
}
