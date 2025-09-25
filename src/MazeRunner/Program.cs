using MazeRunner.Application;
using MazeRunner.Application.Direction;
using MazeRunner.Infrastructure;
using MazeRunner.Presentation;
using MazeRunner.Presentation.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "MAZE_");

builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<IMazeService, MazeRunner.Infrastructure.AmazeingClientAdapter>();

builder.Services.Scan(s => s
    .FromAssemblyOf<Program>()
        .AddClasses(c => c.AssignableTo<IConsoleCommand>())
            .As<IConsoleCommand>()
            .WithSingletonLifetime()
        .AddClasses(c => c.AssignableTo<IDirectionStrategy>())
            .AsImplementedInterfaces()
            .WithSingletonLifetime()
        .AddClasses(c => c.AssignableTo<IDirectionParser>())
            .AsImplementedInterfaces()
            .WithSingletonLifetime()
        .AddClasses(c => c.AssignableTo<IMapTracker>())
            .AsImplementedInterfaces()
            .WithSingletonLifetime()
        .AddClasses(c => c.AssignableTo<MazeRunner.Presentation.Errors.IApiErrorHandler>())
            .AsImplementedInterfaces()
            .WithSingletonLifetime()
        .AddClasses(c => c.AssignableTo<CommandRouter>())
            .AsSelf()
            .WithSingletonLifetime());

builder.Services.AddSingleton<ConsoleUi>();

var host = builder.Build();
var service = host.Services.GetRequiredService<IConfiguration>();

ValidateAuth(service);

var ui = host.Services.GetRequiredService<ConsoleUi>();
await ui.RunAsync();
return;

static void ValidateAuth(IConfiguration cfg)
{
    var auth = cfg["Api:Authorization"];
    if (string.IsNullOrWhiteSpace(auth)) AnsiConsole.MarkupLine("[yellow]Api:Authorization is missing. Set it in appsettings.json or MAZE_Api__Authorization.[/]");
}
