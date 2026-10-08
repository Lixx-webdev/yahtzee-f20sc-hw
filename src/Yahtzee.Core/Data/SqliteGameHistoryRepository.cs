using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Yahtzee.Core.Data;

/// <summary>
/// Keeps the games history in a SQLite database file. The tables are created automatically the first
/// time the database is used.
/// </summary>
/// <remarks>
/// Schema:
/// <code>
/// Games(Id INTEGER PRIMARY KEY, PlayedAt TEXT NOT NULL)            -- ISO-8601 UTC timestamp
/// GameResults(Id INTEGER PRIMARY KEY, GameId INTEGER NOT NULL REFERENCES Games(Id),
///             PlayerName TEXT NOT NULL COLLATE NOCASE, Score INTEGER NOT NULL, IsAi INTEGER NOT NULL)
/// </code>
/// All queries use parameters, never string concatenation, so player names cannot inject SQL.
/// </remarks>
public sealed class SqliteGameHistoryRepository : IGameHistoryRepository
{
    private const string CreateSchemaSql = """
        CREATE TABLE IF NOT EXISTS Games (
            Id       INTEGER PRIMARY KEY AUTOINCREMENT,
            PlayedAt TEXT    NOT NULL
        );
        CREATE TABLE IF NOT EXISTS GameResults (
            Id         INTEGER PRIMARY KEY AUTOINCREMENT,
            GameId     INTEGER NOT NULL REFERENCES Games(Id) ON DELETE CASCADE,
            PlayerName TEXT    NOT NULL COLLATE NOCASE,
            Score      INTEGER NOT NULL,
            IsAi       INTEGER NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_GameResults_Score ON GameResults (Score DESC);
        CREATE INDEX IF NOT EXISTS IX_GameResults_Player ON GameResults (PlayerName, Score DESC);
        """;

    /// <summary>Columns shared by both high-score queries; ties go to whoever scored first.</summary>
    private const string HighScoreSelect = """
        SELECT r.PlayerName, r.Score, r.IsAi, g.PlayedAt
        FROM GameResults r JOIN Games g ON g.Id = r.GameId
        """;

    private readonly string _connectionString;
    private bool _schemaCreated;

    /// <summary>Uses (and if necessary creates) the database file at <paramref name="databasePath"/>.</summary>
    public SqliteGameHistoryRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        string? folder = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (folder is not null)
            Directory.CreateDirectory(folder);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true,
        }.ToString();
    }

    /// <summary>The default database location: <c>%LOCALAPPDATA%\Yahtzee\yahtzee.db</c>.</summary>
    public static string DefaultDatabasePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yahtzee", "yahtzee.db");

    /// <inheritdoc/>
    public async Task RecordGameAsync(CompletedGame game, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(game);
        await using var connection = await OpenAsync(cancellationToken);
        // One transaction: either the game and all of its results are stored, or nothing is.
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        using var insertGame = connection.CreateCommand();
        insertGame.Transaction = transaction;
        insertGame.CommandText = "INSERT INTO Games (PlayedAt) VALUES ($playedAt); SELECT last_insert_rowid();";
        insertGame.Parameters.AddWithValue("$playedAt", game.PlayedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        long gameId = (long)(await insertGame.ExecuteScalarAsync(cancellationToken))!;

        using var insertResult = connection.CreateCommand();
        insertResult.Transaction = transaction;
        insertResult.CommandText = """
            INSERT INTO GameResults (GameId, PlayerName, Score, IsAi)
            VALUES ($gameId, $name, $score, $isAi)
            """;
        var nameParameter = insertResult.Parameters.Add("$name", SqliteType.Text);
        var scoreParameter = insertResult.Parameters.Add("$score", SqliteType.Integer);
        var isAiParameter = insertResult.Parameters.Add("$isAi", SqliteType.Integer);
        insertResult.Parameters.AddWithValue("$gameId", gameId);

        foreach (var result in game.Results)
        {
            nameParameter.Value = result.Name;
            scoreParameter.Value = result.Score;
            isAiParameter.Value = result.IsAi ? 1 : 0;
            await insertResult.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HighScore>> GetTopScoresAsync(int count, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        await using var connection = await OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = $"{HighScoreSelect} ORDER BY r.Score DESC, g.PlayedAt ASC LIMIT $count";
        command.Parameters.AddWithValue("$count", count);
        return await ReadHighScoresAsync(command, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HighScore>> GetTopScoresForPlayerAsync(string playerName, int count,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(playerName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        await using var connection = await OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        // PlayerName is declared COLLATE NOCASE, so "alice" also finds "Alice".
        command.CommandText = $"{HighScoreSelect} WHERE r.PlayerName = $name ORDER BY r.Score DESC, g.PlayedAt ASC LIMIT $count";
        command.Parameters.AddWithValue("$name", playerName.Trim());
        command.Parameters.AddWithValue("$count", count);
        return await ReadHighScoresAsync(command, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetPlayerNamesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT PlayerName FROM GameResults ORDER BY PlayerName";
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            names.Add(reader.GetString(0));
        return names;
    }

    /// <summary>Opens a connection, creating the tables the first time.</summary>
    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        if (!_schemaCreated)
        {
            using var command = connection.CreateCommand();
            command.CommandText = CreateSchemaSql;
            await command.ExecuteNonQueryAsync(cancellationToken);
            _schemaCreated = true;
        }
        return connection;
    }

    private static async Task<IReadOnlyList<HighScore>> ReadHighScoresAsync(SqliteCommand command, CancellationToken cancellationToken)
    {
        var scores = new List<HighScore>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            scores.Add(new HighScore(
                PlayerName: reader.GetString(0),
                Score: reader.GetInt32(1),
                IsAi: reader.GetInt64(2) != 0,
                PlayedAt: DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)));
        }
        return scores;
    }
}
