using Yahtzee.Core.Model;

namespace Yahtzee.Core.Persistence;

/// <summary>
/// Everything needed to resume a game exactly where it was saved. This is the root object of the
/// <c>.yahtzee</c> save file; <see cref="SaveGameSerializer"/> converts it to and from JSON.
/// </summary>
/// <remarks>
/// The round number is not stored: it is derived from the scorecards when the game is restored.
/// </remarks>
/// <param name="FormatVersion">Save-file format version; see <see cref="SaveGameSerializer.CurrentFormatVersion"/>.</param>
/// <param name="StartedAt">When the game was originally started.</param>
/// <param name="Players">The players in turn order.</param>
/// <param name="CurrentPlayerIndex">Index into <paramref name="Players"/> of the player whose turn it is.</param>
/// <param name="RollsUsed">Rolls already made in the current turn, 0–3.</param>
/// <param name="Dice">The five die values; all 0 when <paramref name="RollsUsed"/> is 0.</param>
/// <param name="Held">Which of the five dice are held.</param>
public sealed record GameSnapshot(
    int FormatVersion,
    DateTimeOffset StartedAt,
    IReadOnlyList<PlayerSnapshot> Players,
    int CurrentPlayerIndex,
    int RollsUsed,
    IReadOnlyList<int> Dice,
    IReadOnlyList<bool> Held);

/// <summary>One player's saved state.</summary>
/// <param name="Name">The player's name.</param>
/// <param name="Strategy">The AI strategy name, or <see langword="null"/> for a human player.</param>
/// <param name="Scores">Recorded scores by category; a <see langword="null"/> or missing entry is an empty box.</param>
public sealed record PlayerSnapshot(string Name, string? Strategy, IReadOnlyDictionary<Category, int?> Scores);
