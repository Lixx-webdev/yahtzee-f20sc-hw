using Yahtzee.Core.Model;
using Yahtzee.Core.Rules;

namespace Yahtzee.Core.Game;

/// <summary>
/// Runs a game of Yahtzee: whose turn it is, how many rolls are left, which dice are held,
/// and recording scores. The GUI calls the action methods and reacts to the events;
/// all rule enforcement happens here.
/// </summary>
/// <remarks>
/// Turn rules: a turn starts with no dice rolled. The player must roll at least once and may
/// roll up to <see cref="MaxRollsPerTurn"/> times, holding or releasing any dice between rolls.
/// After any roll the player may score one open category, which ends the turn and passes play
/// to the next player. The game ends when every player has filled all 13 categories.
/// <para>Saving and restoring games is in <c>GameEngine.Persistence.cs</c> (the other part of this partial class).</para>
/// </remarks>
public sealed partial class GameEngine
{
    /// <summary>Maximum number of rolls in one turn.</summary>
    public const int MaxRollsPerTurn = 3;

    /// <summary>Maximum number of players in one game.</summary>
    public const int MaxPlayers = 6;

    /// <summary>Number of rounds in a game: one per scorecard category.</summary>
    public static int TotalRounds => CategoryExtensions.All.Count;

    private readonly List<Player> _players;
    private readonly DiceCup _dice;
    private readonly TimeProvider _timeProvider;

    /// <summary>Starts a new game with the first player to roll.</summary>
    /// <param name="players">The players in turn order; 1 to <see cref="MaxPlayers"/>, with unique names.</param>
    /// <param name="roller">Source of die values.</param>
    /// <param name="timeProvider">Clock for timestamps; defaults to the system clock.</param>
    /// <exception cref="ArgumentException">The player list is empty, too long, or has duplicate names.</exception>
    public GameEngine(IEnumerable<Player> players, IDiceRoller roller, TimeProvider? timeProvider = null)
        : this(players, new DiceCup(roller), timeProvider, startedAt: null)
    {
    }

    /// <summary>
    /// Shared constructor: validates the players and stores the state. Used directly by
    /// <see cref="Restore"/> to supply restored dice and the original start time.
    /// </summary>
    private GameEngine(IEnumerable<Player> players, DiceCup dice, TimeProvider? timeProvider, DateTimeOffset? startedAt)
    {
        ArgumentNullException.ThrowIfNull(players);

        _players = [.. players];
        if (_players.Count is 0 or > MaxPlayers)
            throw new ArgumentException($"A game needs between 1 and {MaxPlayers} players.", nameof(players));
        if (_players.DistinctBy(player => player.Name, StringComparer.OrdinalIgnoreCase).Count() != _players.Count)
            throw new ArgumentException("Player names must be unique.", nameof(players));

        _dice = dice;
        _timeProvider = timeProvider ?? TimeProvider.System;
        StartedAt = startedAt ?? _timeProvider.GetUtcNow();
    }

    /// <summary>Raised whenever any game state changes. Useful for refreshing the GUI and tracking unsaved changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Raised when play passes to the next player. Not raised for the very first turn.</summary>
    public event EventHandler<TurnStartedEventArgs>? TurnStarted;

    /// <summary>Raised after each roll.</summary>
    public event EventHandler<DiceRolledEventArgs>? DiceRolled;

    /// <summary>Raised after a score is recorded.</summary>
    public event EventHandler<CategoryScoredEventArgs>? CategoryScored;

    /// <summary>Raised once when the game ends.</summary>
    public event EventHandler<GameOverEventArgs>? GameOver;

    /// <summary>The players in turn order.</summary>
    public IReadOnlyList<Player> Players => _players;

    /// <summary>Index into <see cref="Players"/> of the player whose turn it is.</summary>
    public int CurrentPlayerIndex { get; private set; }

    /// <summary>The player whose turn it is.</summary>
    public Player CurrentPlayer => _players[CurrentPlayerIndex];

    /// <summary>When the game was started.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>
    /// The current round, 1–13. Derived from the current player's scorecard, since every
    /// player fills exactly one category per round.
    /// </summary>
    public int Round => Math.Min(CurrentPlayer.ScoreCard.Scores.Count + 1, TotalRounds);

    /// <summary>Rolls made so far in the current turn, 0–3.</summary>
    public int RollsUsed { get; private set; }

    /// <summary>Rolls left in the current turn.</summary>
    public int RollsRemaining => MaxRollsPerTurn - RollsUsed;

    /// <summary>The five current die values. Only meaningful once <see cref="RollsUsed"/> is above 0.</summary>
    public IReadOnlyList<int> Dice => _dice.Values;

    /// <summary>Which dice are held and will be kept on the next roll.</summary>
    public IReadOnlyList<bool> Held => _dice.Held;

    /// <summary>Whether every player has filled their scorecard.</summary>
    public bool IsGameOver { get; private set; }

    /// <summary>Whether the current player may roll now.</summary>
    public bool CanRoll => !IsGameOver && RollsRemaining > 0;

    /// <summary>
    /// Whether the current player may hold or release dice now: after a roll, with another roll still to come.
    /// </summary>
    public bool CanHold => !IsGameOver && RollsUsed > 0 && RollsRemaining > 0;

