using HightechICT.Amazeing.Client.Rest;
using MazeRunner.Application;
using MazeRunner.Application.Direction;
using MazeRunner.Infrastructure;
using MazeRunner.Presentation.Errors;

namespace MazeRunner.Presentation.Commands;

public sealed class MoveCommand(IMazeService api, IDirectionParser parser, IApiErrorHandler errors, IMapTracker map)
    : IConsoleCommand
{
    public IReadOnlyCollection<string> Names => new[] { "move", "m" };
    public string Usage => "move <u|r|d|l>";
    public async Task<bool> TryExecuteAsync(string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2) { Render.Warn("move <u|r|d|l>"); return true; }
        var key = parts[1];
        try
        {
            Render.Json(await api.MoveAsync(parser.Parse(key), ct));
            map.Move(key);
            Render.Map(map.RenderAscii());
        }
        catch (ApiException ex) when (errors.TryHandle("move", ex)) { }
        catch (ApiException ex) { Render.ApiError(ex); }
        return true;
    }
}