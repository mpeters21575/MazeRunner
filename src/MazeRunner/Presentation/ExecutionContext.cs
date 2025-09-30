namespace MazeRunner.Presentation;

public interface IExecutionContext
{
    bool IsInteractiveMode { get; }
}

public sealed class ExecutionContext : IExecutionContext
{
    public bool IsInteractiveMode { get; private set; } = true;
    
    public void SetCommandLineMode()
    {
        IsInteractiveMode = false;
    }
}