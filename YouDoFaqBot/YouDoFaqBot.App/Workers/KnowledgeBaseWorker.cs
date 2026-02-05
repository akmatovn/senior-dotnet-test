using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Settings;

namespace YouDoFaqBot.App.Workers;

/// <summary>
/// Background service that loads the knowledge base from a JSON file at application startup.
/// Ensures the knowledge base is available as a singleton for fast access throughout the bot's lifetime.
/// </summary>
/// <param name="kbService">The service responsible for managing the knowledge base.</param>
/// <param name="options">The options containing the knowledge base file path.</param>
/// <param name="logger">The logger instance for logging events and errors.</param>
public class KnowledgeBaseWorker(
    IKnowledgeBaseService kbService,
    IOptions<KnowledgeBaseOptions> options,
    ILogger<KnowledgeBaseWorker> logger)
    : BackgroundService
{
    private readonly IKnowledgeBaseService _kbService = kbService;
    private readonly KnowledgeBaseOptions _options = options.Value;
    private readonly ILogger<KnowledgeBaseWorker> _logger = logger;

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
            await _kbService.LoadFromFileAsync(_options.FilePath, stoppingToken);
            _logger.LogInformation("Knowledge base loaded successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to load knowledge base.");
            throw;
        }
    }
}
