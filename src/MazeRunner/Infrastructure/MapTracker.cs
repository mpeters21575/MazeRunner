using System.Text;
using System.Text.Json;
using HightechICT.Amazeing.Client.Rest;

namespace MazeRunner.Infrastructure;

public class DirectionOpportunityInfo
{
    public string Direction { get; set; } = string.Empty;
    public bool AllowsCollection { get; set; }
    public bool AllowsExit { get; set; }
}

public interface IMapTracker
{
    void Reset();
    void Enter();
    void Enter(bool canCollectScore, bool canExit);
    void Enter(bool canCollectScore, bool canExit, string[] possibleMoves);
    void Enter(bool canCollectScore, bool canExit, DirectionOpportunityInfo[] directionOpportunities);
    void Move(string directionKey);
    void Move(string directionKey, bool canCollectScore, bool canExit);
    void Move(string directionKey, bool canCollectScore, bool canExit, string[] possibleMoves);
    void Move(string directionKey, bool canCollectScore, bool canExit, DirectionOpportunityInfo[] directionOpportunities);
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
                foreach (var dirOpp in nodeData.DirectionOpportunities)
                {
                    node.DirectionOpportunities[(Direction)dirOpp.Key] = new DirectionOpportunity
                    {
                        AllowsCollection = dirOpp.Value.AllowsCollection,
                        AllowsExit = dirOpp.Value.AllowsExit
                    };
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
                    PossibleMoves = kvp.Value.PossibleMoves.Select(d => (int)d).ToList(),
                    DirectionOpportunities = kvp.Value.DirectionOpportunities.ToDictionary(
                        kv => (int)kv.Key,
                        kv => new DirectionOpportunityData
                        {
                            AllowsCollection = kv.Value.AllowsCollection,
                            AllowsExit = kv.Value.AllowsExit
                        })
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
        var from = _possition;
        var fromNode = Get(from);
        
        // Validate that the move is actually possible from current position
        if (!fromNode.PossibleMoves.Contains(dir))
        {
            // Invalid move - server didn't move us, so don't update position
            // Just update the current node's possible moves with the server response
            fromNode.PossibleMoves.Clear();
            foreach (var move in possibleMoves)
            {
                if (TryParse(move, out var direction))
                {
                    fromNode.PossibleMoves.Add(direction);
                }
            }
            
            // Update current position properties but don't change position
            fromNode.CanCollectScore = canCollectScore;
            fromNode.CanExit = canExit;
            
            Set(from, fromNode);
            SaveState();
            return;
        }
        
        // Valid move - proceed with position update
        var delta = Delta(dir);
        var to = (from.x + delta.xDirection, from.y + delta.yDirection);
        var toNode = Get(to);

        // TRACKING LESSON: Only add outgoing links, don't assume bidirectional connections!
        // The maze may have one-way passages or blocked returns
        fromNode.Links.Add(dir);
        
        // COLLECTION STRATEGY: Track these flags carefully - they determine scoring opportunities!
        // CanCollectScore = secure points here with 'collect' command BEFORE exiting
        // CanExit = potential maze completion point, but only exit with points in bag!
        toNode.CanCollectScore = canCollectScore;
        toNode.CanExit = canExit;
        
        // Parse and store possible moves for the destination
        // Use these to determine which links exist FROM the destination
        toNode.PossibleMoves.Clear();
        toNode.Links.Clear(); // Clear existing links and rebuild from server response
        
        foreach (var move in possibleMoves)
        {
            if (TryParse(move, out var direction))
            {
                toNode.PossibleMoves.Add(direction);
                
                // Check if this direction leads to a known visited position
                var deltaMove = Delta(direction);
                var adjacentPos = (to.Item1 + deltaMove.xDirection, to.Item2 + deltaMove.yDirection);
                
                if (_nodes.ContainsKey(adjacentPos))
                {
                    // We know this adjacent position exists and has been visited
                    // So we can create a link to it
                    toNode.Links.Add(direction);
                }
            }
        }

        Set(from, fromNode);
        Set(to, toNode);

        _possition = to;
        SaveState();
    }

