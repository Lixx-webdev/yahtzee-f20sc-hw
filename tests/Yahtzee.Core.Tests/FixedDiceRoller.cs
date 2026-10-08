using Yahtzee.Core.Model;

namespace Yahtzee.Core.Tests;

/// <summary>Test double that returns a predetermined sequence of die values.</summary>
internal sealed class FixedDiceRoller(params int[] values) : IDiceRoller
{
    private readonly Queue<int> _values = new(values);

    public int RollDie() => _values.Count > 0
        ? _values.Dequeue()
        : throw new InvalidOperationException("FixedDiceRoller ran out of values.");
}
