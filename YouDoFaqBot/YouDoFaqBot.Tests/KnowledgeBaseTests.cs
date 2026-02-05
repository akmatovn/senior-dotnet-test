using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;
using YouDoFaqBot.Core.Models;
using YouDoFaqBot.Core.Services;
using YouDoFaqBot.Core.Settings;

namespace YouDoFaqBot.Tests;

public class KnowledgeBaseTests
{
    [Fact]
    public async Task SearchAsync_Should_Be_CaseInsensitive_And_By_Relevance()
    {
        var svc = new KnowledgeBaseService(
            NullLogger<KnowledgeBaseService>.Instance,
            Options.Create(new KnowledgeBaseOptions())
        );

        var category = new Category("c", "Cat", null);
        var subcategory = new Subcategory("s", "Sub", null);
        var kb = new KnowledgeBase(
        [
            new Article("Pay by Card", "Details...", "a", category, subcategory),
            new Article("Other", "pay by card is possible", "b", category, subcategory),
        ]);

        var json = JsonSerializer.Serialize(kb);
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, json);

        await svc.LoadFromFileAsync(path, CancellationToken.None);

        var results = await svc.SearchAsync("PAY BY CARD", CancellationToken.None, limit: null);

        results.Should().HaveCount(2);
        results[0].Slug.Should().Be("a"); // title match scores higher
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_Should_Return_Empty()
    {
        var svc = new KnowledgeBaseService(
            NullLogger<KnowledgeBaseService>.Instance,
            Options.Create(new KnowledgeBaseOptions())
        );

        var kb = new KnowledgeBase([]);
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(kb));
        await svc.LoadFromFileAsync(path, CancellationToken.None);

        var results = await svc.SearchAsync(" ", CancellationToken.None, limit: null);
        results.Should().BeEmpty();
    }
}
