
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using YouDoFaqBot.Services;
using YouDoFaqBot.Telegram;

var builder = Host.CreateApplicationBuilder(args);

// Bind TelegramOptions from configuration
builder.Services.Configure<TelegramOptions>(
    builder.Configuration.GetSection("Telegram"));

builder.Services.AddSingleton<IKnowledgeBaseService, KnowledgeBaseService>();
builder.Services.AddSingleton<UpdateHandler>();
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var options = builder.Configuration.GetSection("Telegram").Get<TelegramOptions>()!;
    return new TelegramBotClient(options.BotToken);
});
builder.Services.AddHostedService<BotHostedService>();

var host = builder.Build();
// await host.RunAsync();
