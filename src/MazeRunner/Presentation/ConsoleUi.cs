using MazeRunner.Presentation.Commands;
using Spectre.Console;

namespace MazeRunner.Presentation;

public static class ConsoleUi
{
    public static async Task RunAsync(CommandRouter router, CancellationToken ct)
    {
        AnsiConsole.MarkupLine("[bold cyan]MazeRunner[/] type [bold]help[/] for commands.");
        var running = true;
        while (running && !ct.IsCancellationRequested)
        {
            var input = AnsiConsole.Prompt(new TextPrompt<string>("[grey]>[/] ")).Trim();
            if (string.IsNullOrEmpty(input)) continue;
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            try
            {
                var handled = await router.RouteAsync(parts, ct);
                if (!handled) running = false;
            }
            catch (HightechICT.Amazeing.Client.Rest.ApiException aex)
            {
                Render.ApiError(aex);
            }
            catch (Exception ex)
            {
                Render.Error(ex.Message);
            }
        }
    }
}