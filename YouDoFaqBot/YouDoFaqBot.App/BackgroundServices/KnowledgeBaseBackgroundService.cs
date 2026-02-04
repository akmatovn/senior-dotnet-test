using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YouDoFaqBot.Core.Interfaces;

namespace YouDoFaqBot.App.BackgroundServices;

/// <summary>
/// Background service that loads the knowledge base from a JSON file at application startup.
/// Ensures the knowledge base is available as a singleton for fast access throughout the bot's lifetime.
/// </summary>
public class KnowledgeBaseBackgroundService(
    IKnowledgeBaseService kbService,
    ILogger<KnowledgeBaseBackgroundService> logger)
    : BackgroundService
{
    private readonly IKnowledgeBaseService _kbService = kbService;
    private readonly ILogger<KnowledgeBaseBackgroundService> _logger = logger;

    /// <summary>
    /// Executes the background service logic to load the knowledge base.
    /// This method is called when the host starts.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token for graceful shutdown.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KnowledgeBaseHostedService starting...");
        try
        {
            await _kbService.LoadFromFileAsync("knowledge_base.json", stoppingToken);
            _logger.LogInformation("Knowledge base loaded successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to load knowledge base.");
            throw;
        }
    }
}
