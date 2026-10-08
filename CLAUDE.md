# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project context

This is the F20SC Industrial Programming Coursework 1 (Heriot-Watt, 2026/27): a **C# Yahtzee game with a GUI**. The spec is [F20SC_Coursework1.pdf](F20SC_Coursework1.pdf), and it is the source of truth. Read it again before changing scope. **Deadline: 29 October 2026.** A late submission loses 30%, and nothing is accepted after 5 working days.

- The spec says this is an **individual project**. Every submitted line must be explainable by the student in a possible in-person demo.
- **GenAI policy (Level 6):** GenAI use is allowed, but the report must have a GenAI usage statement with **prompts and model responses in an appendix**. Log every substantive Claude prompt and its outcome in `docs/genai-log.md` as the work goes on, so the appendix can be written from it.
- Code taken from external sources must be cited in a code comment or in the report.
- The reference platform is **Visual Studio 2026 on Windows 11**. The final build must run on a lab machine.

**Status:** All 7 implementation steps are done and verified: 130 xUnit tests, plus an end-to-end UI Automation run against the published `Yahtzee.exe`. **Still outstanding (the student's own work):** the report PDF, the narrated video, testing the exe on a clean lab machine, and filling in the "my evaluation" column of `docs/genai-log.md`. Update this file as the design changes.

## Tech stack

| Concern | Choice | Reason (goes in report) |
|---|---|---|
| Runtime | .NET 10 (LTS), C# 14, `<Nullable>enable</Nullable>` | Ships with VS 2026 |
| GUI | Windows Forms (`net10.0-windows`) | Recommended by the spec, simplest, target is Windows only |
| Save files | JSON via `System.Text.Json` | Built in, human-readable, versionable |
| Games database | SQLite via `Microsoft.Data.Sqlite` | A real DB engine that needs no server. The native `e_sqlite3` gets bundled into the exe |
| Tests | xUnit | Covers rules, engine, save files, database, AI |

The only external package in the app is `Microsoft.Data.Sqlite` 10.0.12 (with its `SQLitePCLRaw` native bundle). The report must declare it.

## Commands

```powershell
dotnet build Yahtzee.slnx
dotnet run --project src/Yahtzee.WinForms
dotnet test                                                    # all tests
dotnet test --filter "FullyQualifiedName~ScorerTests"          # one test class
dotnet test --filter "FullyQualifiedName~ScorerTests.FullHouse_RejectsYahtzee"  # one test

# Standalone executable for submission: publish/Yahtzee.exe (~53 MB, self-contained single file, no .NET needed)
dotnet publish src/Yahtzee.WinForms -p:PublishProfile=Standalone

# Use a separate games-history database (keeps test/demo games out of the real high scores)
Yahtzee.exe --database C:\path\to\demo.db

# Regenerate the code guide PDF (docs/Yahtzee_Code_Guide.pdf); needs Python 3 + Pillow and Microsoft Edge
powershell -File docs/guide-src/build_guide.ps1
```

The .NET SDK is not installed system-wide on this machine. Install the .NET 10 SDK or VS 2026, or use a user-local install via `dotnet-install.ps1 -Channel 10.0`.

The solution uses the `.slnx` format, which VS 2026 opens natively. `Directory.Build.props` turns on nullable, latest C#, and **warnings as errors** for every project. Core also has `GenerateDocumentationFile`, so **a public member without an XML doc comment fails the build**.

## Architecture

```
Yahtzee.slnx
src/Yahtzee.Core/        net10.0 class library: no UI references, fully unit-testable
  Model/                 Category (enum + extensions), IDiceRoller, DiceCup, ScoreCard, Player
  Rules/                 Scorer (pure static scoring functions)
  Game/                  GameEngine (state machine; partial class) + GameEngine.Persistence.cs (CreateSnapshot/Restore),
                         GameEvents (event-arg records, PlayerStanding, AiStepResult)
  AI/                    IPlayerStrategy, RandomStrategy, GreedyStrategy, ExpectedValueStrategy,
                         StrategyCatalog (name -> strategy), ScoringHeuristics (internal "par" values)
  Persistence/           GameSnapshot/PlayerSnapshot records, SaveGameSerializer (JSON), InvalidSaveFileException
  Data/                  IGameHistoryRepository (+ CompletedGame, HighScore records), SqliteGameHistoryRepository
src/Yahtzee.WinForms/    net10.0-windows, assembly name Yahtzee.exe
  Forms/                 MainForm, NewGameDialog, ResultsForm, HighScoresForm, PlayerTypeOption
  Properties/PublishProfiles/Standalone.pubxml   settings for the submission exe
  Controls/              DieControl (custom-painted die)
  UiTheme.cs             shared colours and fonts
tests/Yahtzee.Core.Tests/
docs/                    genai-log.md, Yahtzee_Code_Guide.pdf, guide-src/ (generator for the PDF)
```

Key design rules:

- **Core and UI are strictly separate.** `GameEngine` exposes state plus events (`TurnStarted`, `DiceRolled`, `CategoryScored`, `GameOver`), and the WinForms layer only reacts to them. All game logic lives in Core so it can be unit tested and described in the developer guide.
- **Inject randomness.** `DiceCup` takes an `IDiceRoller`. Tests use `FixedDiceRoller`, which returns a scripted sequence, so tests and AI simulations can be deterministic.
- **Division of responsibility.** `DiceCup` only tracks values and holds; it does not limit rolls, so `GameEngine` must enforce the 3-roll limit. `ScoreCard` blocks reuse of a category. `DiceCup` and `ScoreCard` each have a "restore" constructor that loading a save will use.
- **`Category` numeric values are stable.** The save format and the DB depend on them, so never reorder or insert members.
- **The `GameEngine` state machine** stores only the players, `CurrentPlayerIndex`, `RollsUsed`, and the `DiceCup`. **`Round` is derived** from the current player's filled-box count, so it is never stored or saved. The UI enables or disables controls from `CanRoll`, `CanHold`, and `CanScore`. Each action method throws `InvalidOperationException` if its guard fails. The invariants:
  - A player cannot score before the first roll.
  - Holds can be toggled only after a roll and while another roll remains. Any die can be held or released freely.
  - The player must score after the 3rd roll.
  - Scoring moves play to the next player. After every player has filled all 13 boxes, the game raises `GameOver`.
  - There are 1–6 players (`MaxPlayers`), and names are unique ignoring case.
- **Events.** `StateChanged` fires after *every* mutation; the UI uses it to refresh and to set the dirty flag. After a score, events fire in this order: `CategoryScored`, then `GameOver` or `TurnStarted`, then `StateChanged`. `TurnStarted` is **not** raised for the first turn, so the UI must render the initial state itself. `GameOverEventArgs.Standings` is sorted, and tied scores share a rank (1, 1, 3).
- **Players** share one `Player` type (name, `ScoreCard`, `IPlayerStrategy?`). A null strategy means a human. Strategies only *decide* (`ChooseHolds` returns null to stop rolling, `ChooseCategory`) and never mutate state. `GameEngine.PlayAiStep()` does one visible action per call (a roll or the final score). The UI should call it on a `System.Windows.Forms.Timer` until it returns `Scored`. It works from any mid-turn state, so AI turns resume after loading a save.
- **Timestamps** come from an injected `TimeProvider`. Tests use `ManualTimeProvider` and `ScriptedStrategy`, both in `tests/.../TestDoubles.cs`.
- **WinForms conventions.** Forms are built in code with no `.Designer.cs` files. Each one sets `AutoScaleMode.Dpi` inside `SuspendLayout`/`ResumeLayout`, and layout panels handle sizing.
  - Pixel values that AutoScale does *not* touch must be converted for DPI: DataGridView row and header heights use `LogicalToDeviceUnits` *after* the handle exists, and ListView column widths are set in `Load`. The dev machine runs at 150%, where an unconverted value shows up as cramped rows.
  - `MainForm.RefreshUi()` redraws everything from engine state and runs on every `StateChanged`.
  - The "This roll" grid column (`PreviewColumn`) is both the score preview and the scoring control: clicking an open cell scores it, and a 0 score asks for confirmation first.
  - Keyboard: 1–5 hold dice and R rolls (`ProcessCmdKey`).
  - `GameOver` opens `ResultsForm` via `BeginInvoke` so the board redraws first.
  - New AI strategies are added to `PlayerTypeOption.All`, which feeds the new-game dialog's "Type" drop-down.
  - Public properties on custom controls need `[DesignerSerializationVisibility(Hidden)]`, or the WFO1000 analyzer fails the build.
- **Unsaved changes** are tracked with a counter, not a bool: `_changeCount` goes up on every `StateChanged`, and `_savedChangeCount` records the count that was written. `HasUnsavedChanges` compares the two, so an AI move during an async save still leaves the game marked dirty. New Game, Open and closing the window call `ConfirmUnsavedChangesAsync` (Yes = save, No = discard, Cancel = abort) **only when there are unsaved changes**, which avoids the "partial" example in Appendix A. `OnFormClosing` cancels the close, asks via `BeginInvoke`, then calls `Close()` again with `_closeConfirmed` set, because saving is async.
- **Every dialog goes through `MainForm.ShowModal`**, which pauses the AI timer while it is open. New MessageBox or ShowDialog calls must use it too.
- **AI**: strategies are registered in `StrategyCatalog`, and that list feeds both `PlayerTypeOption.All` and `GameEngine.Restore`. A strategy's `Name` is persisted in save files, so never rename one. `ExpectedValueStrategy` evaluates all 32 hold masks against every re-roll outcome, caching values by sorted dice (252 hands). Measured averages over 30 seeded games: Random 39, Greedy 176, ExpectedValue 228 (see `AiStrategyTests`).
- **Games history** is recorded in `MainForm.OnGameOver` (async void, with errors caught and shown in the status bar). `Program.Main` accepts `--database <file>`. UI tests must pass it, so test games never touch the user's real high scores.

### Simplified scoring rules (Appendix B). Don't use the full Wikipedia rules.

- 13 categories: Aces through Sixes (sum of the matching face), Chance, Three of a Kind and Four of a Kind (the sum of all dice, using "at least" N), Full House 25, Small Straight 30, Large Straight 40, Yahtzee 50.
- **A Yahtzee is NOT a valid Full House.** Full House requires exactly a 3+2 split of two distinct values.
- Small Straight means the dice contain 1-2-3-4, 2-3-4-5, or 3-4-5-6, so a large straight also scores as a small one. Large Straight is 1-5 or 2-6.
- Choosing a category that doesn't match scores 0. A category can only be used once per player.
- **No** upper-section bonus, Yahtzee bonus, or joker rules. The total is the sum of the 13 boxes.
- The score preview after each roll is `Scorer.Score(category, dice)` for every unfilled category.

### Save file format (document it in the report's developer guide)

The file is indented camelCase JSON with a `.yahtzee` extension, defined by `GameSnapshot` and `PlayerSnapshot`. Its fields:
- `formatVersion` (currently 1)
- `startedAt`
- `players[]`, each with `name`, `strategy` (null means human), and `scores`: an object keyed by category name, with `null` for an empty box
- `currentPlayerIndex`, `rollsUsed`, `dice[5]` (all 0 before the first roll) and `held[5]`

`round` is **not** stored, because the engine derives it.

Validation happens in two layers:
- `SaveGameSerializer` checks the shape and version. Missing or null-but-required properties are errors, via `RespectRequiredConstructorParameters` and `RespectNullableAnnotations`.
- `GameEngine.Restore` checks that the game is possible: dice values, counts, unique names, a known strategy, and the turn order. Players before the current one must have exactly one more filled box than the current player.

Both throw `InvalidSaveFileException`. Saves are written to `path.tmp` and then moved over the target.

### Games database (document the schema in the report)

The database is at `%LOCALAPPDATA%\Yahtzee\yahtzee.db` and is created on first run.

- `Games(Id INTEGER PK AUTOINCREMENT, PlayedAt TEXT)`, where `PlayedAt` is an ISO-8601 UTC string
- `GameResults(Id, GameId → Games ON DELETE CASCADE, PlayerName TEXT COLLATE NOCASE, Score INTEGER, IsAi INTEGER)`, with indexes on `(Score DESC)` and `(PlayerName, Score DESC)`

`SqliteGameHistoryRepository` creates the schema on first use. It inserts a game and its results in one transaction. Its queries are top N overall and top N for a player name (case-insensitive); both sort ties by the earliest date and return that date. Every query is parameterised.

## Requirement → implementation map

Every row of the Appendix A checklist must be **implemented and shown in the video**. Features left out of the video count as not implemented.

| Appendix A feature (report must list exactly these) | Where | Marks |
|---|---|---|
| Single-player gameplay | GameEngine + MainForm | 5 |
| Multi-player gameplay (hotseat, cap e.g. 6 players) | NewGameDialog, GameEngine turn rotation | 10 |
| Specifying player names | NewGameDialog | — |
| Displaying whose turn it is | MainForm status/header | — |
| Displaying the number of rolls available | MainForm "Rolls left: n" | — |
| Indicating which dice will be rolled/set aside | DieControl held styling + "HELD" label | — |
| Score preview | Scorecard grid preview column | 5 |
| Saving/loading games | SaveGameSerializer + File menu | 10 |
| Offer to save the game before exiting | FormClosing + dirty flag | — |
| Result summary at the end of the game | ResultsForm, sorted by score desc | — |
| Games database | SqliteGameHistoryRepository + HighScoresForm (with dates) | 15 |
| AI opponents (several strategies, the "could" item) | AI/ strategies, chosen per player in NewGameDialog | 10 |

The other marks are code quality 20, report 15, GUI quality 5, and video 5. For code quality, use and point out advanced language features: records, pattern matching/switch expressions, LINQ, events/delegates, interfaces, generics, async/await, nullable reference types, and XML doc comments on public APIs.

## Implementation order

1. Core model and `Scorer`, with xUnit tests for every category and edge case (Yahtzee vs Full House, overlapping straights).
2. `GameEngine` state machine and its tests.
3. WinForms UI: new game dialog, dice with hold toggles, scorecard with preview, turn and rolls display, results.
4. Save/load JSON and the exit prompt.
5. SQLite history and the high-scores form.
6. AI strategies: random, then greedy (highest preview score, simple hold heuristic), then expected value.
7. Self-contained publish, then smoke test the exe on a clean Windows 11 machine.

## Submission format

1. **A zip on Canvas** containing the full source (solution and projects, without `bin/` and `obj/`), the standalone **executable** (`publish/`), and the **report as a PDF**.
2. **The report** is at most 10 pages, with screenshots in an appendix. Its sections are:
   1. Introduction (purpose, remit, assumptions)
   2. Requirements checklist table, with exactly the Appendix A rows and a status of fully, partially, or not implemented, plus notes
   3. Design considerations (class design, data structures, GUI, performance)
   4. Developer guide (classes and responsibilities, interfaces, **save-file format spec**, **DB schema**)
   5. GenAI usage statement (prompts and responses in an appendix)
   6. Conclusion and reflection
   7. References (optional)

   Note every external dependency in the report (Microsoft.Data.Sqlite / SQLitePCLRaw).
3. **The video** (mandatory, or the coursework may get 0) has voice-over and demonstrates every checklist feature. Upload it to Canvas or put a OneDrive link in the report.
