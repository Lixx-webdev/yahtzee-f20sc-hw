using Yahtzee.Core.Model;

namespace Yahtzee.Core.Tests;

public class PlayerTests
{
    [Fact]
    public void Constructor_TrimsName()
    {
        var player = new Player("  Alice  ");

        Assert.Equal("Alice", player.Name);
        Assert.False(player.IsAi);
        Assert.Equal(0, player.Total);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ThisNameIsWayTooLongToFit")]
    public void Constructor_RejectsInvalidName(string name) =>
        Assert.Throws<ArgumentException>(() => new Player(name));

    [Fact]
    public void PlayerWithStrategy_IsAi() =>
        Assert.True(new Player("Bot", new ScriptedStrategy()).IsAi);

    [Fact]
    public void Constructor_KeepsGivenScoreCard()
    {
        var card = new ScoreCard(new Dictionary<Category, int> { [Category.Chance] = 20 });

        var player = new Player("Alice", scoreCard: card);

        Assert.Same(card, player.ScoreCard);
        Assert.Equal(20, player.Total);
    }
}
