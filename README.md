# MazeRunner (console)

A production-ready .NET console client for the **Amazeing** maze challenge API.  
It uses the official `HightechICT.Amazeing.Client.Rest` package, applies SOLID patterns, includes resilient HTTP error handling for the server’s content-type glitch, and renders a live **ASCII map** (“fog-of-war”) of what you’ve explored.

---

## Features

- Clean command-driven UX: `register`, `list`, `enter`, `move`, `collect`, `exit`, `status`, `map`, `player`, `forget`, `help`, `quit`
- `AmazeingClientAdapter` boundary + direction **strategy** pattern
- **HTTP error normalizer** for empty/plain-text error bodies; stable `ProblemDetails` handling
- Friendly, action-aware error panels
- **ASCII map**: visited tiles, discovered connections, current position
- Spectre.Console output, DI with `Microsoft.Extensions.*` and `IHttpClientFactory`

---

## Quick start

### 1) Prerequisites
- .NET SDK **9.0** or later

### 2) Configure API
Edit `src/MazeRunner.App/appsettings.json`:

```json
{
  "Api": {
    "BaseUrl": "https://maze.kluster.htiprojects.nl",
    "Authorization": "HTI Thanks You <your-token-here>",
    "TimeoutSeconds": 30
  }
}
```

> **Authorization** must be the full header value exactly as used in Postman, e.g. `HTI Thanks You abcdefgh...`.

You can also override via environment variables (prefix `MAZE_`):

- `MAZE_Api__BaseUrl`
- `MAZE_Api__Authorization`
- `MAZE_Api__TimeoutSeconds`

Example (bash):

```bash
export MAZE_Api__Authorization="HTI Thanks You abcdef123456"
```

### 3) Build & run

```bash
dotnet build
dotnet run --project src/MazeRunner.App
```

---

## Commands

| Command    | Alias | Usage               | What it does                                        |
|------------|-------|---------------------|-----------------------------------------------------|
| `help`     | `h`   | `help`              | Lists commands                                      |
| `register` |       | `register name`   | Registers your player                               |
| `player`   |       | `player`            | Shows current player info                           |
| `list`     | `mazes` | `list`            | Lists available mazes (static list)                 |
| `enter`    |       | `enter mazeName`  | Enters a maze (once per maze per registration)      |
| `move`     | `m`   | `move u, r, d, l`    | Moves Up/Right/Down/Left                            |
| `collect`  |       | `collect`           | Collects score on a **C** tile                      |
| `exit`     |       | `exit`              | Exits on an **E** tile                              |
| `status`   |       | `status`            | Shows possible actions and current score            |
| `map`      |       | `map`               | Renders the ASCII map of explored area              |
| `forget`   |       | `forget`            | Deletes your player (token remains valid)           |
| `quit`     | `q`   | `quit`              | Exits the app                                       |

**Directions accepted:** `u`, `r`, `d`, `l` (also `up`, `right`, `down`, `left`, or `↑→↓←`).

---

## ASCII map

After `enter` and every successful `move`, the map prints automatically.  
You can draw it anytime with `map`.

Legend:
- `@` current position
- `S` starting tile (visited)
- `o` visited tile
- `-` and `|` discovered connections
- space = unknown (unvisited)

Example:

```
o-o
  |
S-@
```

This is a **discovery map**: it only shows what you’ve actually traversed (fog-of-war).

---

## Typical flow

```
register Ada Lovelace
list
enter Example maze
status
map
move r
move u
collect
map
exit
```

If the server says you’re already in or have played a maze, you’ll get a clear panel explaining how to proceed.

---

## Resilient HTTP handling (server glitch)

The server can sometimes respond with `text/json` or empty bodies for errors, while the NSwag client expects `application/json` and tries to parse `ProblemDetails`.  
This app includes an **ErrorBodyNormalizationHandler** that:

- Intercepts all 4xx/5xx responses
- If the body is empty or not JSON, replaces it with minimal RFC-7807 JSON
- Forces `Content-Type: application/json`
- Supplies action-aware default messages (`enter`, `move`, `collect`, etc.)

The UI renders a readable, friendly error panel.

---

## Architecture (at a glance)

- **Infrastructure**
    - `AmazeingClientAdapter` — wraps `HightechICT.Amazeing.Client.Rest.AmazeingClient`, centralizes calls, handles deserialization edge cases
    - `ErrorBodyNormalizationHandler` — normalizes error bodies/content-type
    - `MapTracker` — tracks explored tiles and links; renders ASCII
- **Application**
    - Direction **strategy** implementations (`Up/Right/Down/Left`) + `DirectionParser`
    - `IMazeService` abstraction (UI/API boundary)
- **Presentation**
    - `ConsoleUi` loop, `CommandRouter`, command implementations
    - `Render` helpers (tables, panels, JSON, map)

---

## Troubleshooting

- **409 Already in / already played this maze**
    - Use `exit` if you’re inside.
    - Need a fresh start? `forget` then `register` again.

- **412 You haven’t entered/registered**
    - `register <name>` before `enter`.
    - `enter <mazeName>` before `move` / `collect` / `status`.

- **403 Not on correct tile**
    - `collect` works only on a **C** tile.
    - `exit` works only on an **E** tile.

- **Auth problems**
    - Ensure `Api:Authorization` contains the **full** value, e.g. `HTI Thanks You <token>`.
    - The app sends exactly what you configure—no automatic prefixing.

---

## Scripts / One-liners

Build & run:

```bash
dotnet run --project src/MazeRunner.App
```

Override token for a single run:

```bash
MAZE_Api__Authorization="HTI Thanks You abc..." dotnet run --project src/MazeRunner.App
```

---

Happy mazing! 🍩
