namespace MazeRunner.Presentation.Commands;

public sealed class CommandRouter
{
    readonly IReadOnlyList<IConsoleCommand> _commands;

    public CommandRouter(IEnumerable<IConsoleCommand> commands)
    {
        _commands = commands.OrderBy(c => c.Names.First()).ToList();
    }

    public async Task<bool> DispatchAsync(string[] parts, CancellationToken ct)
    {
        var name = parts[0].ToLowerInvariant();
        var cmd = _commands.FirstOrDefault(c => c.Names.Contains(name, StringComparer.OrdinalIgnoreCase));
        if (cmd is null)
        {
            Render.Warn("unknown command");
            return true;
        }

        return await cmd.TryExecuteAsync(parts, ct);
    }

    public IEnumerable<(string name, string usage)> Help()
        => from command in _commands from name in command.Names select (name, command.Usage);
}