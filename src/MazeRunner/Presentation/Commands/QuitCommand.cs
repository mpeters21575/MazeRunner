namespace MazeRunner.Presentation.Commands;

public sealed class QuitCommand : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => new[] { "quit", "q" };
    public string Usage => "quit";
    public Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct) => Task.FromResult(false);
}
