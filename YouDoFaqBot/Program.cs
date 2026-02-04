using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using YouDoFaqBot.BackgroundServices;
using YouDoFaqBot.Interfaces;
using YouDoFaqBot.Services;
using YouDoFaqBot.Settings;
using YouDoFaqBot.Telegram;

var builder = Host.CreateApplicationBuilder(args);

/// <summary>
/// Loads application settings from appsettings.json.
/// </summary>
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

/// <summary>
/// Binds Telegram bot options from configuration.
/// </summary>
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));

/// <summary>
/// Registers the knowledge base service as a singleton for fast in-memory access.
/// </summary>
builder.Services.AddSingleton<IKnowledgeBaseService, KnowledgeBaseService>();

/// <summary>
/// Registers the Telegram update handler for processing incoming messages.
/// </summary>
builder.Services.AddSingleton<IUpdateHandler, UpdateHandler>();

/// <summary>
/// Registers the Telegram bot client using the configured bot token.
/// </summary>
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var options = builder.Configuration.GetSection("Telegram").Get<TelegramOptions>();
    if (options is null || string.IsNullOrWhiteSpace(options.BotToken))
    {
        throw new InvalidOperationException("Telegram configuration is missing or invalid.");
    }
    return new TelegramBotClient(options.BotToken);
});

/// <summary>
/// Ensures the knowledge base is loaded before starting the bot.
/// </summary>
builder.Services.AddHostedService<KnowledgeBaseHostedService>();

/// <summary>
/// Registers the background service that starts the Telegram bot and listens for updates.
/// </summary>
builder.Services.AddHostedService<BotBackgroundService>();

using var host = builder.Build();

/// <summary>
/// Runs the host and starts all registered services.
/// </summary>
await host.RunAsync();
