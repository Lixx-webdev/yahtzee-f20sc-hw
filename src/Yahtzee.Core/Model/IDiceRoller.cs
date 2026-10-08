using Yahtzee.Core.Rules;

namespace Yahtzee.Core.Model;

/// <summary>
/// Source of die rolls. Abstracted so that tests and AI simulations can use
/// deterministic values instead of real randomness.
/// </summary>
public interface IDiceRoller
{
    /// <summary>Returns a value between 1 and 6 inclusive.</summary>
    int RollDie();
}

/// <summary>An <see cref="IDiceRoller"/> backed by <see cref="Random"/>.</summary>
/// <param name="random">The random source; defaults to <see cref="Random.Shared"/>.</param>
public sealed class RandomDiceRoller(Random? random = null) : IDiceRoller
{
    private readonly Random _random = random ?? Random.Shared;

    /// <inheritdoc/>
    public int RollDie() => _random.Next(Scorer.MinFace, Scorer.MaxFace + 1);
}
