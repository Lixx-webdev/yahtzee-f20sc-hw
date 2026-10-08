using Yahtzee.Core.Model;
using Yahtzee.Core.Rules;

namespace Yahtzee.Core.Tests;

public class ScorerTests
{
    [Theory]
    [InlineData(Category.Aces, new[] { 1, 1, 1, 4, 5 }, 3)]
    [InlineData(Category.Aces, new[] { 2, 3, 4, 5, 6 }, 0)]
    [InlineData(Category.Twos, new[] { 2, 2, 2, 5, 6 }, 6)]
    [InlineData(Category.Threes, new[] { 3, 3, 3, 3, 1 }, 12)]
    [InlineData(Category.Fours, new[] { 4, 4, 5, 5, 6 }, 8)]
    [InlineData(Category.Fives, new[] { 1, 1, 2, 2, 5 }, 5)]
    [InlineData(Category.Sixes, new[] { 3, 3, 6, 6, 6 }, 18)]
    [InlineData(Category.Sixes, new[] { 6, 6, 6, 6, 6 }, 30)]
    public void UpperSection_SumsMatchingFace(Category category, int[] dice, int expected) =>
        Assert.Equal(expected, Scorer.Score(category, dice));

    [Theory]
    [InlineData(new[] { 1, 1, 3, 4, 5 }, 14)]
    [InlineData(new[] { 6, 6, 6, 6, 6 }, 30)]
    [InlineData(new[] { 1, 1, 1, 1, 1 }, 5)]
    public void Chance_SumsAllDice(int[] dice, int expected) =>
        Assert.Equal(expected, Scorer.Score(Category.Chance, dice));

    [Theory]
    [InlineData(new[] { 2, 3, 4, 4, 4 }, 17)]
    [InlineData(new[] { 4, 4, 4, 4, 1 }, 17)] // four of a kind also counts
    [InlineData(new[] { 5, 5, 5, 5, 5 }, 25)] // a Yahtzee also counts
    [InlineData(new[] { 2, 2, 3, 3, 4 }, 0)]
    public void ThreeOfAKind_RequiresAtLeastThreeSame(int[] dice, int expected) =>
        Assert.Equal(expected, Scorer.Score(Category.ThreeOfAKind, dice));

    [Theory]
    [InlineData(new[] { 4, 5, 5, 5, 5 }, 24)]
    [InlineData(new[] { 3, 3, 3, 3, 3 }, 15)] // a Yahtzee also counts
    [InlineData(new[] { 5, 5, 5, 4, 4 }, 0)]
    public void FourOfAKind_RequiresAtLeastFourSame(int[] dice, int expected) =>
        Assert.Equal(expected, Scorer.Score(Category.FourOfAKind, dice));

    [Theory]
    [InlineData(new[] { 2, 2, 5, 5, 5 }, 25)]
    [InlineData(new[] { 5, 2, 5, 2, 5 }, 25)] // order does not matter
    [InlineData(new[] { 2, 2, 5, 5, 6 }, 0)]  // two pair
    [InlineData(new[] { 2, 5, 5, 5, 5 }, 0)]  // four of a kind
    public void FullHouse_RequiresThreeAndTwo(int[] dice, int expected) =>
        Assert.Equal(expected, Scorer.Score(Category.FullHouse, dice));

    [Fact]
    public void FullHouse_RejectsYahtzee() =>
        Assert.Equal(0, Scorer.Score(Category.FullHouse, [4, 4, 4, 4, 4]));

    [Theory]
    [InlineData(new[] { 1, 2, 3, 4, 6 }, 30)]
    [InlineData(new[] { 2, 3, 4, 5, 5 }, 30)] // duplicate inside the run
    [InlineData(new[] { 6, 4, 3, 1, 5 }, 30)] // unsorted 3-4-5-6
    [InlineData(new[] { 1, 2, 3, 4, 5 }, 30)] // a large straight is also a small straight
    [InlineData(new[] { 1, 2, 3, 5, 6 }, 0)]  // gap breaks the run
    [InlineData(new[] { 1, 1, 2, 2, 3 }, 0)]
    public void SmallStraight_RequiresFourSequential(int[] dice, int expected) =>
        Assert.Equal(expected, Scorer.Score(Category.SmallStraight, dice));

    [Theory]
    [InlineData(new[] { 1, 2, 3, 4, 5 }, 40)]
    [InlineData(new[] { 6, 5, 4, 3, 2 }, 40)]
    [InlineData(new[] { 1, 2, 3, 4, 6 }, 0)]
    [InlineData(new[] { 1, 2, 3, 4, 4 }, 0)]
    public void LargeStraight_RequiresFiveSequential(int[] dice, int expected) =>
        Assert.Equal(expected, Scorer.Score(Category.LargeStraight, dice));

    [Theory]
    [InlineData(new[] { 3, 3, 3, 3, 3 }, 50)]
    [InlineData(new[] { 3, 3, 3, 3, 2 }, 0)]
    public void Yahtzee_RequiresFiveSame(int[] dice, int expected) =>
        Assert.Equal(expected, Scorer.Score(Category.Yahtzee, dice));

    [Theory]
    [InlineData(new[] { 1, 2, 3, 4 })]
    [InlineData(new[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(new[] { 0, 2, 3, 4, 5 })]
    [InlineData(new[] { 1, 2, 3, 4, 7 })]
    public void Score_RejectsInvalidDice(int[] dice) =>
        Assert.Throws<ArgumentException>(() => Scorer.Score(Category.Chance, dice));

    [Fact]
    public void Preview_ScoresOnlyRequestedCategories()
    {
        var preview = Scorer.Preview([2, 2, 5, 5, 5], [Category.FullHouse, Category.Fives, Category.Yahtzee]);

        Assert.Equal(3, preview.Count);
        Assert.Equal(25, preview[Category.FullHouse]);
        Assert.Equal(15, preview[Category.Fives]);
        Assert.Equal(0, preview[Category.Yahtzee]);
    }
}
