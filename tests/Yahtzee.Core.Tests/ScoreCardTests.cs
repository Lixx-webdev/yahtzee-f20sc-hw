using Yahtzee.Core.Model;

namespace Yahtzee.Core.Tests;

public class ScoreCardTests
{
    [Fact]
    public void NewCard_IsEmpty()
    {
        var card = new ScoreCard();

        Assert.Equal(13, card.AvailableCategories.Count());
        Assert.False(card.IsComplete);
        Assert.Equal(0, card.Total);
        Assert.Null(card.GetScore(Category.Chance));
    }

    [Fact]
    public void Record_StoresScoreAndRemovesCategory()
    {
        var card = new ScoreCard();

        int score = card.Record(Category.Sixes, [6, 6, 6, 1, 2]);

        Assert.Equal(18, score);
        Assert.Equal(18, card.GetScore(Category.Sixes));
        Assert.True(card.IsFilled(Category.Sixes));
        Assert.DoesNotContain(Category.Sixes, card.AvailableCategories);
    }

    [Fact]
    public void Record_NonMatchingDiceScoresZero()
    {
        var card = new ScoreCard();

        Assert.Equal(0, card.Record(Category.Yahtzee, [1, 2, 3, 4, 6]));
        Assert.True(card.IsFilled(Category.Yahtzee));
    }

    [Fact]
    public void Record_SameCategoryTwice_Throws()
    {
        var card = new ScoreCard();
        card.Record(Category.Chance, [1, 2, 3, 4, 5]);

        Assert.Throws<InvalidOperationException>(() => card.Record(Category.Chance, [6, 6, 6, 6, 6]));
    }

    [Fact]
    public void FillingAllCategories_CompletesCardAndSumsTotal()
    {
        var card = new ScoreCard();
        int[] dice = [5, 5, 5, 5, 5];

        foreach (var category in CategoryExtensions.All)
            card.Record(category, dice);

        Assert.True(card.IsComplete);
        Assert.Empty(card.AvailableCategories);
        // Fives 25 + Chance 25 + 3oak 25 + 4oak 25 + Yahtzee 50; everything else 0.
        Assert.Equal(150, card.Total);
    }

    [Fact]
    public void Preview_ExcludesFilledCategories()
    {
        var card = new ScoreCard();
        card.Record(Category.FullHouse, [2, 2, 3, 3, 3]);

        var preview = card.Preview([2, 2, 3, 3, 3]);

        Assert.Equal(12, preview.Count);
        Assert.False(preview.ContainsKey(Category.FullHouse));
        Assert.Equal(9, preview[Category.Threes]);
    }

    [Fact]
    public void RestoreConstructor_KeepsScores()
    {
        var card = new ScoreCard(new Dictionary<Category, int> { [Category.Aces] = 3, [Category.Yahtzee] = 50 });

        Assert.Equal(53, card.Total);
        Assert.Equal(11, card.AvailableCategories.Count());
    }

    [Fact]
    public void RestoreConstructor_RejectsNegativeScore() =>
        Assert.Throws<ArgumentException>(() => new ScoreCard(new Dictionary<Category, int> { [Category.Aces] = -1 }));
}
