namespace MazeRunner.Presentation.Commands;

public interface IConsoleCommand
{
    IReadOnlyCollection<string> Names { get; }
    string Usage { get; }
    Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct);
}