using Spectre.Console;

namespace MazeRunner.Presentation.Commands;

public sealed class HelpCommand(CommandRouter router) : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => new[] { "help", "h", "?" };
    public string Usage => "help";
    public Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct)
    {
        var t = new Table().Border(TableBorder.Rounded);
        t.AddColumn("Command"); t.AddColumn("Usage");
        foreach (var (name, usage) in router.Help()) t.AddRow(name, usage);
        Spectre.Console.AnsiConsole.Write(t);
        return Task.FromResult(true);
    }
}