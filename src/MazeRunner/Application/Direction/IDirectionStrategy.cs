namespace MazeRunner.Application.Direction;

public interface IDirectionStrategy
{
    HightechICT.Amazeing.Client.Rest.Direction Value { get; }
    bool MatchesToken(string token);
    IEnumerable<string> Aliases { get; }
}