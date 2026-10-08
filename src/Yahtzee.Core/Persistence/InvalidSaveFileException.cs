namespace Yahtzee.Core.Persistence;

/// <summary>
/// Thrown when a save file cannot be read or describes a game that cannot exist
/// (for example, impossible dice or scorecards that disagree with the turn order).
/// </summary>
public sealed class InvalidSaveFileException : Exception
{
    /// <summary>Creates the exception with a message explaining what is wrong with the file.</summary>
    public InvalidSaveFileException(string message) : base(message)
    {
    }

    /// <summary>Creates the exception with a message and the error that caused it.</summary>
    public InvalidSaveFileException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
