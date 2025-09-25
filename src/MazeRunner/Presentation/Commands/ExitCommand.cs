using HightechICT.Amazeing.Client.Rest;
using MazeRunner.Application;
using MazeRunner.Infrastructure;
using MazeRunner.Presentation.Errors;

namespace MazeRunner.Presentation.Commands;

public sealed class ExitCommand(IMazeService api, IApiErrorHandler errors, IMapTracker map) : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => new[] { "exit" };
    public string Usage => "exit";
    public async Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct)
    {
        try
        {
            await api.ExitMazeAsync(ct);
            map.Reset();
            Render.Info("exited");
        }
        catch (ApiException ex) when (errors.TryHandle("exit", ex)) { }
        catch (ApiException ex) { Render.ApiError(ex); }
        return true;
    }
}