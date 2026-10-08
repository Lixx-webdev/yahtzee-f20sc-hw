namespace Yahtzee.Core.Model;

/// <summary>
/// The 13 scoring categories (boxes) on a Yahtzee scorecard, in the order shown in
/// Figure 1 of the coursework specification.
/// </summary>
/// <remarks>
/// The numeric values are stable and are used by the save-file format and the database,
/// so new members must not be inserted or reordered.
/// </remarks>
public enum Category
{
    /// <summary>Sum of dice showing 1.</summary>
    Aces = 0,
    /// <summary>Sum of dice showing 2.</summary>
    Twos = 1,
    /// <summary>Sum of dice showing 3.</summary>
    Threes = 2,
    /// <summary>Sum of dice showing 4.</summary>
    Fours = 3,
    /// <summary>Sum of dice showing 5.</summary>
    Fives = 4,
    /// <summary>Sum of dice showing 6.</summary>
    Sixes = 5,
    /// <summary>Sum of all dice, any combination.</summary>
    Chance = 6,
    /// <summary>Sum of all dice if at least three are the same.</summary>
    ThreeOfAKind = 7,
    /// <summary>Sum of all dice if at least four are the same.</summary>
    FourOfAKind = 8,
    /// <summary>25 points for three of one value and two of another.</summary>
    FullHouse = 9,
    /// <summary>30 points for four sequential dice.</summary>
    SmallStraight = 10,
    /// <summary>40 points for five sequential dice.</summary>
    LargeStraight = 11,
    /// <summary>50 points for all five dice the same.</summary>
    Yahtzee = 12,
}

/// <summary>Helper members for <see cref="Category"/>.</summary>
public static class CategoryExtensions
{
    /// <summary>All categories in scorecard order.</summary>
    public static IReadOnlyList<Category> All { get; } = Enum.GetValues<Category>();

    /// <summary>Human-readable name for display in the GUI and reports.</summary>
    public static string DisplayName(this Category category) => category switch
    {
        Category.ThreeOfAKind => "Three of a Kind",
        Category.FourOfAKind => "Four of a Kind",
        Category.FullHouse => "Full House",
        Category.SmallStraight => "Small Straight",
        Category.LargeStraight => "Large Straight",
        _ => category.ToString(),
    };

    /// <summary>
    /// For the upper-section categories (Aces to Sixes), the die face they count;
    /// otherwise <see langword="null"/>.
    /// </summary>
    public static int? Face(this Category category) =>
        category is >= Category.Aces and <= Category.Sixes ? (int)category + 1 : null;
}
