using Yahtzee.Core.AI;

namespace Yahtzee.WinForms.Forms;

/// <summary>
/// One choice in the "Type" drop-down of the new-game dialog: a human, or an AI with a given strategy.
/// </summary>
/// <param name="Label">Text shown in the drop-down.</param>
/// <param name="CreateStrategy">Creates the AI strategy, or returns <see langword="null"/> for a human.</param>
internal sealed record PlayerTypeOption(string Label, Func<IPlayerStrategy?> CreateStrategy)
{
    /// <summary>A human player.</summary>
    public static PlayerTypeOption Human { get; } = new("Human", () => null);

    /// <summary>All selectable player types: Human, then one entry per strategy in <see cref="StrategyCatalog"/>.</summary>
    public static IReadOnlyList<PlayerTypeOption> All { get; } =
    [
        Human,
        .. StrategyCatalog.Names.Select(name => new PlayerTypeOption(StrategyCatalog.Describe(name), () => StrategyCatalog.Create(name))),
    ];

    /// <summary>Whether this option is a computer player.</summary>
    public bool IsComputer => this != Human;

    public override string ToString() => Label;
}
