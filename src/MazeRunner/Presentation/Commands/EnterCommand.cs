using HightechICT.Amazeing.Client.Rest;
using MazeRunner.Application;
using MazeRunner.Infrastructure;
using MazeRunner.Presentation.Errors;

namespace MazeRunner.Presentation.Commands;

public sealed class EnterCommand(IMazeService api, IApiErrorHandler errors, IMapTracker map) : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => new[] { "enter" };
    public string Usage => "enter <mazeName>";

    public async Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2)
        {
            Render.Warn("enter <mazeName>");
            return true;
        }

        var mazeName = string.Join(" ", parts.Skip(1));
        try
        {
            var response = await api.EnterAsync(mazeName, ct);
            Render.Json(response);
            map.Enter(response.CanCollectScoreHere, response.CanExitMazeHere);
            Render.Map(map.RenderAscii());
        }
        catch (ApiException ex) when (errors.TryHandle("enter", ex))
        {
        }
        catch (ApiException ex)
        {
            Render.ApiError(ex);
        }

        return true;
    }
}