    /// <summary>Whether the current player may record a score now: after at least one roll.</summary>
    public bool CanScore => !IsGameOver && RollsUsed > 0;

    /// <summary>Rolls every die that is not held.</summary>
    /// <exception cref="InvalidOperationException">The game is over or no rolls remain this turn.</exception>
    public void Roll()
    {
        if (!CanRoll)
            throw new InvalidOperationException(IsGameOver ? "The game is over." : "No rolls remain this turn.");

        _dice.Roll();
        RollsUsed++;

        DiceRolled?.Invoke(this, new DiceRolledEventArgs(CurrentPlayer, [.. Dice], RollsRemaining));
        OnStateChanged();
    }

    /// <summary>Holds or releases the die at <paramref name="index"/> (0–4).</summary>
    /// <exception cref="InvalidOperationException">Dice cannot be held right now (see <see cref="CanHold"/>).</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not 0–4.</exception>
    public void SetHeld(int index, bool held)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Scorer.DiceCount);
        if (!CanHold)
            throw new InvalidOperationException("Dice can only be held after a roll, while another roll remains.");
        if (_dice.Held[index] == held)
            return;

        _dice.SetHeld(index, held);
        OnStateChanged();
    }

    /// <summary>Flips the held state of the die at <paramref name="index"/> (0–4).</summary>
    public void ToggleHeld(int index) => SetHeld(index, !Held[index]);

    /// <summary>
    /// The score the current dice would earn in each of the current player's open categories.
    /// Empty before the first roll of a turn.
    /// </summary>
    public IReadOnlyDictionary<Category, int> Preview() =>
        CanScore ? CurrentPlayer.ScoreCard.Preview(Dice) : new Dictionary<Category, int>();

    /// <summary>
    /// Records the current dice in <paramref name="category"/> for the current player, ends the turn,
    /// and passes play to the next player (or ends the game).
    /// </summary>
    /// <returns>The points recorded.</returns>
    /// <exception cref="InvalidOperationException">
    /// The dice have not been rolled this turn, the game is over, or the category is already filled.
    /// </exception>
    public int Score(Category category)
    {
        if (!CanScore)
            throw new InvalidOperationException(IsGameOver ? "The game is over." : "Roll the dice before scoring.");

        var player = CurrentPlayer;
        int points = player.ScoreCard.Record(category, Dice);
        CategoryScored?.Invoke(this, new CategoryScoredEventArgs(player, category, points));

        if (_players.TrueForAll(p => p.ScoreCard.IsComplete))
        {
            IsGameOver = true;
            GameOver?.Invoke(this, new GameOverEventArgs(GetStandings(), _timeProvider.GetUtcNow()));
        }
        else
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % _players.Count;
            RollsUsed = 0;
            _dice.Reset();
            TurnStarted?.Invoke(this, new TurnStartedEventArgs(CurrentPlayer, Round));
        }

        OnStateChanged();
        return points;
    }

    /// <summary>
    /// Plays one visible step of a computer-controlled player's turn: either a roll or the final score.
    /// The GUI calls this repeatedly (with a delay) until it returns <see cref="AiStepResult.Scored"/>.
    /// </summary>
    /// <returns>Which action was taken.</returns>
    /// <exception cref="InvalidOperationException">The game is over, the current player is human, or the strategy returned an invalid choice.</exception>
    public AiStepResult PlayAiStep()
    {
        if (IsGameOver)
            throw new InvalidOperationException("The game is over.");
        var strategy = CurrentPlayer.Strategy
            ?? throw new InvalidOperationException($"{CurrentPlayer.Name} is not an AI player.");

        if (RollsUsed == 0)
        {
            Roll();
            return AiStepResult.Rolled;
        }

        if (RollsRemaining > 0)
        {
            var holds = strategy.ChooseHolds([.. Dice], RollsRemaining, CurrentPlayer.ScoreCard);
            if (holds is not null)
            {
                if (holds.Count != Scorer.DiceCount)
                    throw new InvalidOperationException($"Strategy {strategy.Name} returned {holds.Count} hold flags.");
                for (int i = 0; i < Scorer.DiceCount; i++)
                    _dice.SetHeld(i, holds[i]);
                Roll();
                return AiStepResult.Rolled;
            }
        }

        var category = strategy.ChooseCategory([.. Dice], CurrentPlayer.ScoreCard);
        if (CurrentPlayer.ScoreCard.IsFilled(category))
            throw new InvalidOperationException($"Strategy {strategy.Name} chose {category.DisplayName()}, which is already filled.");
        Score(category);
        return AiStepResult.Scored;
    }

    /// <summary>
    /// The players ordered by total score, highest first. Ties share a rank and keep turn order.
    /// </summary>
    public IReadOnlyList<PlayerStanding> GetStandings()
    {
        var ordered = _players.OrderByDescending(player => player.Total).ToList(); // stable sort keeps turn order on ties
        return ordered
            .Select(player => new PlayerStanding(
                Rank: ordered.FindIndex(other => other.Total == player.Total) + 1,
                player.Name,
                player.Total,
                player.IsAi))
            .ToList();
    }

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
