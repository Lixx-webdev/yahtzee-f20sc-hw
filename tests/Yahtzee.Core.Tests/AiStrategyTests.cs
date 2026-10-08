using Xunit.Abstractions;
using Yahtzee.Core.AI;
using Yahtzee.Core.Game;
using Yahtzee.Core.Model;

namespace Yahtzee.Core.Tests;

public class AiStrategyTests(ITestOutputHelper output)
{
    /// <summary>Plays complete one-player games with the given strategy and returns the average score.</summary>
    private static double AverageScore(Func<int, IPlayerStrategy> createStrategy, int games)
    {
        int total = 0;
        for (int seed = 0; seed < games; seed++)
        {
            // Seeded dice make every run of the test identical.
            var game = new GameEngine([new Player("Bot", createStrategy(seed))], new RandomDiceRoller(new Random(seed)));
            while (!game.IsGameOver)
                game.PlayAiStep(); // throws if the strategy ever makes an illegal move
            total += game.Players[0].Total;
        }
        return (double)total / games;
    }

    // ---- Every strategy plays legal, complete games ----

    [Theory]
    [InlineData(RandomStrategy.StrategyName)]
    [InlineData(GreedyStrategy.StrategyName)]
    [InlineData(ExpectedValueStrategy.StrategyName)]
    public void Strategy_PlaysCompleteLegalGames(string name)
    {
        double average = AverageScore(_ => StrategyCatalog.Create(name), games: 10);

        Assert.InRange(average, 5, 375); // 375 is far above any realistic total under these rules
    }

    [Fact]
    public void StrongerStrategies_ScoreMoreOnAverage()
    {
        double random = AverageScore(seed => new RandomStrategy(new Random(seed + 1000)), games: 30);
        double greedy = AverageScore(_ => new GreedyStrategy(), games: 30);
        double expected = AverageScore(_ => new ExpectedValueStrategy(), games: 30);

        output.WriteLine($"Average over 30 games: random {random:F1}, greedy {greedy:F1}, expected value {expected:F1}");
        Assert.True(greedy > random + 30, $"greedy {greedy:F1} vs random {random:F1}");
        Assert.True(expected > greedy, $"expected value {expected:F1} vs greedy {greedy:F1}");
    }

    // ---- Greedy decisions ----

    [Fact]
    public void Greedy_KeepsMostCommonFace() =>
        Assert.Equal(new[] { false, true, true, false, true }, new GreedyStrategy().ChooseHolds([2, 5, 5, 1, 5], 2, new ScoreCard()));

    [Fact]
    public void Greedy_KeepsRunOfFourWhenStraightIsOpen() =>
        Assert.Equal(new[] { true, true, true, true, false }, new GreedyStrategy().ChooseHolds([1, 2, 3, 4, 6], 2, new ScoreCard()));

    [Fact]
    public void Greedy_StopsOnYahtzee() =>
        Assert.Null(new GreedyStrategy().ChooseHolds([4, 4, 4, 4, 4], 2, new ScoreCard()));

    [Fact]
    public void Greedy_ScoresHighestPoints() =>
        Assert.Equal(Category.LargeStraight, new GreedyStrategy().ChooseCategory([2, 3, 4, 5, 6], new ScoreCard()));

    [Fact]
    public void Greedy_SacrificesCheapestBoxWhenNothingScores()
    {
        // With Twos, Fours, Sixes, Chance and both "of a kind" boxes filled, 2 2 4 4 6 scores 0 in every open box,
        // so Greedy gives up the box that is usually worth least: Aces.
        var card = new ScoreCard(new Dictionary<Category, int> { [Category.Twos] = 4, [Category.Fours] = 8, [Category.Sixes] = 18,
            [Category.Chance] = 20, [Category.ThreeOfAKind] = 20, [Category.FourOfAKind] = 0 });

        Assert.Equal(Category.Aces, new GreedyStrategy().ChooseCategory([2, 2, 4, 4, 6], card));
    }

    // ---- Expected-value decisions ----

    [Fact]
    public void ExpectedValue_StopsOnYahtzee() =>
        Assert.Null(new ExpectedValueStrategy().ChooseHolds([6, 6, 6, 6, 6], 2, new ScoreCard()));

    [Fact]
    public void ExpectedValue_KeepsFourOfAKindAndRerollsTheOddDie()
    {
        var holds = new ExpectedValueStrategy().ChooseHolds([5, 5, 2, 5, 5], 2, new ScoreCard());

        Assert.Equal(new[] { true, true, false, true, true }, holds);
    }

    [Fact]
    public void ExpectedValue_SavesChanceForLaterRatherThanWastingIt() =>
        // 1 1 2 3 5 scores 12 in Chance (10 below par) but only 2 in Aces (par 2): Aces is the better use.
        Assert.Equal(Category.Aces, new ExpectedValueStrategy().ChooseCategory([1, 1, 2, 3, 5], new ScoreCard()));

    // ---- Catalog ----

    [Fact]
    public void Catalog_CreatesEveryListedStrategy() =>
        Assert.All(StrategyCatalog.Names, name => Assert.Equal(name, StrategyCatalog.Create(name).Name));

    [Fact]
    public void Catalog_RejectsUnknownName() =>
        Assert.Throws<ArgumentException>(() => StrategyCatalog.Create("Cheater"));
}
