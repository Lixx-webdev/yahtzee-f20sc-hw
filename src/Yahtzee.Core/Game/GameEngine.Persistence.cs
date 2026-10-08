using Yahtzee.Core.AI;
using Yahtzee.Core.Model;
using Yahtzee.Core.Persistence;
using Yahtzee.Core.Rules;

namespace Yahtzee.Core.Game;

// Saving and restoring games. This half of the partial class can reach the engine's private state,
// which a restore needs to set; everything else about the engine is in GameEngine.cs.
public sealed partial class GameEngine
{
    /// <summary>Whether the game can be saved: any time before it ends.</summary>
    public bool CanSave => !IsGameOver;

    /// <summary>Captures the complete game state, including mid-turn dice and holds.</summary>
    /// <exception cref="InvalidOperationException">The game is over.</exception>
    public GameSnapshot CreateSnapshot()
    {
        if (!CanSave)
            throw new InvalidOperationException("A finished game cannot be saved.");

        var players = _players
            .Select(player => new PlayerSnapshot(
                player.Name,
                player.Strategy?.Name,
                CategoryExtensions.All.ToDictionary(category => category, player.ScoreCard.GetScore)))
            .ToList();

        bool rolled = RollsUsed > 0;
        return new GameSnapshot(
            SaveGameSerializer.CurrentFormatVersion,
            StartedAt,
            players,
            CurrentPlayerIndex,
            RollsUsed,
            rolled ? [.. Dice] : [0, 0, 0, 0, 0],
            rolled ? [.. Held] : [false, false, false, false, false]);
    }

    /// <summary>
    /// Recreates a game from a snapshot, after checking that the snapshot describes a game that could
    /// really have been reached by playing.
    /// </summary>
    /// <param name="snapshot">The saved state, usually from <see cref="SaveGameSerializer.LoadAsync"/>.</param>
    /// <param name="roller">Source of die values for the rest of the game.</param>
    /// <param name="createStrategy">Creates the AI strategy with the given saved name.</param>
    /// <param name="timeProvider">Clock for timestamps; defaults to the system clock.</param>
    /// <exception cref="InvalidSaveFileException">The snapshot is inconsistent or refers to an unknown strategy.</exception>
    public static GameEngine Restore(GameSnapshot snapshot, IDiceRoller roller, Func<string, IPlayerStrategy> createStrategy,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(roller);
        ArgumentNullException.ThrowIfNull(createStrategy);

        if (snapshot.Players.Count is 0 or > MaxPlayers)
            throw new InvalidSaveFileException($"A game must have between 1 and {MaxPlayers} players.");
        if (snapshot.CurrentPlayerIndex < 0 || snapshot.CurrentPlayerIndex >= snapshot.Players.Count)
            throw new InvalidSaveFileException("The current player does not exist.");
        if (snapshot.RollsUsed is < 0 or > MaxRollsPerTurn)
            throw new InvalidSaveFileException($"Rolls used must be between 0 and {MaxRollsPerTurn}.");
        if (snapshot.Dice.Count != Scorer.DiceCount || snapshot.Held.Count != Scorer.DiceCount)
            throw new InvalidSaveFileException($"Exactly {Scorer.DiceCount} dice and held flags are required.");

        GameEngine game;
        try
        {
            var players = snapshot.Players.Select(saved => RestorePlayer(saved, createStrategy));
            var dice = snapshot.RollsUsed == 0
                ? new DiceCup(roller)
                : new DiceCup(roller, snapshot.Dice, snapshot.Held);
            game = new GameEngine(players, dice, timeProvider, snapshot.StartedAt);
        }
        catch (ArgumentException ex)
        {
            // Invalid names, scores or dice are reported by the model classes as ArgumentExceptions.
            throw new InvalidSaveFileException($"The save file contains invalid data: {ex.Message}", ex);
        }

        game.CurrentPlayerIndex = snapshot.CurrentPlayerIndex;
        game.RollsUsed = snapshot.RollsUsed;
        game.CheckTurnOrder();
        return game;
    }

    private static Player RestorePlayer(PlayerSnapshot saved, Func<string, IPlayerStrategy> createStrategy)
    {
        var scores = saved.Scores
            .Where(entry => entry.Value is not null)
            .ToDictionary(entry => entry.Key, entry => entry.Value!.Value);

        IPlayerStrategy? strategy = null;
        if (saved.Strategy is not null)
        {
            try
            {
                strategy = createStrategy(saved.Strategy);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidSaveFileException($"Unknown AI strategy \"{saved.Strategy}\" for {saved.Name}.", ex);
            }
        }

        return new Player(saved.Name, strategy, new ScoreCard(scores));
    }

    /// <summary>
    /// Every player fills one box per turn, in turn order. So players before the current one must have
    /// filled exactly one more box than the current player, and players after it the same number.
    /// </summary>
    private void CheckTurnOrder()
    {
        int filled = CurrentPlayer.ScoreCard.Scores.Count;
        if (filled >= TotalRounds)
            throw new InvalidSaveFileException("The game in this file has already finished.");

        for (int i = 0; i < _players.Count; i++)
        {
            int expected = i < CurrentPlayerIndex ? filled + 1 : filled;
            if (_players[i].ScoreCard.Scores.Count != expected)
                throw new InvalidSaveFileException(
                    $"{_players[i].Name} has {_players[i].ScoreCard.Scores.Count} scores filled, but it is " +
                    $"{CurrentPlayer.Name}'s turn in round {filled + 1}, so {expected} were expected.");
        }
    }
}
