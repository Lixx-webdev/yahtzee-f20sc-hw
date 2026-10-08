using Yahtzee.Core.Model;
using Yahtzee.Core.Rules;

namespace Yahtzee.Core.AI;

/// <summary>
/// The simplest possible opponent: holds random dice, sometimes stops early, and scores in a random
/// open category. Useful as a baseline to show that the other strategies really are better.
/// </summary>
/// <param name="random">Source of randomness; pass a seeded <see cref="Random"/> for repeatable play.</param>
public sealed class RandomStrategy(Random? random = null) : IPlayerStrategy
{
    /// <summary>The name stored in save files for this strategy.</summary>
    public const string StrategyName = "Random";

    private readonly Random _random = random ?? Random.Shared;

    /// <inheritdoc/>
    public string Name => StrategyName;

    /// <inheritdoc/>
    public IReadOnlyList<bool>? ChooseHolds(IReadOnlyList<int> dice, int rollsRemaining, ScoreCard scoreCard)
    {
        if (_random.Next(3) == 0)
            return null; // stop rolling one time in three
        return [.. Enumerable.Range(0, Scorer.DiceCount).Select(_ => _random.Next(2) == 0)];
    }

    /// <inheritdoc/>
    public Category ChooseCategory(IReadOnlyList<int> dice, ScoreCard scoreCard)
    {
        var open = scoreCard.AvailableCategories.ToList();
        return open[_random.Next(open.Count)];
    }
}
