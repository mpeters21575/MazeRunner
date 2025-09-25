using MazeRunner.Infrastructure;

namespace MazeRunner.Presentation.Commands;

public sealed class MapCommand(IMapTracker map) : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => new[] { "map" };
    public string Usage => "map";

    public Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct)
    {
        Render.Map(map.RenderAscii());
        return Task.FromResult(true);
    }
}