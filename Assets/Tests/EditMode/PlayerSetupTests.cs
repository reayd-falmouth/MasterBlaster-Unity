using System.Linq;
using NUnit.Framework;
using Scenes.Arena;

public class PlayerSetupTests
{
    [Test]
    public void GetPlayerSetup_Count2_ReturnsTopLeftAndBottomRight()
    {
        var setup = ArenaLogic.GetPlayerSetup(2);
        Assert.That(setup.Count, Is.EqualTo(2));
        Assert.That(setup[0].slot, Is.EqualTo(PlayerSlot.TopLeft));
        Assert.That(setup[0].playerId, Is.EqualTo(1));
        Assert.That(setup[1].slot, Is.EqualTo(PlayerSlot.BottomRight));
        Assert.That(setup[1].playerId, Is.EqualTo(2));
    }

    [Test]
    public void GetPlayerSetup_Count3_ReturnsTopLeftBottomRightMiddle()
    {
        var setup = ArenaLogic.GetPlayerSetup(3);
        Assert.That(setup.Count, Is.EqualTo(3));
        Assert.That(setup[0], Is.EqualTo((PlayerSlot.TopLeft, 1)));
        Assert.That(setup[1], Is.EqualTo((PlayerSlot.BottomRight, 2)));
        Assert.That(setup[2], Is.EqualTo((PlayerSlot.Middle, 3)));
    }

    [Test]
    public void GetPlayerSetup_Count4_ReturnsAllFourCorners()
    {
        var setup = ArenaLogic.GetPlayerSetup(4);
        Assert.That(setup.Count, Is.EqualTo(4));
        Assert.That(
            setup.Select(s => s.slot).ToArray(),
            Is.EquivalentTo(
                new[]
                {
                    PlayerSlot.TopLeft,
                    PlayerSlot.TopRight,
                    PlayerSlot.BottomLeft,
                    PlayerSlot.BottomRight
                }
            )
        );
        Assert.That(setup.Select(s => s.playerId).ToArray(), Is.EquivalentTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void GetPlayerSetup_Count5_ReturnsAllFiveSlots()
    {
        var setup = ArenaLogic.GetPlayerSetup(5);
        Assert.That(setup.Count, Is.EqualTo(5));
        Assert.That(
            setup.Select(s => s.slot).ToArray(),
            Is.EquivalentTo(
                new[]
                {
                    PlayerSlot.TopLeft,
                    PlayerSlot.TopRight,
                    PlayerSlot.BottomLeft,
                    PlayerSlot.BottomRight,
                    PlayerSlot.Middle
                }
            )
        );
        Assert.That(
            setup.Select(s => s.playerId).ToArray(),
            Is.EquivalentTo(new[] { 1, 2, 3, 4, 5 })
        );
    }

    [Test]
    public void GetPlayerSetup_Count1_ReturnsEmpty()
    {
        var setup = ArenaLogic.GetPlayerSetup(1);
        Assert.That(setup, Is.Empty);
    }

    [Test]
    public void GetPlayerSetup_Count6_ReturnsEmpty()
    {
        var setup = ArenaLogic.GetPlayerSetup(6);
        Assert.That(setup, Is.Empty);
    }
}
