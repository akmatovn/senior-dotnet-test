using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YouDoFaqBot.Interfaces;

namespace YouDoFaqBot.BackgroundServices;

/// <summary>
/// Hosted service responsible for loading the knowledge base from a JSON file at application startup.
/// Ensures the knowledge base is available as a singleton for fast access throughout the bot's lifetime.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KnowledgeBaseHostedService"/> class.
/// </remarks>
/// <param name="kbService">The knowledge base service to load data into.</param>
/// <param name="logger">Logger for reporting status and errors.</param>
public class KnowledgeBaseHostedService(
    IKnowledgeBaseService kbService,
    ILogger<KnowledgeBaseHostedService> logger)
    : IHostedService
{
    private readonly IKnowledgeBaseService _kbService = kbService;
    private readonly ILogger<KnowledgeBaseHostedService> _logger = logger;

    /// <summary>
    /// Loads the knowledge base from the JSON file when the host starts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _kbService.LoadFromFileAsync("knowledge_base.json", cancellationToken);
            _logger.LogInformation("Knowledge base loaded successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to load knowledge base.");
            throw;
        }
    }

    /// <summary>
    /// Called when the host is performing a graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for shutdown.</param>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
