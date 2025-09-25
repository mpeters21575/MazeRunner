using HightechICT.Amazeing.Client.Rest;
using Spectre.Console;

namespace MazeRunner.Presentation.Errors;

public interface IApiErrorHandler
{
    bool TryHandle(string action, ApiException ex);
}

public sealed class ApiErrorHandler : IApiErrorHandler
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> _map =
        new Dictionary<string, IReadOnlyDictionary<int, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["enter"] = new Dictionary<int, string>
            {
                [400] = "Maze name is required.",
                [404] = "That maze does not exist. Use 'list' to see available mazes.",
                [409] = "You are already in a maze or you have already played this maze.",
                [412] = "You must register before entering a maze."
            },
            ["move"] = new Dictionary<int, string>
            {
                [412] = "Enter a maze first."
            },
            ["collect"] = new Dictionary<int, string>
            {
                [403] = "Not on a collection tile or you have no points in hand.",
                [412] = "Enter a maze first."
            },
            ["exit"] = new Dictionary<int, string>
            {
                [403] = "Not on an exit tile.",
                [412] = "Enter a maze first."
            },
            ["status"] = new Dictionary<int, string>
            {
                [412] = "Enter a maze first."
            },
            ["register"] = new Dictionary<int, string>
            {
                [400] = "Name must be 1–50 chars, not whitespace.",
                [409] = "You are already registered. Use 'forget' to re-register."
            }
        };

    public bool TryHandle(string action, ApiException ex)
    {
        if (!_map.TryGetValue(action, out var byStatus)) return false;
        if (!byStatus.TryGetValue((int)ex.StatusCode, out var friendly)) return false;

        var body = string.IsNullOrWhiteSpace(ex.Response) 
            ? "(empty body)" 
            : Markup.Escape(ex.Response);
        var panel = new Panel($"[red]HTTP {(int)ex.StatusCode}[/] — {Markup.Escape(friendly)}\n\n{body}")
            .Header("API Error").Border(BoxBorder.Rounded).Expand();
        AnsiConsole.Write(panel);
        return true;
    }
}
