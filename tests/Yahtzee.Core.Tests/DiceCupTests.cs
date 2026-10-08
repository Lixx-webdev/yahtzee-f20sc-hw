using Yahtzee.Core.Model;

namespace Yahtzee.Core.Tests;

public class DiceCupTests
{
    [Fact]
    public void FirstRoll_RollsAllFiveDice()
    {
        var cup = new DiceCup(new FixedDiceRoller(1, 2, 3, 4, 5));

        cup.Roll();

        Assert.True(cup.HasRolled);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, cup.Values);
    }

    [Fact]
    public void Reroll_KeepsHeldDice()
    {
        var cup = new DiceCup(new FixedDiceRoller(1, 2, 3, 4, 5, 6, 6, 6));
        cup.Roll();
        cup.SetHeld(1, true);
        cup.SetHeld(3, true);

        cup.Roll();

        Assert.Equal(new[] { 6, 2, 6, 4, 6 }, cup.Values);
    }

    [Fact]
    public void ToggleHeld_ReleasesPreviouslyHeldDie()
    {
        var cup = new DiceCup(new FixedDiceRoller(1, 2, 3, 4, 5));
        cup.Roll();

        cup.ToggleHeld(2);
        cup.ToggleHeld(2);

        Assert.False(cup.Held[2]);
    }

    [Fact]
    public void Hold_BeforeFirstRoll_Throws()
    {
        var cup = new DiceCup(new FixedDiceRoller());

        Assert.Throws<InvalidOperationException>(() => cup.SetHeld(0, true));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void Hold_InvalidIndex_Throws(int index)
    {
        var cup = new DiceCup(new FixedDiceRoller(1, 2, 3, 4, 5));
        cup.Roll();

        Assert.Throws<ArgumentOutOfRangeException>(() => cup.SetHeld(index, true));
    }

    [Fact]
    public void Reset_ClearsHoldsAndRolledState()
    {
        var cup = new DiceCup(new FixedDiceRoller(1, 2, 3, 4, 5, 6, 6, 6, 6, 6));
        cup.Roll();
        cup.SetHeld(0, true);

        cup.Reset();
        cup.Roll();

        Assert.All(cup.Held, held => Assert.False(held));
        Assert.Equal(new[] { 6, 6, 6, 6, 6 }, cup.Values);
    }

    [Fact]
    public void RestoreConstructor_KeepsValuesAndHolds()
    {
        var cup = new DiceCup(new FixedDiceRoller(), [3, 3, 3, 1, 2], [true, true, true, false, false]);

        Assert.True(cup.HasRolled);
        Assert.Equal(new[] { 3, 3, 3, 1, 2 }, cup.Values);
        Assert.Equal(new[] { true, true, true, false, false }, cup.Held);
    }

    [Fact]
    public void RestoreConstructor_RejectsInvalidValues() =>
        Assert.Throws<ArgumentException>(() =>
            new DiceCup(new FixedDiceRoller(), [0, 3, 3, 1, 2], [false, false, false, false, false]));
}
