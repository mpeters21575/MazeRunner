using MazeRunner.Application;
using MazeRunner.Infrastructure;
using Spectre.Console;

namespace MazeRunner.Presentation.Commands;

public sealed class ForgetCommand(IMazeService api, IMapTracker map, IExecutionContext executionContext) : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => ["forget"];
    public string Usage => "forget";

    public async Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct)
    {
        // Skip confirmation when running from command line
        if (executionContext.IsInteractiveMode && !await AnsiConsole.ConfirmAsync("Forget all progress?", true, ct)) 
            return true;
            
        await api.ForgetAsync(ct);
        map.Reset();
        Render.Info("forgotten");
        return true;
    }
}