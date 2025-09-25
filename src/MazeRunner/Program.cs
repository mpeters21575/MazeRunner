using MazeRunner.Application;
using MazeRunner.Application.Direction;
using MazeRunner.Infrastructure;
using MazeRunner.Infrastructure.Http;
using MazeRunner.Presentation;
using MazeRunner.Presentation.Commands;
using MazeRunner.Presentation.Errors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders().AddConsole();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables("MAZE_");

builder.Services.AddSingleton<IDirectionStrategy, UpStrategy>();
builder.Services.AddSingleton<IDirectionStrategy, RightStrategy>();
builder.Services.AddSingleton<IDirectionStrategy, DownStrategy>();
builder.Services.AddSingleton<IDirectionStrategy, LeftStrategy>();
builder.Services.AddSingleton<IDirectionParser, DirectionParser>();

builder.Services.AddTransient<ErrorBodyNormalizationHandler>();
builder.Services.AddHttpClient("Amaze")
    .AddHttpMessageHandler<ErrorBodyNormalizationHandler>();

builder.Services.AddSingleton<IMazeService>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Amaze");
    var cfg  = sp.GetRequiredService<IConfiguration>();
    return new AmazeingClientAdapter(http, cfg);
});

builder.Services.AddSingleton<IMapTracker, MapTracker>();

builder.Services.AddSingleton<IApiErrorHandler, ApiErrorHandler>();
builder.Services.AddSingleton<IConsoleCommand, RegisterCommand>();
builder.Services.AddSingleton<IConsoleCommand, PlayerCommand>();
builder.Services.AddSingleton<IConsoleCommand, ForgetCommand>();
builder.Services.AddSingleton<IConsoleCommand, ListCommand>();
builder.Services.AddSingleton<IConsoleCommand, EnterCommand>();
builder.Services.AddSingleton<IConsoleCommand, MoveCommand>();
builder.Services.AddSingleton<IConsoleCommand, CollectCommand>();
builder.Services.AddSingleton<IConsoleCommand, ExitCommand>();
builder.Services.AddSingleton<IConsoleCommand, StatusCommand>();
builder.Services.AddSingleton<IConsoleCommand, QuitCommand>();
builder.Services.AddSingleton<IConsoleCommand, MapCommand>();
builder.Services.AddSingleton<CommandRouter>(sp =>
{
    var commands = sp.GetServices<IConsoleCommand>().ToList();
    var router = new CommandRouter(commands);
    var help = new HelpCommand(router);
    commands.Add(help);
    return new CommandRouter(commands);
});

using var host = builder.Build();
var routerInstance = host.Services.GetRequiredService<CommandRouter>();
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
await ConsoleUi.RunAsync(routerInstance, cts.Token);
