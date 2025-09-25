namespace MazeRunner.Application.Direction;

public sealed class RightStrategy : IDirectionStrategy
{
    public HightechICT.Amazeing.Client.Rest.Direction Value => HightechICT.Amazeing.Client.Rest.Direction.Right;
    public bool MatchesToken(string t) => new[] { "r", "right", "→" }.Contains(t, StringComparer.OrdinalIgnoreCase);
    public IEnumerable<string> Aliases => new[] { "r", "right", "→" };
}