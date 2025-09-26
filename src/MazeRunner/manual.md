# MazeRunner Manual

## Overview
MazeRunner is a CLI-based maze solving application that provides persistent state tracking and enhanced visualization for systematic maze exploration.

## Goals
- **PRIMARY GOAL**: Collect ALL possible points from any maze
- **SECONDARY GOAL**: Use as few API calls as possible

## Competition Rules
- **Scoring Priority**: Points collected are the primary ranking criteria
- **API Call Efficiency**: Number of API calls is the secondary ranking criteria (fewer is better)
- **Point Loss on Exit**: Points in hand are LOST when exiting a maze - only points in bag are kept
- **No Maze Abandonment**: Cannot give up or abandon a maze once entered - must complete or forget all progress
- **Persistent API Count**: API call counting continues even after using `forget` command
- **One Maze Rule**: Must exit current maze before entering a different one

## Command Line Interface

### Basic Usage
```
dotnet run --no-build -- "<command> [parameters]"
```

**IMPORTANT**: When using commands with parameters from the command line, you must quote the entire command and its parameters together as a single string.

### Essential Commands
- `register "<name>"` - Register with the maze system (required first step)
- `list` - **CRITICAL: Shows all mazes with maximum possible points and tile counts**
- `enter "<mazeName>"` - Enter a specific maze
- `move <direction>` - Move in direction: u/up, d/down, l/left, r/right
- `collect` - Collect points in hand and secure them in bag at Collection points
- `exit` - Exit the current maze (only possible at Exit points)
- `map` - Display current maze visualization
- `status` - Show current game state and possible actions
- `forget` - Reset all progress (requires confirmation)

### Command Examples
```
dotnet run --no-build -- "register MyName"
dotnet run --no-build -- "list"                    # ALWAYS do this first!
dotnet run --no-build -- "enter Glasses"           # Target: 272 points
dotnet run --no-build -- "move u"
dotnet run --no-build -- "collect"
dotnet run --no-build -- "map"
```

## The List Command - KEY TO SUCCESS

### List Output Format
```
┌─────────────────┬───────────────────┬─────────────┐
│ Name            │ Potential rewards │ Total tiles │
├─────────────────┼───────────────────┼─────────────┤
│ Glasses         │ 272               │ 30          │
│ Test            │ 1                 │ 5           │
│ PacMan          │ 1209              │ 298         │
└─────────────────┴───────────────────┴─────────────┘
```

### Critical Information Provided
- **Name**: Maze identifier for enter command
- **Potential rewards**: **MAXIMUM POSSIBLE POINTS** achievable in the maze
- **Total tiles**: Complexity/size indicator

### Position Markers (Priority Order)
1. `@` = **Current position** (highest priority - always shows where you are)
2. `C` = **Collection point** (can secure points in bag here)
3. `E` = **Exit point** (can leave maze here)
4. `X` = **Collection AND Exit** (both functions available)
5. `S` = **Start point** (lowest priority - only shown if no special functions)
6. `o` = **Regular explored tile**

