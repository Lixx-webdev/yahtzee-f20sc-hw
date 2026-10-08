using Yahtzee.Core.Model;

namespace Yahtzee.Core.Rules;

/// <summary>
/// Pure scoring functions implementing the simplified rules from Appendix B of the
/// coursework specification. There is no upper-section bonus, Yahtzee bonus or joker rule.
/// </summary>
public static class Scorer
{
    /// <summary>Number of dice in play.</summary>
    public const int DiceCount = 5;

    /// <summary>Lowest face value of a die.</summary>
    public const int MinFace = 1;

    /// <summary>Highest face value of a die.</summary>
    public const int MaxFace = 6;

    /// <summary>Fixed score for a Full House.</summary>
    public const int FullHouseScore = 25;

    /// <summary>Fixed score for a Small Straight.</summary>
    public const int SmallStraightScore = 30;

    /// <summary>Fixed score for a Large Straight.</summary>
    public const int LargeStraightScore = 40;

    /// <summary>Fixed score for a Yahtzee.</summary>
    public const int YahtzeeScore = 50;

    /// <summary>
    /// Calculates the score the given dice would earn in <paramref name="category"/>.
    /// Dice that do not meet the category's requirement score 0.
    /// </summary>
    /// <param name="category">The category to score.</param>
    /// <param name="dice">Exactly five die values, each between 1 and 6.</param>
    /// <exception cref="ArgumentException">The dice are not five values in the range 1–6.</exception>
    public static int Score(Category category, IReadOnlyList<int> dice)
    {
        ValidateDice(dice);
        var counts = CountFaces(dice);
        int sum = dice.Sum();

        return category switch
        {
            >= Category.Aces and <= Category.Sixes => SumOfFace(counts, category.Face()!.Value),
            Category.Chance => sum,
            Category.ThreeOfAKind => counts.Max() >= 3 ? sum : 0,
            Category.FourOfAKind => counts.Max() >= 4 ? sum : 0,
            // Exactly a 3 + 2 split: a Yahtzee (5 of one value) is not a Full House.
            Category.FullHouse => counts.Contains(3) && counts.Contains(2) ? FullHouseScore : 0,
            Category.SmallStraight => LongestRun(counts) >= 4 ? SmallStraightScore : 0,
            Category.LargeStraight => LongestRun(counts) == 5 ? LargeStraightScore : 0,
            Category.Yahtzee => counts.Contains(DiceCount) ? YahtzeeScore : 0,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown category."),
        };
    }

    /// <summary>
    /// Scores the dice against each of the given categories, for the score preview.
    /// </summary>
    /// <param name="dice">Exactly five die values, each between 1 and 6.</param>
    /// <param name="categories">The categories to preview, typically the player's unfilled ones.</param>
    /// <returns>A map from each category to the score it would earn.</returns>
    public static IReadOnlyDictionary<Category, int> Preview(IReadOnlyList<int> dice, IEnumerable<Category> categories) =>
        categories.ToDictionary(category => category, category => Score(category, dice));

    /// <summary>
    /// Throws if <paramref name="dice"/> is not exactly five values in the range 1–6.
    /// </summary>
    /// <exception cref="ArgumentException">The dice are invalid.</exception>
    public static void ValidateDice(IReadOnlyList<int> dice)
    {
        ArgumentNullException.ThrowIfNull(dice);
        if (dice.Count != DiceCount)
            throw new ArgumentException($"Expected {DiceCount} dice but got {dice.Count}.", nameof(dice));
        if (dice.Any(value => value is < MinFace or > MaxFace))
            throw new ArgumentException($"Die values must be between {MinFace} and {MaxFace}.", nameof(dice));
    }

    /// <summary>Returns how many dice show each face; index 0 is unused so index = face.</summary>
    private static int[] CountFaces(IReadOnlyList<int> dice)
    {
        var counts = new int[MaxFace + 1];
        foreach (int value in dice)
            counts[value]++;
        return counts;
    }

    private static int SumOfFace(int[] counts, int face) => counts[face] * face;

    /// <summary>Length of the longest run of consecutive faces present at least once.</summary>
    private static int LongestRun(int[] counts)
    {
        int longest = 0, current = 0;
        for (int face = MinFace; face <= MaxFace; face++)
        {
            current = counts[face] > 0 ? current + 1 : 0;
            longest = Math.Max(longest, current);
        }
        return longest;
    }
}
