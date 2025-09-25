using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace MazeRunner.Presentation.Commands;

public sealed class HelpCommand(IServiceProvider sp) : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => ["help", "h", "?"];
    public string Usage => "help";

    public Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct)
    {
        var commands = sp.GetRequiredService<IEnumerable<IConsoleCommand>>();

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Command");
        table.AddColumn("Usage");

        foreach (var command in commands)
        {
            foreach (var name in command.Names)
            {
                table.AddRow(name, command.Usage);
            }
        }

        AnsiConsole.Write(table);
        return Task.FromResult(true);
    }
}