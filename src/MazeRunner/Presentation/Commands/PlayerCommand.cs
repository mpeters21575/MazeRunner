using MazeRunner.Application;

namespace MazeRunner.Presentation.Commands;

public sealed class PlayerCommand(IMazeService api) : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => new[] { "player" };
    public string Usage => "player";
    public async Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct)
    {
        Render.Json(await api.GetPlayerAsync(ct));
        return true;
    }
}