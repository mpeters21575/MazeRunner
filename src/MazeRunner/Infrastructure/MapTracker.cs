using System.Text;
using System.Text.Json;
using HightechICT.Amazeing.Client.Rest;

namespace MazeRunner.Infrastructure;

public interface IMapTracker
{
    void Reset();
    void Enter();
    void Enter(bool canCollectScore, bool canExit);
    void Enter(bool canCollectScore, bool canExit, string[] possibleMoves);
    void Move(string directionKey);
    void Move(string directionKey, bool canCollectScore, bool canExit);
    void Move(string directionKey, bool canCollectScore, bool canExit, string[] possibleMoves);
    string RenderAscii();
    void LoadState();
    void SaveState();
}

public sealed class MapTracker : IMapTracker
{
    private readonly Dictionary<(int x,int y), Node> _nodes = new();
    private (int x,int y) _possition;
    private static readonly string StateFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mazerunner-state.json");

    public void LoadState()
    {
        try
        {
            if (!File.Exists(StateFile)) return;
            
            var json = File.ReadAllText(StateFile);
            var state = JsonSerializer.Deserialize<MapState>(json);
            if (state == null) return;

            _nodes.Clear();
            _possition = (state.PositionX, state.PositionY);
            
            foreach (var nodeData in state.Nodes)
            {
                var node = new Node
                {
                    IsStart = nodeData.IsStart,
                    CanCollectScore = nodeData.CanCollectScore,
                    CanExit = nodeData.CanExit
                };
                foreach (var direction in nodeData.Links)
                {
                    node.Links.Add((Direction)direction);
                }
                foreach (var direction in nodeData.PossibleMoves)
                {
                    node.PossibleMoves.Add((Direction)direction);
                }
                _nodes[(nodeData.X, nodeData.Y)] = node;
            }
        }
        catch
        {
            // If loading fails, start fresh
            Reset();
        }
    }

