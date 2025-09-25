using HightechICT.Amazeing.Client.Rest;
using MazeRunner.Application;
using MazeRunner.Presentation.Errors;

namespace MazeRunner.Presentation.Commands;

public sealed class StatusCommand(IMazeService api, IApiErrorHandler errors) : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => ["status"];
    public string Usage => "status";

    public async Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct)
    {
        try
        {
            Render.Json(await api.PossibleActionsAsync(ct));
        }
        catch (ApiException ex) when (errors.TryHandle("status", ex))
        {
        }
        catch (ApiException ex)
        {
            Render.ApiError(ex);
        }

        return true;
    }
}