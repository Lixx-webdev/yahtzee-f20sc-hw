using Yahtzee.Core.Model;
using Yahtzee.Core.Rules;

namespace Yahtzee.Core.AI;

/// <summary>
/// A simple rule-based opponent. It chases straights when it has four in a row, otherwise collects as
/// many dice of one face as it can, and always scores the highest number of points available.
/// </summary>
public sealed class GreedyStrategy : IPlayerStrategy
{
    /// <summary>The name stored in save files for this strategy.</summary>
    public const string StrategyName = "Greedy";

    /// <inheritdoc/>
    public string Name => StrategyName;

    /// <inheritdoc/>
    public IReadOnlyList<bool>? ChooseHolds(IReadOnlyList<int> dice, int rollsRemaining, ScoreCard scoreCard)
    {
        bool IsOpenAndMade(Category category) => !scoreCard.IsFilled(category) && Scorer.Score(category, dice) > 0;

        // Nothing can beat these, so stop rolling.
        if (IsOpenAndMade(Category.Yahtzee) || IsOpenAndMade(Category.LargeStraight))
            return null;

        // Four in a row with a straight still open: keep one die of each face in the run.
        bool straightOpen = !scoreCard.IsFilled(Category.SmallStraight) || !scoreCard.IsFilled(Category.LargeStraight);
        if (straightOpen && FindRunOfFour(dice) is int runStart)
        {
            var kept = new HashSet<int>();
            return [.. dice.Select(value => value >= runStart && value < runStart + 4 && kept.Add(value))];
        }

        // Otherwise keep every die showing the most common face (the higher face wins a tie).
        int target = dice.GroupBy(value => value)
            .OrderByDescending(group => group.Count())
            .ThenByDescending(group => group.Key)
            .First().Key;
        bool[] holds = [.. dice.Select(value => value == target)];
        return holds.All(held => held) ? null : holds; // holding all five would waste the roll
    }

    /// <inheritdoc/>
    public Category ChooseCategory(IReadOnlyList<int> dice, ScoreCard scoreCard)
    {
        var open = scoreCard.AvailableCategories.ToList();
        int best = open.Max(category => Scorer.Score(category, dice));
        // Among the categories giving the most points, use up the one that is usually worth least.
        return ScoringHeuristics.CheapestCategory(open.Where(category => Scorer.Score(category, dice) == best));
    }

    /// <summary>The lowest face of a run of four consecutive faces in the dice, or <see langword="null"/>.</summary>
    private static int? FindRunOfFour(IReadOnlyList<int> dice)
    {
        for (int start = 3; start >= 1; start--) // prefer the highest run
        {
            if (Enumerable.Range(start, 4).All(dice.Contains))
                return start;
        }
        return null;
    }
}
