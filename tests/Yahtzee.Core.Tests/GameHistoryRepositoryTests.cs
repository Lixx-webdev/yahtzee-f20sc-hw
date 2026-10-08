using Microsoft.Data.Sqlite;
using Yahtzee.Core.Data;
using Yahtzee.Core.Game;

namespace Yahtzee.Core.Tests;

/// <summary>Runs each test against a fresh database file in the temp folder, deleted afterwards.</summary>
public sealed class GameHistoryRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset Day1 = new(2026, 10, 1, 18, 0, 0, TimeSpan.Zero);

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"yahtzee-test-{Guid.NewGuid():N}.db");
    private readonly SqliteGameHistoryRepository _repository;

    public GameHistoryRepositoryTests() => _repository = new SqliteGameHistoryRepository(_path);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools(); // pooled connections keep the file open
        File.Delete(_path);
    }

    private static CompletedGame Game(DateTimeOffset playedAt, params (string Name, int Score, bool IsAi)[] results) =>
        new(playedAt, [.. results.Select(r => new PlayerStanding(0, r.Name, r.Score, r.IsAi))]);

    [Fact]
    public async Task EmptyDatabase_HasNoScores()
    {
        Assert.Empty(await _repository.GetTopScoresAsync(5));
        Assert.Empty(await _repository.GetPlayerNamesAsync());
        Assert.True(File.Exists(_path));
    }

    [Fact]
    public async Task TopScores_AreBestFiveAcrossAllGames()
    {
        await _repository.RecordGameAsync(Game(Day1, ("Alice", 180, false), ("Bob", 210, false)));
        await _repository.RecordGameAsync(Game(Day1.AddDays(1), ("Alice", 250, false), ("Bot", 190, true)));
        await _repository.RecordGameAsync(Game(Day1.AddDays(2), ("Carol", 120, false), ("Bob", 230, false)));

        var top = await _repository.GetTopScoresAsync(5);

        Assert.Equal(new[] { 250, 230, 210, 190, 180 }, top.Select(score => score.Score));
        Assert.Equal("Alice", top[0].PlayerName);
        Assert.Equal(Day1.AddDays(1), top[0].PlayedAt);
        Assert.True(top[3].IsAi);
    }

    [Fact]
    public async Task TopScoresForPlayer_FiltersByNameIgnoringCase()
    {
        await _repository.RecordGameAsync(Game(Day1, ("Alice", 180, false), ("Bob", 210, false)));
        await _repository.RecordGameAsync(Game(Day1.AddDays(1), ("alice", 250, false)));

        var alice = await _repository.GetTopScoresForPlayerAsync("ALICE", 5);

        Assert.Equal(new[] { 250, 180 }, alice.Select(score => score.Score));
    }

    [Fact]
    public async Task TopScores_RespectsCountAndBreaksTiesByDate()
    {
        for (int day = 0; day < 7; day++)
            await _repository.RecordGameAsync(Game(Day1.AddDays(day), ("Alice", 200, false)));

        var top = await _repository.GetTopScoresAsync(5);

        Assert.Equal(5, top.Count);
        Assert.Equal(Day1, top[0].PlayedAt); // the earliest of the tied scores comes first
    }

    [Fact]
    public async Task PlayerNames_AreDistinctAndSorted()
    {
        await _repository.RecordGameAsync(Game(Day1, ("Carol", 1, false), ("Alice", 2, false)));
        await _repository.RecordGameAsync(Game(Day1, ("Carol", 3, false), ("Bob", 4, false)));

        Assert.Equal(new[] { "Alice", "Bob", "Carol" }, await _repository.GetPlayerNamesAsync());
    }

    [Fact]
    public async Task PlayerNamesWithQuotes_AreStoredSafely()
    {
        const string trickyName = "O'Brien\"; DROP TABLE Games; --";
        await _repository.RecordGameAsync(Game(Day1, (trickyName, 100, false)));

        var scores = await _repository.GetTopScoresForPlayerAsync(trickyName, 5);

        Assert.Equal(trickyName, Assert.Single(scores).PlayerName);
        Assert.Single(await _repository.GetTopScoresAsync(5)); // the Games table is still there
    }

    [Fact]
    public async Task DataSurvivesANewRepositoryInstance()
    {
        await _repository.RecordGameAsync(Game(Day1, ("Alice", 123, false)));

        var reopened = new SqliteGameHistoryRepository(_path);

        Assert.Equal(123, Assert.Single(await reopened.GetTopScoresAsync(5)).Score);
    }
}
