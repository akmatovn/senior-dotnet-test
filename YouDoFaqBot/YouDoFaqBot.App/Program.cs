using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using YouDoFaqBot.App.Telegram;
using YouDoFaqBot.App.Workers;
using YouDoFaqBot.Core.Handlers;
using YouDoFaqBot.Core.Handlers.MessageHandlers;
using YouDoFaqBot.Core.Interfaces;
using YouDoFaqBot.Core.Services;
using YouDoFaqBot.Core.Settings;

/// <summary>
/// Entry point for the YouDo FAQ Bot application.
/// Configures dependency injection, loads configuration, and starts the host.
/// </summary>
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        /// <summary>
        /// Adds the appsettings.json configuration file to the configuration builder.
        /// </summary>
        config.AddJsonFile("appsettings.json", optional: true);
    })
    .ConfigureServices((context, services) =>
    {
        /// <summary>
        /// Configures application services and dependency injection.
        /// </summary>
        services.Configure<TelegramOptions>(context.Configuration.GetSection("Telegram"));
        services.Configure<KnowledgeBaseOptions>(context.Configuration.GetSection("KnowledgeBase"));

        var telegramOptions = context.Configuration.GetSection("Telegram").Get<TelegramOptions>()!;
        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(telegramOptions.BotToken));

        services.AddSingleton<IBotResponsePublisher, TelegramBotResponsePublisher>();

        services.AddSingleton<IKnowledgeBaseService, KnowledgeBaseService>();
        services.AddHostedService<KnowledgeBaseWorker>();

        services.AddSingleton<UpdateDispatcher>();
        services.AddSingleton<ISlugMappingService, SlugMappingService>();
        services.AddSingleton<ISearchStateService, SearchStateService>();
        services.AddSingleton<ICallbackHandler, CategoryHandler>();
        services.AddSingleton<ICallbackHandler, SubcategoryHandler>();
        services.AddSingleton<ICallbackHandler, ArticleHandler>();
        services.AddSingleton<ICallbackHandler, RatingHandler>();
        services.AddSingleton<ICallbackHandler, MainMenuHandler>();
        services.AddSingleton<ICallbackHandler, IgnoreHandler>();
        services.AddSingleton<ICallbackHandler, SearchMoreHandler>();
        services.AddSingleton<ICallbackHandler, SearchRestoreHandler>();
        services.AddSingleton<IMessageHandler, StartMessageHandler>();
        services.AddSingleton<IMessageHandler, SearchMessageHandler>();

        services.AddHostedService<BotWorker>();
    });

/// <summary>
/// Registers global handlers for unhandled exceptions and unobserved task exceptions.
/// Ensures the application exits with an error code on fatal errors.
/// </summary>
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    var ex = e.ExceptionObject as Exception;
    Console.Error.WriteLine($"[FATAL] Unhandled exception: {ex}");
    Environment.Exit(-1);
};
TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    Console.Error.WriteLine($"[FATAL] Unobserved task exception: {e.Exception}");
    e.SetObserved();
    Environment.Exit(-1);
};

/// <summary>
/// Runs the application as a console host.
/// </summary>
await builder.RunConsoleAsync();
