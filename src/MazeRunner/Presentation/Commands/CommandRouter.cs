namespace MazeRunner.Presentation.Commands;

public sealed class CommandRouter(IEnumerable<IConsoleCommand> commands)
{
    readonly IReadOnlyList<IConsoleCommand> _commands = commands.ToList();

    public Task<bool> RouteAsync(string[] parts, CancellationToken ct) =>
        _commands.FirstOrDefault(c => c.Names.Contains(parts[0], StringComparer.OrdinalIgnoreCase))?.TryExecuteAsync(parts, ct)
        ?? Task.FromResult(false);
    public IEnumerable<(string Name, string Usage)> Help() =>
        _commands.Select(c => (string.Join("|", c.Names), c.Usage));
}
