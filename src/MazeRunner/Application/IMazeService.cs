using HightechICT.Amazeing.Client.Rest;

namespace MazeRunner.Application;

public interface IMazeService
{
    Task<PlayerInfo> RegisterAsync(string name, CancellationToken ct);
    Task ForgetAsync(CancellationToken ct);
    Task<IReadOnlyList<MazeInfo>> AllMazesAsync(CancellationToken ct);
    Task<PossibleActionsAndCurrentScore> EnterAsync(string mazeName, CancellationToken ct);
    Task<PossibleActionsAndCurrentScore> MoveAsync(HightechICT.Amazeing.Client.Rest.Direction direction, CancellationToken ct);
    Task<PossibleActionsAndCurrentScore> CollectScoreAsync(CancellationToken ct);
    Task ExitMazeAsync(CancellationToken ct);
    Task<PlayerInfo> GetPlayerAsync(CancellationToken ct);
    Task<PossibleActionsAndCurrentScore> PossibleActionsAsync(CancellationToken ct);
}