    public void Move(string directionKey, bool canCollectScore, bool canExit, DirectionOpportunityInfo[] directionOpportunities)
    {
        LoadState();
        
        var dir = Parse(directionKey);
        var from = _possition;
        var fromNode = Get(from);
        
        // Convert DirectionOpportunityInfo[] to string[] for move validation
        var possibleMoves = directionOpportunities.Select(d => d.Direction).ToArray();
        
        // Validate that the move is actually possible from current position
        if (!fromNode.PossibleMoves.Contains(dir))
        {
            // Invalid move - server didn't move us, so don't update position
            // Just update the current node's possible moves with the server response
            fromNode.PossibleMoves.Clear();
            foreach (var move in possibleMoves)
            {
                if (TryParse(move, out var direction))
                {
                    fromNode.PossibleMoves.Add(direction);
                }
            }
            
            // Update current position properties but don't change position
            fromNode.CanCollectScore = canCollectScore;
            fromNode.CanExit = canExit;
            
            // Store directional opportunities for current position since we're staying here
            fromNode.DirectionOpportunities.Clear();
            foreach (var opportunity in directionOpportunities)
            {
                if (TryParse(opportunity.Direction, out var direction))
                {
                    fromNode.DirectionOpportunities[direction] = new DirectionOpportunity
                    {
                        AllowsCollection = opportunity.AllowsCollection,
                        AllowsExit = opportunity.AllowsExit
                    };
                }
            }
            
            Set(from, fromNode);
            SaveState();
            return;
        }
        
        // Valid move - proceed with position update
        var delta = Delta(dir);
        var to = (from.x + delta.xDirection, from.y + delta.yDirection);
        var toNode = Get(to);

        // TRACKING LESSON: Only add outgoing links, don't assume bidirectional connections!
        // The maze may have one-way passages or blocked returns
        fromNode.Links.Add(dir);
        
        // COLLECTION STRATEGY: Track these flags carefully - they determine scoring opportunities!
        // CanCollectScore = secure points here with 'collect' command BEFORE exiting
        // CanExit = potential maze completion point, but only exit with points in bag!
        toNode.CanCollectScore = canCollectScore;
        toNode.CanExit = canExit;
        
        // Parse and store possible moves for the destination
        // Use these to determine which links exist FROM the destination
        toNode.PossibleMoves.Clear();
        toNode.Links.Clear(); // Clear existing links and rebuild from server response
        
        foreach (var move in possibleMoves)
        {
            if (TryParse(move, out var direction))
            {
                toNode.PossibleMoves.Add(direction);
                
                // Check if this direction leads to a known visited position
                var deltaMove = Delta(direction);
                var adjacentPos = (to.Item1 + deltaMove.xDirection, to.Item2 + deltaMove.yDirection);
                
                if (_nodes.ContainsKey(adjacentPos))
                {
                    // We know this adjacent position exists and has been visited
                    // So we can create a link to it
                    toNode.Links.Add(direction);
                }
            }
        }

        // Store directional opportunities for the NEW position (where we moved TO)
        toNode.DirectionOpportunities.Clear();
        foreach (var opportunity in directionOpportunities)
        {
            if (TryParse(opportunity.Direction, out var direction))
            {
                toNode.DirectionOpportunities[direction] = new DirectionOpportunity
                {
                    AllowsCollection = opportunity.AllowsCollection,
                    AllowsExit = opportunity.AllowsExit
                };
            }
        }

        Set(from, fromNode);
        Set(to, toNode);

        _possition = to;
        SaveState();
    }

