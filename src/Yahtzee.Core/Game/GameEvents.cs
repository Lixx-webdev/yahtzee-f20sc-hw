using Yahtzee.Core.Model;

namespace Yahtzee.Core.Game;

/// <summary>Raised when a new turn begins.</summary>
/// <param name="Player">The player whose turn it is.</param>
/// <param name="Round">The round number, 1–13.</param>
public sealed record TurnStartedEventArgs(Player Player, int Round);

/// <summary>Raised after the dice are rolled.</summary>
/// <param name="Player">The player who rolled.</param>
/// <param name="Dice">The five die values after the roll.</param>
/// <param name="RollsRemaining">Rolls left in this turn.</param>
public sealed record DiceRolledEventArgs(Player Player, IReadOnlyList<int> Dice, int RollsRemaining);

/// <summary>Raised after a player records a score.</summary>
/// <param name="Player">The player who scored.</param>
/// <param name="Category">The category that was filled.</param>
/// <param name="Score">The points recorded.</param>
public sealed record CategoryScoredEventArgs(Player Player, Category Category, int Score);

/// <summary>Raised once, when every player has filled all 13 categories.</summary>
/// <param name="Standings">Final results, ordered from highest to lowest score.</param>
/// <param name="CompletedAt">When the game finished.</param>
public sealed record GameOverEventArgs(IReadOnlyList<PlayerStanding> Standings, DateTimeOffset CompletedAt);

/// <summary>One player's position in the results table.</summary>
/// <param name="Rank">1 for the winner. Tied players share a rank (1, 1, 3, ...).</param>
/// <param name="Name">The player's name.</param>
/// <param name="Score">The player's total score.</param>
/// <param name="IsAi">Whether the player was computer-controlled.</param>
public sealed record PlayerStanding(int Rank, string Name, int Score, bool IsAi);

/// <summary>The single action taken by <see cref="GameEngine.PlayAiStep"/>.</summary>
public enum AiStepResult
{
    /// <summary>The AI rolled the dice (after choosing which to hold, if any).</summary>
    Rolled,

    /// <summary>The AI recorded a score, ending its turn.</summary>
    Scored,
}