### Connection Indicators
- `-` = **Confirmed horizontal connection** (you can move left/right here)
- `|` = **Confirmed vertical connection** (you can move up/down here)
- `c` = **UNEXPLORED direction with collection opportunity** (allowsScoreCollection: true)
- `e` = **UNEXPLORED direction with exit opportunity** (allowsExit: true)
- `x` = **UNEXPLORED direction with both collection AND exit opportunities**
- `?` = **UNEXPLORED direction available** (possible move you haven't tried, no special opportunities)
- ` ` = **No connection** (wall or impassable)

### Example Visualization
```
S-o-C-@e
|     |
o    co-E?
|   c |
o-o-o-X
```
**Interpretation:**
- Current position `@` has unexplored right direction `e` (exit opportunity available)
- Collection point `C` available (top row, visited)
- Exit point `E` available (middle-right, visited)  
- Combined point `X` available (bottom-right, visited)
- Start point `S` shown (top-left, visited)
- Regular explored positions `o` connected by confirmed links `-` and `|`
- Two `c` symbols show unexplored directions leading to the same unvisited collection point
- One `e` symbol shows unexplored direction with exit opportunity
- One `?` symbol shows unexplored direction with no special opportunities

### Important Connection Symbol Behavior
- **Opportunity symbols** (`c`, `e`, `x`) only appear for **UNEXPLORED** directions
- Once you visit both ends of a connection, opportunity symbols transform to structural symbols (`-`, `|`)
- **Priority order**: Confirmed connections (`-`, `|`) > Opportunity indicators (`c`, `e`, `x`) > Generic unknown (`?`)

## API Response Data

### Move/Enter Response Structure
```json
{
  "possibleMoveActions": [
    {
      "direction": "Up",
      "isStart": false,
      "allowsExit": false,
      "allowsScoreCollection": true,
      "hasBeenVisited": false,
      "numberOfVisitsToTile": 0,
      "rewardOnDestination": 10
    }
  ],
  "canCollectScoreHere": false,
  "canExitMazeHere": false,
  "currentScoreInHand": 15,
  "currentScoreInBag": 100,
  "numberOfVisitsToTile": 1
}
```

### Key Response Properties
- **possibleMoveActions**: Array of available directions with metadata
- **canCollectScoreHere**: true if current position is Collection point
- **canExitMazeHere**: true if current position is Exit point
- **currentScoreInHand**: Points at risk (lost if you accidentally exit)
- **currentScoreInBag**: Secured points (safe, cannot be lost)
- **rewardOnDestination**: Points gained by moving to that direction

## Important Rules

### Point Management
1. **Points in Hand**: Gained by visiting/revisiting tiles, **LOST FOREVER when exiting maze**
2. **Points in Bag**: Secured at Collection points, **SAFE and permanent**
3. **Exit Warning**: Exiting with points in hand = losing those points permanently
4. **Revisiting**: Every tile gives rewards on each visit
5. **Collection Strategy**: Secure points regularly to avoid loss - only exit with zero points in hand

### Movement Rules
1. **Registration Required**: Must register before entering any maze
2. **One Maze at a Time**: Exit current maze before entering another
3. **Exit Restrictions**: Can only exit at positions where `canExitMazeHere: true`
4. **Collection Restrictions**: Can only collect points where `canCollectScoreHere: true`

### State Persistence
- **Automatic Saving**: All exploration data saved to `~/.mazerunner-state.json`
- **Cross-Session**: Can resume exploration after restarting application
- **Maze Switching**: State resets when entering different mazes
- **Manual Reset**: Use `forget` command to clear all progress

## Strategic Information Available

### From possibleMoveActions Array
- **Direction**: Which way you can move
- **hasBeenVisited**: Whether you've been to that tile before
- **numberOfVisitsToTile**: How many times you've visited that neighboring tile
- **rewardOnDestination**: Points you'll gain by moving there
- **allowsScoreCollection**: Whether destination is Collection point
- **allowsExit**: Whether destination is Exit point

### DirectionOpportunities Data Persistence
- **Automatic Tracking**: The system automatically tracks collection and exit opportunities in unexplored directions
- **JSON Storage**: DirectionOpportunities data is saved to `~/.mazerunner-state.json` and persists across sessions
- **Visual Integration**: Opportunity data is displayed as enhanced connection symbols (`c`, `e`, `x`) on the map

### From Current Position
- **numberOfVisitsToTile**: How many times you've been here
- **canCollectScoreHere**: Whether you can secure points now
- **canExitMazeHere**: Whether you can exit the maze now

## Success Criteria
1. **Achieve Target Score**: `currentScoreInBag` equals the "Potential rewards" from list command
2. **Secure All Points**: All points in hand transferred to bag at Collection points
3. **Safe Exit**: Exit only after achieving target score with zero points at risk

**Note**: Complete exploration (resolving all `?` symbols) is only necessary if you haven't reached the target score yet. Once you achieve the maximum possible points, additional exploration is optional.

## Error Handling
- **412 Errors**: Usually mean you need to register or enter a maze first
- **409 Errors**: Often indicate you're already in a maze or have played it
- **403 Errors**: "Not on an exit tile" when trying to exit from non-exit positions, or "Not on a collection tile" when trying to collect from non-collection positions
- **State Recovery**: Use `status` command to understand current situation

### Invalid Move Behavior
- **No Errors for Invalid Moves**: Attempting moves not in `possibleMoveActions` does not generate API errors
- **Server Response**: The server responds normally but the player position remains unchanged
- **Application Handling**: The application updates with the server response, showing you stayed at the same position
- **Position Tracking**: The MapTracker detects when moves don't change position and handles accordingly
- **Validation**: Only `collect` and `exit` actions generate 403 errors when attempted in wrong locations