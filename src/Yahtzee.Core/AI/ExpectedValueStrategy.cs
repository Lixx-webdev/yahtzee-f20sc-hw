using Yahtzee.Core.Model;
using Yahtzee.Core.Rules;

namespace Yahtzee.Core.AI;

/// <summary>
/// The strongest opponent. Before each roll it considers all 32 ways of holding dice and, for each,
/// averages the result over every possible outcome of the dice it would re-roll. It then makes the
/// choice with the best average, or stops if no re-roll is expected to improve on the current dice.
/// </summary>
/// <remarks>
/// A result is valued as "points above par" in the best open category (see <see cref="ScoringHeuristics"/>),
/// so the strategy saves high-value boxes for good rolls. It looks one roll ahead at a time.
/// </remarks>
public sealed class ExpectedValueStrategy : IPlayerStrategy
{
    /// <summary>The name stored in save files for this strategy.</summary>
    public const string StrategyName = "ExpectedValue";

    /// <summary>A re-roll must be expected to gain at least this much to be worth it.</summary>
    private const double MinimumGain = 0.01;

    /// <inheritdoc/>
    public string Name => StrategyName;

    /// <inheritdoc/>
    public IReadOnlyList<bool>? ChooseHolds(IReadOnlyList<int> dice, int rollsRemaining, ScoreCard scoreCard)
    {
        var evaluator = new PositionEvaluator(scoreCard);
        double bestValue = evaluator.Value(dice);
        bool[]? bestHolds = null;

        // Each bit of the mask says whether that die is held; 2^5 = 32 possibilities.
        for (int mask = 0; mask < 1 << Scorer.DiceCount; mask++)
        {
            bool[] holds = [.. Enumerable.Range(0, Scorer.DiceCount).Select(i => (mask & (1 << i)) != 0)];
            if (holds.All(held => held))
                continue; // holding everything is the same as stopping

            double expected = evaluator.ExpectedValueAfterReroll(dice, holds);
            if (expected > bestValue + MinimumGain)
            {
                bestValue = expected;
                bestHolds = holds;
            }
        }
        return bestHolds;
    }

    /// <inheritdoc/>
    public Category ChooseCategory(IReadOnlyList<int> dice, ScoreCard scoreCard) =>
        ScoringHeuristics.BestRelativeCategory(dice, scoreCard);

    /// <summary>
    /// Values dice positions for one scorecard. Positions are cached by their sorted dice, because the
    /// order of the dice never affects scoring and there are only 252 different sorted hands.
    /// </summary>
    private sealed class PositionEvaluator(ScoreCard scoreCard)
    {
        private readonly List<Category> _open = [.. scoreCard.AvailableCategories];
        private readonly Dictionary<int, double> _cache = [];

        /// <summary>Value of finishing the turn with these dice: the best "points above par" available.</summary>
        public double Value(IReadOnlyList<int> dice)
        {
            int[] sorted = [.. dice.Order()];
            int key = sorted.Aggregate(0, (acc, value) => acc * 7 + value);
            if (!_cache.TryGetValue(key, out double value))
            {
                value = _open.Max(category => ScoringHeuristics.ValueAbovePar(category, sorted));
                _cache[key] = value;
            }
            return value;
        }

        /// <summary>Average <see cref="Value"/> over every equally likely outcome of re-rolling the unheld dice.</summary>
        public double ExpectedValueAfterReroll(IReadOnlyList<int> dice, IReadOnlyList<bool> holds)
        {
            int[] rerolled = [.. Enumerable.Range(0, Scorer.DiceCount).Where(i => !holds[i])];
            int outcomes = (int)Math.Pow(6, rerolled.Length);
            int[] next = [.. dice];
            double total = 0;

            for (int outcome = 0; outcome < outcomes; outcome++)
            {
                // Treat the outcome number as base-6 digits: one digit per re-rolled die.
                int code = outcome;
                foreach (int index in rerolled)
                {
                    next[index] = code % 6 + 1;
                    code /= 6;
                }
                total += Value(next);
            }
            return total / outcomes;
        }
    }
}
