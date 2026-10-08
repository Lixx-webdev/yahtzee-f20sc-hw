using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yahtzee.Core.Persistence;

/// <summary>
/// Reads and writes <see cref="GameSnapshot"/>s as indented, human-readable JSON (<c>.yahtzee</c> files).
/// </summary>
/// <remarks>
/// This class only checks that the file is well-formed JSON of the right shape and version.
/// Whether the described game is actually possible is checked by <c>GameEngine.Restore</c>.
/// </remarks>
public static class SaveGameSerializer
{
    /// <summary>File extension used for save files, including the dot.</summary>
    public const string FileExtension = ".yahtzee";

    /// <summary>The format version written by this program. Increase it whenever the format changes.</summary>
    public const int CurrentFormatVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // A missing property or a null where the type does not allow one is an error, not a silent default.
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Converts a snapshot to JSON text.</summary>
    public static string Serialize(GameSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.Serialize(snapshot, Options);
    }

    /// <summary>Parses JSON text produced by <see cref="Serialize"/>.</summary>
    /// <exception cref="InvalidSaveFileException">The text is not a valid save of a supported version.</exception>
    public static GameSnapshot Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        GameSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<GameSnapshot>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidSaveFileException($"The file is not a valid Yahtzee save: {ex.Message}", ex);
        }

        if (snapshot is null)
            throw new InvalidSaveFileException("The file is empty.");
        if (snapshot.FormatVersion != CurrentFormatVersion)
            throw new InvalidSaveFileException(
                $"The file uses save format version {snapshot.FormatVersion}; this program reads version {CurrentFormatVersion}.");
        return snapshot;
    }

    /// <summary>Writes a snapshot to <paramref name="path"/>, replacing any existing file.</summary>
    /// <remarks>
    /// The JSON is written to a temporary file first and then moved over the target, so a crash
    /// part-way through never leaves a half-written save behind.
    /// </remarks>
    public static async Task SaveAsync(GameSnapshot snapshot, string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string json = Serialize(snapshot);
        string tempPath = path + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        File.Move(tempPath, path, overwrite: true);
    }

    /// <summary>Reads a snapshot from <paramref name="path"/>.</summary>
    /// <exception cref="InvalidSaveFileException">The file is not a valid save of a supported version.</exception>
    /// <exception cref="IOException">The file could not be read.</exception>
    public static async Task<GameSnapshot> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string json = await File.ReadAllTextAsync(path, cancellationToken);
        return Deserialize(json);
    }
}
