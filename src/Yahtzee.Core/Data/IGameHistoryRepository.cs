using Yahtzee.Core.Game;

namespace Yahtzee.Core.Data;

/// <summary>
/// Storage for completed games and high-score queries. The UI depends on this interface, not on
/// SQLite, so the storage engine could be replaced without touching the forms.
/// </summary>
public interface IGameHistoryRepository
{
    /// <summary>Records a finished game and every player's final score.</summary>
    Task RecordGameAsync(CompletedGame game, CancellationToken cancellationToken = default);

    /// <summary>The highest individual scores across all games, best first.</summary>
    /// <param name="count">How many scores to return.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    Task<IReadOnlyList<HighScore>> GetTopScoresAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>The highest scores of one player (name compared ignoring case), best first.</summary>
    /// <param name="playerName">The player's name.</param>
    /// <param name="count">How many scores to return.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    Task<IReadOnlyList<HighScore>> GetTopScoresForPlayerAsync(string playerName, int count, CancellationToken cancellationToken = default);

    /// <summary>Every player name that appears in the history, alphabetically.</summary>
    Task<IReadOnlyList<string>> GetPlayerNamesAsync(CancellationToken cancellationToken = default);
}

/// <summary>A finished game, as recorded in the history.</summary>
/// <param name="PlayedAt">When the game finished.</param>
/// <param name="Results">Each player's final standing.</param>
public sealed record CompletedGame(DateTimeOffset PlayedAt, IReadOnlyList<PlayerStanding> Results);

/// <summary>One row of a high-score table.</summary>
/// <param name="PlayerName">Who scored it.</param>
/// <param name="Score">The final score.</param>
/// <param name="IsAi">Whether the player was computer-controlled.</param>
/// <param name="PlayedAt">When the game finished.</param>
public sealed record HighScore(string PlayerName, int Score, bool IsAi, DateTimeOffset PlayedAt);
