using Yahtzee.Core.Rules;

namespace Yahtzee.Core.Model;

/// <summary>
/// The five dice in play and which of them are held (set aside) for the next roll.
/// </summary>
/// <remarks>
/// This class only tracks dice; the limit of three rolls per turn is enforced by the game engine.
/// </remarks>
public sealed class DiceCup
{
    private readonly int[] _values = new int[Scorer.DiceCount];
    private readonly bool[] _held = new bool[Scorer.DiceCount];
    private readonly IDiceRoller _roller;

    /// <summary>Creates a cup of unrolled dice.</summary>
    /// <param name="roller">Source of die values.</param>
    public DiceCup(IDiceRoller roller)
    {
        _roller = roller ?? throw new ArgumentNullException(nameof(roller));
    }

    /// <summary>
    /// Creates a cup restored to a previous state, for example when loading a saved game.
    /// </summary>
    /// <param name="roller">Source of die values.</param>
    /// <param name="values">Five die values between 1 and 6.</param>
    /// <param name="held">Five flags saying which dice are held.</param>
    /// <exception cref="ArgumentException">The values or flags are invalid.</exception>
    public DiceCup(IDiceRoller roller, IReadOnlyList<int> values, IReadOnlyList<bool> held) : this(roller)
    {
        Scorer.ValidateDice(values);
        ArgumentNullException.ThrowIfNull(held);
        if (held.Count != Scorer.DiceCount)
            throw new ArgumentException($"Expected {Scorer.DiceCount} held flags but got {held.Count}.", nameof(held));

        for (int i = 0; i < Scorer.DiceCount; i++)
        {
            _values[i] = values[i];
            _held[i] = held[i];
        }
        HasRolled = true;
    }

    /// <summary>Whether the dice have been rolled since the cup was created or reset.</summary>
    public bool HasRolled { get; private set; }

    /// <summary>The current die values. Only meaningful once <see cref="HasRolled"/> is true.</summary>
    public IReadOnlyList<int> Values => _values;

    /// <summary>Which dice are held and will not change on the next roll.</summary>
    public IReadOnlyList<bool> Held => _held;

    /// <summary>Rolls every die that is not held. The first roll always rolls all five.</summary>
    public void Roll()
    {
        for (int i = 0; i < Scorer.DiceCount; i++)
        {
            if (!HasRolled || !_held[i])
                _values[i] = _roller.RollDie();
        }
        HasRolled = true;
    }

    /// <summary>Holds or releases the die at <paramref name="index"/>.</summary>
    /// <exception cref="InvalidOperationException">The dice have not been rolled yet.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not 0–4.</exception>
    public void SetHeld(int index, bool held)
    {
        if (!HasRolled)
            throw new InvalidOperationException("Dice cannot be held before the first roll.");
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Scorer.DiceCount);
        _held[index] = held;
    }

    /// <summary>Flips the held state of the die at <paramref name="index"/>.</summary>
    public void ToggleHeld(int index) => SetHeld(index, !_held[index]);

    /// <summary>Releases all dice and marks the cup as unrolled, ready for the next turn.</summary>
    public void Reset()
    {
        Array.Clear(_values);
        Array.Clear(_held);
        HasRolled = false;
    }
}