    public void Enter(bool canCollectScore, bool canExit, DirectionOpportunityInfo[] directionOpportunities)
    {
        LoadState();
        
        // Convert to string array 
        var possibleMoves = directionOpportunities.Select(d => d.Direction).ToArray();
        
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
            
            // Store the rich directional opportunity information
            n.DirectionOpportunities.Clear();
            foreach (var opportunity in directionOpportunities)
            {
                if (TryParse(opportunity.Direction, out var direction))
                {
                    n.DirectionOpportunities[direction] = new DirectionOpportunity
                    {
                        AllowsCollection = opportunity.AllowsCollection,
                        AllowsExit = opportunity.AllowsExit
                    };
                }
            }
            
            Set(_possition, n);
            SaveState();
        }
    }

    public string RenderAscii()
    {
        LoadState();
        
        if (_nodes.Count == 0) return "(map is empty)\n";

        var minX = _nodes.Keys.Min(k => k.x);
        var maxX = _nodes.Keys.Max(k => k.x);
        var minY = _nodes.Keys.Min(k => k.y);
        var maxY = _nodes.Keys.Max(k => k.y);

        // Expand bounds to show unexplored connections (? symbols)
        foreach (var kvp in _nodes)
        {
            var pos = kvp.Key;
            var node = kvp.Value;
            if (node.PossibleMoves.Contains(Direction.Up)) maxY = Math.Max(maxY, pos.y + 1);
            if (node.PossibleMoves.Contains(Direction.Down)) minY = Math.Min(minY, pos.y - 1);
            if (node.PossibleMoves.Contains(Direction.Right)) maxX = Math.Max(maxX, pos.x + 1);
            if (node.PossibleMoves.Contains(Direction.Left)) minX = Math.Min(minX, pos.x - 1);
        }

        // Create 2D array: (2*width+1) x (2*height+1)
        // Odd positions are for nodes, even positions are for connections
        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        var grid = new char[2 * height + 1, 2 * width + 1];
        
        // Initialize with spaces
        for (var row = 0; row < grid.GetLength(0); row++)
            for (var col = 0; col < grid.GetLength(1); col++)
                grid[row, col] = ' ';

        // Helper function to convert world coordinates to grid coordinates
        int WorldToGridX(int worldX) => 2 * (worldX - minX) + 1;
        int WorldToGridY(int worldY) => 2 * (maxY - worldY) + 1;

        // Place all nodes first
        foreach (var kvp in _nodes)
        {
            var pos = kvp.Key;
            var node = kvp.Value;
            var gridX = WorldToGridX(pos.x);
            var gridY = WorldToGridY(pos.y);
            
            grid[gridY, gridX] = Symbol(node, pos == _possition)[0];
        }

        // Place all connections with precedence: - and | always win over ?
        foreach (var kvp in _nodes)
        {
            var pos = kvp.Key;
            var node = kvp.Value;
            var gridX = WorldToGridX(pos.x);
            var gridY = WorldToGridY(pos.y);

            // Check each direction for connections
            foreach (Direction dir in Enum.GetValues<Direction>())
            {
                var hasExplored = node.Links.Contains(dir);
                var hasUnexplored = !hasExplored && node.PossibleMoves.Contains(dir);
                
                if (!hasExplored && !hasUnexplored) continue;

                var (dx, dy) = Delta(dir);
                var connectionGridX = gridX + dx;
                var connectionGridY = gridY - dy; // Note: Y is inverted in grid

                // Check bounds
                if (connectionGridX < 0 || connectionGridX >= grid.GetLength(1) ||
                    connectionGridY < 0 || connectionGridY >= grid.GetLength(0))
                    continue;

                char symbol;
                if (hasExplored)
                {
                    symbol = (dir == Direction.Up || dir == Direction.Down ? '|' : '-');
                }
                else
                {
                    // Unexplored connection - check what's at the destination from current status response
                    // We need to look up what the server told us about this direction's opportunities
                    symbol = GetUnexploredConnectionSymbol(node, dir);
                }
                
                // Apply precedence: - and | always win over other symbols 
                var current = grid[connectionGridY, connectionGridX];
                if (current == ' ' || (current != '-' && current != '|' && (symbol == '-' || symbol == '|')) ||
                    (current == '?' && symbol != '?'))
                {
                    grid[connectionGridY, connectionGridX] = symbol;
                }
            }
        }

        // Convert grid to string
        var sb = new StringBuilder();
        for (var row = 0; row < grid.GetLength(0); row++)
        {
            for (var col = 0; col < grid.GetLength(1); col++)
            {
                sb.Append(grid[row, col]);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static char GetUnexploredConnectionSymbol(Node node, Direction dir)
    {
        if (node.DirectionOpportunities.TryGetValue(dir, out var opportunity))
        {
            // Show most important opportunity with single letters
            if (opportunity.AllowsCollection && opportunity.AllowsExit) return 'x'; // Both
            if (opportunity.AllowsCollection) return 'c'; // Collection
            if (opportunity.AllowsExit) return 'e'; // Exit
        }
        
        return '?'; // Unknown/no special opportunities
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
        // Store what opportunities exist in each direction
        public Dictionary<Direction, DirectionOpportunity> DirectionOpportunities { get; } = new();
    }
    
    private sealed class DirectionOpportunity
    {
        public bool AllowsCollection { get; set; }
        public bool AllowsExit { get; set; }
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
    public Dictionary<int, DirectionOpportunityData> DirectionOpportunities { get; set; } = new();
}

public class DirectionOpportunityData
{
    public bool AllowsCollection { get; set; }
    public bool AllowsExit { get; set; }
}
