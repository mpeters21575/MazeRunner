using MazeRunner.Presentation;
using MazeRunner.Presentation.Commands;

namespace MazeRunner.Presentation;

public sealed class ConsoleUi(CommandRouter router)
{
    public async Task RunAsync()
    {
        Render.Info("MazeRunner");
        var running = true;
        while (running)
        {
            var line = Spectre.Console.AnsiConsole.Prompt(new Spectre.Console.TextPrompt<string>(">"));
            var parts = Split(line);
            if (parts.Length == 0) continue;
            running = await router.DispatchAsync(parts, CancellationToken.None);
        }
    }

    private static string[] Split(string input)
    {
        var r = new List<string>();
        var sb = new System.Text.StringBuilder();
        var q = false;
        foreach (var ch in input)
        {
            if (ch == '"')
            {
                q = !q;
                continue;
            }

            if (!q && char.IsWhiteSpace(ch))
            {
                if (sb.Length > 0)
                {
                    r.Add(sb.ToString());
                    sb.Clear();
                }
            }
            else sb.Append(ch);
        }

        if (sb.Length > 0) r.Add(sb.ToString());
        return r.ToArray();
    }
}