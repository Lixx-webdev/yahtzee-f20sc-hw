namespace Yahtzee.Core.AI;

/// <summary>
/// The list of available computer strategies, looked up by the name stored in save files.
/// Adding a strategy means adding it here; the new-game dialog and save loading pick it up automatically.
/// </summary>
public static class StrategyCatalog
{
    /// <summary>Every strategy name, from weakest to strongest.</summary>
    public static IReadOnlyList<string> Names { get; } =
        [RandomStrategy.StrategyName, GreedyStrategy.StrategyName, ExpectedValueStrategy.StrategyName];

    /// <summary>Creates a new instance of the strategy called <paramref name="name"/>.</summary>
    /// <exception cref="ArgumentException">No strategy has that name.</exception>
    public static IPlayerStrategy Create(string name) => name switch
    {
        RandomStrategy.StrategyName => new RandomStrategy(),
        GreedyStrategy.StrategyName => new GreedyStrategy(),
        ExpectedValueStrategy.StrategyName => new ExpectedValueStrategy(),
        _ => throw new ArgumentException($"Unknown AI strategy \"{name}\".", nameof(name)),
    };

    /// <summary>A friendly description of the strategy for the user interface.</summary>
    public static string Describe(string name) => name switch
    {
        RandomStrategy.StrategyName => "Computer: easy (random)",
        GreedyStrategy.StrategyName => "Computer: medium (greedy)",
        ExpectedValueStrategy.StrategyName => "Computer: hard (expected value)",
        _ => name,
    };
}