    public void SaveState()
    {
        try
        {
            var state = new MapState
            {
                PositionX = _possition.x,
                PositionY = _possition.y,
                Nodes = _nodes.Select(kvp => new NodeData
                {
                    X = kvp.Key.x,
                    Y = kvp.Key.y,
                    IsStart = kvp.Value.IsStart,
                    CanCollectScore = kvp.Value.CanCollectScore,
                    CanExit = kvp.Value.CanExit,
                    Links = kvp.Value.Links.Select(d => (int)d).ToList(),
                    PossibleMoves = kvp.Value.PossibleMoves.Select(d => (int)d).ToList()
                }).ToList()
            };

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StateFile, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    public void Reset()
    {
        _nodes.Clear();
        _possition = (0,0);
        SaveState();
    }

    public void Enter()
    {
        Enter(false, false);
    }

    public void Enter(bool canCollectScore, bool canExit)
    {
        Enter(canCollectScore, canExit, Array.Empty<string>());
    }

    public void Enter(bool canCollectScore, bool canExit, string[] possibleMoves)
    {
        LoadState();
        // Only reset if we don't have any saved state
        if (_nodes.Count == 0)
        {
            var n = Get(_possition);
            n.IsStart = true;
            n.CanCollectScore = canCollectScore;
            n.CanExit = canExit;
            
            // Parse and store possible moves
            n.PossibleMoves.Clear();
            foreach (var move in possibleMoves)
            {
                if (TryParse(move, out var direction))
                {
                    n.PossibleMoves.Add(direction);
                }
            }
            
            Set(_possition, n);
            SaveState();
        }
    }

    public void Move(string directionKey)
    {
        Move(directionKey, false, false);
    }

    public void Move(string directionKey, bool canCollectScore, bool canExit)
    {
        Move(directionKey, canCollectScore, canExit, Array.Empty<string>());
    }

    public void Move(string directionKey, bool canCollectScore, bool canExit, string[] possibleMoves)
    {
        LoadState();
        
        var dir = Parse(directionKey);
        var delta = Delta(dir);
        var from = _possition;
        var to = (from.x + delta.xDirection, from.y + delta.yDirection);

        var fromNode = Get(from);
        var toNode = Get(to);

        fromNode.Links.Add(dir);
        toNode.Links.Add(Opposite(dir));
        
        // Update the destination node with the special properties
        toNode.CanCollectScore = canCollectScore;
        toNode.CanExit = canExit;
        
        // Parse and store possible moves for the destination
        toNode.PossibleMoves.Clear();
        foreach (var move in possibleMoves)
        {
            if (TryParse(move, out var direction))
            {
                toNode.PossibleMoves.Add(direction);
            }
        }

        Set(from, fromNode);
        Set(to, toNode);

        _possition = to;
        SaveState();
    }

    public string RenderAscii()
    {
        LoadState();
        
        if (_nodes.Count == 0) return "(map is empty)\n";

        var minX = _nodes.Keys.Min(k => k.x);
        var maxX = _nodes.Keys.Max(k => k.x);
        var minY = _nodes.Keys.Min(k => k.y);
        var maxY = _nodes.Keys.Max(k => k.y);

        var sb = new StringBuilder();
        for (var y = maxY; y >= minY; y--)
        {
            var rowTiles = new StringBuilder();
            var rowLinks = new StringBuilder();

            for (var x = minX; x <= maxX; x++)
            {
                var here = (x: x,y);
                var value = _nodes.TryGetValue(here, out var node) ? node : null;

                rowTiles.Append(Symbol(value, here == _possition));
                if (x < maxX)
                {
                    var hasExploredRight = value is { } && value.Links.Contains(Direction.Right);
                    var hasUnexploredRight = value is { } && !hasExploredRight && value.PossibleMoves.Contains(Direction.Right);
                    if (hasExploredRight)
                        rowTiles.Append("-");
                    else if (hasUnexploredRight)
                        rowTiles.Append("?");
                    else
                        rowTiles.Append(" ");
                }

                if (y > minY)
                {
                    // Check the node above this position for Down connections
                    var nodeAbove = _nodes.TryGetValue((x, y - 1), out var aboveNode) ? aboveNode : null;
                    var hasExploredDown = nodeAbove?.Links.Contains(Direction.Down) == true;
                    var hasUnexploredDown = nodeAbove != null && !hasExploredDown && nodeAbove.PossibleMoves.Contains(Direction.Down);
                    if (hasExploredDown)
                        rowLinks.Append("|");
                    else if (hasUnexploredDown)
                        rowLinks.Append("?");
                    else
                        rowLinks.Append(" ");
                    if (x < maxX) rowLinks.Append(" ");
                }
            }

            sb.AppendLine(rowTiles.ToString());
            if (y > minY) sb.AppendLine(rowLinks.ToString());
        }

        return sb.ToString();
    }

    private static Direction Parse(string key)
    {
        var value = key.Trim().ToLowerInvariant();
        if (new[] { "u", "up", "↑" }.Contains(value)) return Direction.Up;
        if (new[] { "r", "right", "→" }.Contains(value)) return Direction.Right;
        if (new[] { "d", "down", "↓" }.Contains(value)) return Direction.Down;
        if (new[] { "l", "left", "←" }.Contains(value)) return Direction.Left;
        throw new ArgumentException("invalid direction");
    }

    private static bool TryParse(string key, out Direction direction)
    {
        try
        {
            direction = Parse(key);
            return true;
        }
        catch
        {
            direction = default;
            return false;
        }
    }

    private static (int xDirection, int yDirection) Delta(Direction direction)
    {
        if (direction == Direction.Up) return (0, 1);
        if (direction == Direction.Right) return (1, 0);
        if (direction == Direction.Down) return (0,-1);
        return (-1,0);
    }

    private static Direction Opposite(Direction direction)
    {
        if (direction == Direction.Up) return Direction.Down;
        if (direction == Direction.Right) return Direction.Left;
        if (direction == Direction.Down) return Direction.Up;
        return Direction.Right;
    }

    private static string Symbol(Node? node, bool current)
    {
        if (current) return "@";
        if (node is null) return " ";
        
        // Special tiles with collection and/or exit capabilities (priority over start)
        if (node.CanCollectScore && node.CanExit) return "X"; // Both collection and exit
        if (node.CanCollectScore) return "C"; // Collection point
        if (node.CanExit) return "E"; // Exit point
        
        // Only show start if it has no special capabilities
        if (node.IsStart) return "S";
        
        return "o"; // Regular tile
    }

    private Node Get((int x,int y) coordinates)
    {
        if (_nodes.TryGetValue(coordinates, out var n)) return n;
        n = new Node();
        _nodes[coordinates] = n;
        return n;
    }

    private void Set((int x,int y) p, Node n) => _nodes[p] = n;

    private sealed class Node
    {
        public bool IsStart { get; set; }
        public bool CanCollectScore { get; set; }
        public bool CanExit { get; set; }
        public HashSet<Direction> Links { get; } = new();
        public HashSet<Direction> PossibleMoves { get; } = new();
    }
}

// Data classes for JSON serialization
public class MapState
{
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public List<NodeData> Nodes { get; set; } = new();
}

public class NodeData
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsStart { get; set; }
    public bool CanCollectScore { get; set; }
    public bool CanExit { get; set; }
    public List<int> Links { get; set; } = new();
    public List<int> PossibleMoves { get; set; } = new();
}
