namespace MazeRunner.Application.Direction;

public sealed class DownStrategy : IDirectionStrategy
{
    public HightechICT.Amazeing.Client.Rest.Direction Value => HightechICT.Amazeing.Client.Rest.Direction.Down;
    public bool MatchesToken(string t) => new[] { "d", "down", "↓" }.Contains(t, StringComparer.OrdinalIgnoreCase);
    public IEnumerable<string> Aliases => new[] { "d", "down", "↓" };
}