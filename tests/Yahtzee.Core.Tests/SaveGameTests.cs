using Yahtzee.Core.AI;
using Yahtzee.Core.Game;
using Yahtzee.Core.Model;
using Yahtzee.Core.Persistence;

namespace Yahtzee.Core.Tests;

public class SaveGameTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 8, 9, 30, 0, TimeSpan.Zero);

    private static FixedDiceRoller Fives() => new([.. Enumerable.Repeat(5, 1000)]);

    private static IPlayerStrategy CreateScripted(string name) =>
        name == "Scripted" ? new ScriptedStrategy() : throw new ArgumentException($"Unknown strategy {name}");

    private static GameEngine Restore(GameSnapshot snapshot) =>
        GameEngine.Restore(snapshot, Fives(), CreateScripted, new ManualTimeProvider(Start.AddHours(1)));

    /// <summary>Alice and Bob have each played one turn; Alice has rolled once and held two dice.</summary>
    private static GameEngine MidTurnGame()
    {
        var roller = new FixedDiceRoller([.. Enumerable.Repeat(5, 10), 3, 3, 6, 2, 1]);
        var game = new GameEngine([new Player("Alice"), new Player("Bob", new ScriptedStrategy())], roller, new ManualTimeProvider(Start));
        game.Roll();
        game.Score(Category.Fives);    // Alice 25
        game.Roll();
        game.Score(Category.Yahtzee);  // Bob 50
        game.Roll();                   // Alice rolls 3 3 6 2 1
        game.SetHeld(0, true);
        game.SetHeld(1, true);
        return game;
    }

    // ---- Snapshots and restoring ----

    [Fact]
    public void Snapshot_CapturesMidTurnState()
    {
        var snapshot = MidTurnGame().CreateSnapshot();

        Assert.Equal(SaveGameSerializer.CurrentFormatVersion, snapshot.FormatVersion);
        Assert.Equal(Start, snapshot.StartedAt);
        Assert.Equal(0, snapshot.CurrentPlayerIndex);
        Assert.Equal(1, snapshot.RollsUsed);
        Assert.Equal(new[] { 3, 3, 6, 2, 1 }, snapshot.Dice);
        Assert.Equal(new[] { true, true, false, false, false }, snapshot.Held);
        Assert.Null(snapshot.Players[0].Strategy);
        Assert.Equal("Scripted", snapshot.Players[1].Strategy);
        Assert.Equal(25, snapshot.Players[0].Scores[Category.Fives]);
        Assert.Null(snapshot.Players[0].Scores[Category.Aces]);
        Assert.Equal(13, snapshot.Players[1].Scores.Count);
    }

    [Fact]
    public void Snapshot_BeforeFirstRoll_HasBlankDice()
    {
        var game = new GameEngine([new Player("Alice")], Fives(), new ManualTimeProvider(Start));

        var snapshot = game.CreateSnapshot();

        Assert.Equal(0, snapshot.RollsUsed);
        Assert.Equal(new[] { 0, 0, 0, 0, 0 }, snapshot.Dice);
    }

    [Fact]
    public void Restore_ResumesExactlyWhereSaved()
    {
        var original = MidTurnGame();

        var restored = Restore(original.CreateSnapshot());

        Assert.Equal("Alice", restored.CurrentPlayer.Name);
        Assert.Equal(2, restored.Round);
        Assert.Equal(1, restored.RollsUsed);
        Assert.Equal(original.Dice, restored.Dice);
        Assert.Equal(original.Held, restored.Held);
        Assert.Equal(Start, restored.StartedAt);
        Assert.True(restored.Players[1].IsAi);
        Assert.Equal(50, restored.Players[1].ScoreCard.GetScore(Category.Yahtzee));
        Assert.True(restored.CanHold);
    }

    [Fact]
    public void Restore_GameCanBePlayedToTheEnd()
    {
        var restored = Restore(MidTurnGame().CreateSnapshot());
        int gameOverCount = 0;
        restored.GameOver += (_, _) => gameOverCount++;

        while (!restored.IsGameOver)
        {
            if (restored.CurrentPlayer.IsAi)
            {
                while (restored.PlayAiStep() != AiStepResult.Scored) { }
            }
            else
            {
                if (restored.RollsUsed == 0)
                    restored.Roll();
                restored.Score(restored.CurrentPlayer.ScoreCard.AvailableCategories.First());
            }
        }

        Assert.Equal(1, gameOverCount);
        Assert.All(restored.Players, player => Assert.True(player.ScoreCard.IsComplete));
    }

    [Fact]
    public void CreateSnapshot_AfterGameOver_Throws()
    {
        var game = new GameEngine([new Player("Alice")], Fives(), new ManualTimeProvider(Start));
        foreach (var category in CategoryExtensions.All)
        {
            game.Roll();
            game.Score(category);
        }

        Assert.False(game.CanSave);
        Assert.Throws<InvalidOperationException>(game.CreateSnapshot);
    }

    // ---- JSON format ----

    [Fact]
    public void Json_RoundTripsAndIsReadable()
    {
        var snapshot = MidTurnGame().CreateSnapshot();

        string json = SaveGameSerializer.Serialize(snapshot);
        var reloaded = SaveGameSerializer.Deserialize(json);

        Assert.Contains("\"formatVersion\": 1", json);
        Assert.Contains("\"Fives\": 25", json);
        Assert.Contains("\"strategy\": null", json);
        Assert.Equal(snapshot.Dice, reloaded.Dice);
        Assert.Equal(snapshot.Held, reloaded.Held);
        Assert.Equal(snapshot.Players[1].Scores[Category.Yahtzee], reloaded.Players[1].Scores[Category.Yahtzee]);
        Assert.Equal(snapshot.StartedAt, reloaded.StartedAt);
    }

    [Fact]
    public async Task SaveAsync_And_LoadAsync_UseTheFileSystem()
    {
        string path = Path.Combine(Path.GetTempPath(), $"yahtzee-test-{Guid.NewGuid():N}{SaveGameSerializer.FileExtension}");
        try
        {
            var snapshot = MidTurnGame().CreateSnapshot();

            await SaveGameSerializer.SaveAsync(snapshot, path);
            var loaded = await SaveGameSerializer.LoadAsync(path);

            Assert.Equal(snapshot.RollsUsed, loaded.RollsUsed);
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("""{ "formatVersion": 1 }""")]
    public void Deserialize_RejectsMalformedFiles(string json) =>
        Assert.Throws<InvalidSaveFileException>(() => SaveGameSerializer.Deserialize(json));

    [Fact]
    public void Deserialize_RejectsOtherFormatVersions()
    {
        string json = SaveGameSerializer.Serialize(MidTurnGame().CreateSnapshot()).Replace("\"formatVersion\": 1", "\"formatVersion\": 99");

        var ex = Assert.Throws<InvalidSaveFileException>(() => SaveGameSerializer.Deserialize(json));
        Assert.Contains("version 99", ex.Message);
    }

    [Fact]
    public void Deserialize_RejectsNullPlayerName()
    {
        string json = SaveGameSerializer.Serialize(MidTurnGame().CreateSnapshot()).Replace("\"name\": \"Alice\"", "\"name\": null");

        Assert.Throws<InvalidSaveFileException>(() => SaveGameSerializer.Deserialize(json));
    }

    // ---- Impossible games are rejected ----

    [Fact]
    public void Restore_RejectsScorecardsThatDisagreeWithTurnOrder()
    {
        var snapshot = MidTurnGame().CreateSnapshot();
        var bobScores = new Dictionary<Category, int?>(snapshot.Players[1].Scores) { [Category.Chance] = 20 }; // Bob has played twice
        var tampered = snapshot with { Players = [snapshot.Players[0], snapshot.Players[1] with { Scores = bobScores }] };

        var ex = Assert.Throws<InvalidSaveFileException>(() => Restore(tampered));
        Assert.Contains("Bob", ex.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Restore_RejectsInvalidRollCount(int rollsUsed) =>
        Assert.Throws<InvalidSaveFileException>(() => Restore(MidTurnGame().CreateSnapshot() with { RollsUsed = rollsUsed }));

    [Fact]
    public void Restore_RejectsInvalidDice() =>
        Assert.Throws<InvalidSaveFileException>(() => Restore(MidTurnGame().CreateSnapshot() with { Dice = [3, 3, 9, 2, 1] }));

    [Fact]
    public void Restore_RejectsMissingCurrentPlayer() =>
        Assert.Throws<InvalidSaveFileException>(() => Restore(MidTurnGame().CreateSnapshot() with { CurrentPlayerIndex = 2 }));

    [Fact]
    public void Restore_RejectsUnknownStrategy()
    {
        var snapshot = MidTurnGame().CreateSnapshot();
        var tampered = snapshot with { Players = [snapshot.Players[0], snapshot.Players[1] with { Strategy = "Cheater" }] };

        var ex = Assert.Throws<InvalidSaveFileException>(() => Restore(tampered));
        Assert.Contains("Cheater", ex.Message);
    }

    [Fact]
    public void Restore_RejectsDuplicateNames()
    {
        var snapshot = MidTurnGame().CreateSnapshot();
        var tampered = snapshot with { Players = [snapshot.Players[0], snapshot.Players[1] with { Name = "ALICE" }] };

        Assert.Throws<InvalidSaveFileException>(() => Restore(tampered));
    }
}
