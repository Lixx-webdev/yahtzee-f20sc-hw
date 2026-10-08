using Yahtzee.Core.AI;

namespace Yahtzee.Core.Model;

/// <summary>
/// A participant in the game: a name, a scorecard, and, for computer-controlled players, a strategy.
/// </summary>
public sealed class Player
{
    /// <summary>Longest allowed player name.</summary>
    public const int MaxNameLength = 20;

    /// <summary>Creates a player.</summary>
    /// <param name="name">Display name; surrounding whitespace is trimmed.</param>
    /// <param name="strategy">The AI strategy, or <see langword="null"/> for a human player.</param>
    /// <param name="scoreCard">An existing scorecard (when loading a save), or <see langword="null"/> for a new empty one.</param>
    /// <exception cref="ArgumentException">The name is blank or longer than <see cref="MaxNameLength"/>.</exception>
    public Player(string name, IPlayerStrategy? strategy = null, ScoreCard? scoreCard = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        name = name.Trim();
        if (name.Length == 0)
            throw new ArgumentException("Player name cannot be blank.", nameof(name));
        if (name.Length > MaxNameLength)
            throw new ArgumentException($"Player name cannot be longer than {MaxNameLength} characters.", nameof(name));

        Name = name;
        Strategy = strategy;
        ScoreCard = scoreCard ?? new ScoreCard();
    }

    /// <summary>The player's display name.</summary>
    public string Name { get; }

    /// <summary>The AI strategy, or <see langword="null"/> for a human player.</summary>
    public IPlayerStrategy? Strategy { get; }

    /// <summary>Whether the player is computer-controlled.</summary>
    public bool IsAi => Strategy is not null;

    /// <summary>The player's scorecard.</summary>
    public ScoreCard ScoreCard { get; }

    /// <summary>The player's current total score.</summary>
    public int Total => ScoreCard.Total;

    /// <inheritdoc/>
    public override string ToString() => IsAi ? $"{Name} (AI: {Strategy!.Name})" : Name;
}
