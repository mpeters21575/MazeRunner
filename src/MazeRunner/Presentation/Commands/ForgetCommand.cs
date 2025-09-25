using MazeRunner.Application;
using MazeRunner.Infrastructure;
using Spectre.Console;

namespace MazeRunner.Presentation.Commands;

public sealed class ForgetCommand(IMazeService api, IMapTracker map) : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => new[] { "forget" };
    public string Usage => "forget";
    public async Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct)
    {
        if (!AnsiConsole.Confirm("Forget all progress?")) return true;
        await api.ForgetAsync(ct);
        map.Reset();
        Render.Info("forgotten");
        return true;
    }
}