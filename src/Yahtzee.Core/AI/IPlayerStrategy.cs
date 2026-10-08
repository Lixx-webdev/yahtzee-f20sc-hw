using Yahtzee.Core.Model;

namespace Yahtzee.Core.AI;

/// <summary>
/// Decision-making for a computer-controlled player. The game engine asks the strategy
/// what to do after each roll; the strategy never changes game state itself.
/// </summary>
public interface IPlayerStrategy
{
    /// <summary>Short display name of the strategy, also stored in save files.</summary>
    string Name { get; }

    /// <summary>
    /// Decides which dice to keep before the next roll, or to stop rolling.
    /// Only called when at least one roll remains in the turn.
    /// </summary>
    /// <param name="dice">The five current die values.</param>
    /// <param name="rollsRemaining">Rolls left in this turn (1 or 2).</param>
    /// <param name="scoreCard">The player's scorecard, for checking which categories are still open.</param>
    /// <returns>
    /// Five flags marking the dice to hold, or <see langword="null"/> to stop rolling and score now.
    /// </returns>
    IReadOnlyList<bool>? ChooseHolds(IReadOnlyList<int> dice, int rollsRemaining, ScoreCard scoreCard);

    /// <summary>Chooses which open category to score the final dice in.</summary>
    /// <param name="dice">The five final die values.</param>
    /// <param name="scoreCard">The player's scorecard; the result must be one of its available categories.</param>
    Category ChooseCategory(IReadOnlyList<int> dice, ScoreCard scoreCard);
}
