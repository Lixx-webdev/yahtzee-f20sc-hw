using Yahtzee.Core.Model;
using Yahtzee.Core.Rules;

namespace Yahtzee.Core.AI;

/// <summary>
/// Shared judgement used by the computer strategies: how good a score in a category is compared
/// with what a reasonable player usually gets there.
/// </summary>
internal static class ScoringHeuristics
{
    /// <summary>
    /// "Par" for each category: a rough typical score for a sensible player. Scoring above par is a
    /// good use of the box; scoring below par wastes it. These are hand-picked estimates, not exact values.
    /// </summary>
    private static readonly Dictionary<Category, double> Par = new()
    {
        [Category.Aces] = 2, [Category.Twos] = 5, [Category.Threes] = 8.5, [Category.Fours] = 12,
        [Category.Fives] = 15.5, [Category.Sixes] = 19, [Category.Chance] = 22, [Category.ThreeOfAKind] = 16,
        [Category.FourOfAKind] = 7, [Category.FullHouse] = 15, [Category.SmallStraight] = 18,
        [Category.LargeStraight] = 11, [Category.Yahtzee] = 9,
    };

    /// <summary>How much better than par the dice would score in <paramref name="category"/>.</summary>
    public static double ValueAbovePar(Category category, IReadOnlyList<int> dice) =>
        Scorer.Score(category, dice) - Par[category];

    /// <summary>The open category in which the dice score furthest above (or least below) par.</summary>
    public static Category BestRelativeCategory(IReadOnlyList<int> dice, ScoreCard scoreCard) =>
        scoreCard.AvailableCategories.MaxBy(category => ValueAbovePar(category, dice));

    /// <summary>The open category with the lowest par: the cheapest box to sacrifice.</summary>
    public static Category CheapestCategory(IEnumerable<Category> categories) =>
        categories.MinBy(category => Par[category]);
}
