namespace MazeRunner.Presentation.Commands;

public sealed class QuitCommand : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => ["quit", "q"];
    public string Usage => "quit";
    public Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct) => Task.FromResult(false);
}
