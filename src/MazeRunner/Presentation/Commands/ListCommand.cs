using MazeRunner.Application;

namespace MazeRunner.Presentation.Commands;

public sealed class ListCommand(IMazeService api) : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => new[] { "list", "mazes" };
    public string Usage => "list";
    public async Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct)
    {
        Render.Mazes(await api.AllMazesAsync(ct));
        return true;
    }
}