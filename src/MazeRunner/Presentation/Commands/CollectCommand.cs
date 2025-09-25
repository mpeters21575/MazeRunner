using HightechICT.Amazeing.Client.Rest;
using MazeRunner.Application;
using MazeRunner.Presentation.Errors;

namespace MazeRunner.Presentation.Commands;

public sealed class CollectCommand(IMazeService api, IApiErrorHandler errors) : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => ["collect"];
    public string Usage => "collect";

    public async Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct)
    {
        try
        {
            Render.Json(await api.CollectScoreAsync(ct));
        }
        catch (ApiException ex) when (errors.TryHandle("collect", ex))
        {
        }
        catch (ApiException ex)
        {
            Render.ApiError(ex);
        }

        return true;
    }
}