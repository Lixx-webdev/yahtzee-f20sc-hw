using Yahtzee.Core.Game;
using Yahtzee.Core.Model;

namespace Yahtzee.Core.Tests;

public class GameEngineTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    /// <summary>A roller with enough fives for any game in these tests.</summary>
    private static FixedDiceRoller Fives() => new([.. Enumerable.Repeat(5, 1000)]);

    private static GameEngine NewGame(IDiceRoller roller, params string[] names) =>
        new(names.Select(name => new Player(name)), roller, new ManualTimeProvider(Start));

    // ---- Construction ----

    [Fact]
    public void NewGame_StartsWithFirstPlayerUnrolled()
    {
        var game = NewGame(Fives(), "Alice", "Bob");

        Assert.Equal("Alice", game.CurrentPlayer.Name);
        Assert.Equal(1, game.Round);
        Assert.Equal(0, game.RollsUsed);
        Assert.Equal(3, game.RollsRemaining);
        Assert.True(game.CanRoll);
        Assert.False(game.CanHold);
        Assert.False(game.CanScore);
        Assert.False(game.IsGameOver);
        Assert.Equal(Start, game.StartedAt);
    }

    [Fact]
    public void Constructor_RejectsNoPlayers() =>
        Assert.Throws<ArgumentException>(() => NewGame(Fives()));

    [Fact]
    public void Constructor_RejectsTooManyPlayers() =>
        Assert.Throws<ArgumentException>(() => NewGame(Fives(), "A", "B", "C", "D", "E", "F", "G"));

    [Fact]
    public void Constructor_RejectsDuplicateNamesIgnoringCase() =>
        Assert.Throws<ArgumentException>(() => NewGame(Fives(), "Alice", "alice"));

    // ---- Rolling and holding ----

    [Fact]
    public void Roll_UsesUpRollsAndRaisesEvent()
    {
        var game = NewGame(new FixedDiceRoller(1, 2, 3, 4, 5), "Alice");
        DiceRolledEventArgs? rolled = null;
        game.DiceRolled += (_, e) => rolled = e;

        game.Roll();

        Assert.Equal(1, game.RollsUsed);
        Assert.Equal(2, game.RollsRemaining);
        Assert.NotNull(rolled);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, rolled.Dice);
        Assert.Equal(2, rolled.RollsRemaining);
    }

    [Fact]
    public void Roll_FourthTimeInTurn_Throws()
    {
        var game = NewGame(Fives(), "Alice");
        game.Roll();
        game.Roll();
        game.Roll();

        Assert.False(game.CanRoll);
        Assert.Throws<InvalidOperationException>(game.Roll);
    }

    [Fact]
    public void HeldDice_AreKeptOnReroll()
    {
        var game = NewGame(new FixedDiceRoller(6, 6, 1, 2, 3, 6, 4, 5), "Alice");
        game.Roll();
        game.SetHeld(0, true);
        game.ToggleHeld(1);

        game.Roll();

        Assert.Equal(new[] { 6, 6, 6, 4, 5 }, game.Dice);
    }

    [Fact]
    public void Hold_BeforeFirstRoll_Throws()
    {
        var game = NewGame(Fives(), "Alice");

        Assert.Throws<InvalidOperationException>(() => game.SetHeld(0, true));
    }

    [Fact]
    public void Hold_AfterThirdRoll_Throws()
    {
        var game = NewGame(Fives(), "Alice");
        game.Roll();
        game.Roll();
        game.Roll();

        Assert.False(game.CanHold);
        Assert.Throws<InvalidOperationException>(() => game.SetHeld(0, true));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void Hold_InvalidIndex_Throws(int index)
    {
        var game = NewGame(Fives(), "Alice");
        game.Roll();

        Assert.Throws<ArgumentOutOfRangeException>(() => game.SetHeld(index, true));
    }

    // ---- Scoring and turn order ----

    [Fact]
    public void Score_BeforeRolling_Throws()
    {
        var game = NewGame(Fives(), "Alice");

        Assert.Throws<InvalidOperationException>(() => game.Score(Category.Chance));
    }

    [Fact]
    public void Preview_IsEmptyBeforeRollAndCoversOpenCategoriesAfter()
    {
        var game = NewGame(new FixedDiceRoller(2, 2, 3, 3, 3), "Alice");
        Assert.Empty(game.Preview());

        game.Roll();
        var preview = game.Preview();

        Assert.Equal(13, preview.Count);
        Assert.Equal(25, preview[Category.FullHouse]);
        Assert.Equal(13, preview[Category.Chance]);
    }

    [Fact]
    public void Score_RecordsPointsAndPassesTurnToNextPlayer()
    {
        var game = NewGame(Fives(), "Alice", "Bob");
        TurnStartedEventArgs? turn = null;
        CategoryScoredEventArgs? scored = null;
        game.TurnStarted += (_, e) => turn = e;
        game.CategoryScored += (_, e) => scored = e;
        game.Roll();
        game.SetHeld(0, true);

        int points = game.Score(Category.Yahtzee);

        Assert.Equal(50, points);
        Assert.Equal(50, game.Players[0].ScoreCard.GetScore(Category.Yahtzee));
        Assert.Equal(new CategoryScoredEventArgs(game.Players[0], Category.Yahtzee, 50), scored);
        Assert.Equal("Bob", game.CurrentPlayer.Name);
        Assert.Equal(new TurnStartedEventArgs(game.Players[1], 1), turn);
        Assert.Equal(0, game.RollsUsed);
        Assert.All(game.Held, held => Assert.False(held));
    }

    [Fact]
    public void Round_AdvancesAfterEveryPlayerHasHadATurn()
    {
        var game = NewGame(Fives(), "Alice", "Bob");

        game.Roll();
        game.Score(Category.Aces);
        Assert.Equal(1, game.Round); // Bob's first turn

        game.Roll();
        game.Score(Category.Aces);
        Assert.Equal(2, game.Round); // back to Alice
        Assert.Equal("Alice", game.CurrentPlayer.Name);
    }

    [Fact]
    public void Score_FilledCategory_ThrowsAndKeepsTurn()
    {
        var game = NewGame(Fives(), "Alice");
        game.Roll();
        game.Score(Category.Chance);
        game.Roll();

        Assert.Throws<InvalidOperationException>(() => game.Score(Category.Chance));
        Assert.Equal(1, game.RollsUsed);
    }

    [Fact]
    public void StateChanged_RaisedForEveryAction()
    {
        var game = NewGame(Fives(), "Alice");
        int changes = 0;
        game.StateChanged += (_, _) => changes++;

        game.Roll();
        game.SetHeld(0, true);
        game.SetHeld(0, true); // no change, no event
        game.Score(Category.Fives);

        Assert.Equal(3, changes);
    }

    // ---- Game over ----

    [Fact]
    public void SinglePlayerGame_EndsAfterThirteenTurns()
    {
        var clock = new ManualTimeProvider(Start);
        var game = new GameEngine([new Player("Alice")], Fives(), clock);
        GameOverEventArgs? over = null;
        game.GameOver += (_, e) => over = e;
        clock.Now = Start.AddMinutes(10);

        foreach (var category in CategoryExtensions.All)
        {
            Assert.Null(over);
            game.Roll();
            game.Score(category);
        }

        Assert.True(game.IsGameOver);
        Assert.False(game.CanRoll);
        Assert.False(game.CanScore);
        Assert.NotNull(over);
        Assert.Equal(Start.AddMinutes(10), over.CompletedAt);
        // All fives: Fives 25 + Chance 25 + 3oak 25 + 4oak 25 + Yahtzee 50.
        Assert.Equal(new[] { new PlayerStanding(1, "Alice", 150, false) }, over.Standings);
        Assert.Throws<InvalidOperationException>(game.Roll);
    }

    [Fact]
    public void MultiPlayerGame_EndsOnlyWhenLastPlayerFinishes()
    {
        var game = NewGame(Fives(), "Alice", "Bob");
        int gameOverCount = 0;
        game.GameOver += (_, _) => gameOverCount++;

        foreach (var category in CategoryExtensions.All)
        {
            game.Roll();
            game.Score(category); // Alice
            Assert.False(game.IsGameOver);
            game.Roll();
            game.Score(category); // Bob
        }

        Assert.True(game.IsGameOver);
        Assert.Equal(1, gameOverCount);
    }

    [Fact]
    public void GetStandings_OrdersByScoreAndSharesRankOnTies()
    {
        var game = NewGame(Fives(), "Alice", "Bob", "Carol");
        game.Roll(); game.Score(Category.Aces);    // Alice 0
        game.Roll(); game.Score(Category.Yahtzee); // Bob 50
        game.Roll(); game.Score(Category.Twos);    // Carol 0

        var standings = game.GetStandings();

        Assert.Equal(
            new[]
            {
                new PlayerStanding(1, "Bob", 50, false),
                new PlayerStanding(2, "Alice", 0, false),
                new PlayerStanding(2, "Carol", 0, false),
            },
            standings);
    }

    // ---- AI steps ----

    [Fact]
    public void PlayAiStep_RollsThenRerollsWithChosenHoldsThenScores()
    {
        var strategy = new ScriptedStrategy { HoldsToReturn = [true, true, false, false, false] };
        var game = new GameEngine(
            [new Player("Bot", strategy)],
            new FixedDiceRoller(6, 6, 1, 2, 3, 6, 6, 6, 6, 6, 6),
            new ManualTimeProvider(Start));

        Assert.Equal(AiStepResult.Rolled, game.PlayAiStep()); // first roll: 6 6 1 2 3
        Assert.Equal(AiStepResult.Rolled, game.PlayAiStep()); // hold 6 6: 6 6 6 6 6
        Assert.Equal(new[] { 6, 6, 6, 6, 6 }, game.Dice);
        Assert.Equal(AiStepResult.Rolled, game.PlayAiStep()); // third roll
        Assert.Equal(AiStepResult.Scored, game.PlayAiStep()); // no rolls left: must score

        Assert.Equal(2, strategy.ChooseHoldsCalls);
        Assert.Single(game.Players[0].ScoreCard.Scores);
    }

    [Fact]
    public void PlayAiStep_StrategyStopsEarly_ScoresImmediately()
    {
        var strategy = new ScriptedStrategy { HoldsToReturn = null, CategoryToReturn = Category.Yahtzee };
        var game = new GameEngine([new Player("Bot", strategy)], Fives(), new ManualTimeProvider(Start));

        game.PlayAiStep();
        var result = game.PlayAiStep();

        Assert.Equal(AiStepResult.Scored, result);
        Assert.Equal(50, game.Players[0].ScoreCard.GetScore(Category.Yahtzee));
    }

    [Fact]
    public void PlayAiStep_HumanPlayer_Throws()
    {
        var game = NewGame(Fives(), "Alice");

        Assert.Throws<InvalidOperationException>(() => game.PlayAiStep());
    }

    [Fact]
    public void PlayAiStep_StrategyChoosesFilledCategory_Throws()
    {
        var strategy = new ScriptedStrategy { CategoryToReturn = Category.Chance };
        var game = new GameEngine([new Player("Bot", strategy)], Fives(), new ManualTimeProvider(Start));
        game.PlayAiStep();
        game.PlayAiStep(); // scores Chance
        game.PlayAiStep(); // rolls

        Assert.Throws<InvalidOperationException>(() => game.PlayAiStep());
    }

    [Fact]
    public void MixedHumanAndAiGame_PlaysToCompletion()
    {
        var game = new GameEngine(
            [new Player("Alice"), new Player("Bot", new ScriptedStrategy())],
            Fives(),
            new ManualTimeProvider(Start));

        while (!game.IsGameOver)
        {
            if (game.CurrentPlayer.IsAi)
            {
                while (game.PlayAiStep() != AiStepResult.Scored) { }
            }
            else
            {
                game.Roll();
                game.Score(game.CurrentPlayer.ScoreCard.AvailableCategories.Last());
            }
        }

        Assert.All(game.Players, player => Assert.True(player.ScoreCard.IsComplete));
    }
}
