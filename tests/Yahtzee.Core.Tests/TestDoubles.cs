using Yahtzee.Core.AI;
using Yahtzee.Core.Model;

namespace Yahtzee.Core.Tests;

/// <summary>Clock that always returns a fixed time, which tests can move forward.</summary>
internal sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>
/// Strategy test double: holds whatever <see cref="HoldsToReturn"/> says and scores the first open category.
/// </summary>
internal sealed class ScriptedStrategy : IPlayerStrategy
{
    public string Name => "Scripted";

    /// <summary>Returned from every <see cref="ChooseHolds"/> call; null means "stop rolling".</summary>
    public IReadOnlyList<bool>? HoldsToReturn { get; set; }

    /// <summary>If set, returned from <see cref="ChooseCategory"/> instead of the first open category.</summary>
    public Category? CategoryToReturn { get; set; }

    public int ChooseHoldsCalls { get; private set; }

    public IReadOnlyList<bool>? ChooseHolds(IReadOnlyList<int> dice, int rollsRemaining, ScoreCard scoreCard)
    {
        ChooseHoldsCalls++;
        return HoldsToReturn;
    }

    public Category ChooseCategory(IReadOnlyList<int> dice, ScoreCard scoreCard) =>
        CategoryToReturn ?? scoreCard.AvailableCategories.First();
}
