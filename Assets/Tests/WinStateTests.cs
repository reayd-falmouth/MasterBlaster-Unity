using NUnit.Framework;
using Scenes.Arena;

public class WinStateTests
{
    [Test]
    public void EvaluateWinState_MoreThanOneAlive_ReturnsNoChange()
    {
        var result = ArenaLogic.EvaluateWinState(new[] { true, true }, 0, 3);
        Assert.That(result.Outcome, Is.EqualTo(WinOutcome.NoChange));
        Assert.That(result.LastAliveIndex, Is.Null);
    }

    [Test]
    public void EvaluateWinState_OneAlive_UnderWinsNeeded_ReturnsGoToStandings()
    {
        var result = ArenaLogic.EvaluateWinState(new[] { true, false }, 1, 3);
        Assert.That(result.Outcome, Is.EqualTo(WinOutcome.GoToStandings));
        Assert.That(result.LastAliveIndex, Is.EqualTo(0));
    }

    [Test]
    public void EvaluateWinState_OneAlive_MeetsWinsNeeded_ReturnsGoToOvers()
    {
        var result = ArenaLogic.EvaluateWinState(new[] { false, true }, 2, 3);
        Assert.That(result.Outcome, Is.EqualTo(WinOutcome.GoToOvers));
        Assert.That(result.LastAliveIndex, Is.EqualTo(1));
    }

    [Test]
    public void EvaluateWinState_OneAlive_ExactlyWinsNeeded_ReturnsGoToOvers()
    {
        var result = ArenaLogic.EvaluateWinState(new[] { true, false, false }, 2, 3);
        Assert.That(result.Outcome, Is.EqualTo(WinOutcome.GoToOvers));
        Assert.That(result.LastAliveIndex, Is.EqualTo(0));
    }

    [Test]
    public void EvaluateWinState_AllDead_ReturnsGoToStandings()
    {
        var result = ArenaLogic.EvaluateWinState(new[] { false, false }, 0, 3);
        Assert.That(result.Outcome, Is.EqualTo(WinOutcome.GoToStandings));
        Assert.That(result.LastAliveIndex, Is.Null);
    }

    [Test]
    public void EvaluateWinState_NullOrEmpty_ReturnsNoChange()
    {
        Assert.That(
            ArenaLogic.EvaluateWinState(null, 0, 3).Outcome,
            Is.EqualTo(WinOutcome.NoChange)
        );
        Assert.That(
            ArenaLogic.EvaluateWinState(System.Array.Empty<bool>(), 0, 3).Outcome,
            Is.EqualTo(WinOutcome.NoChange)
        );
    }

    [Test]
    public void EvaluateWinState_LastAliveIndex_MatchesLastActiveSlot()
    {
        var result = ArenaLogic.EvaluateWinState(new[] { false, false, true }, 0, 3);
        Assert.That(result.LastAliveIndex, Is.EqualTo(2));
    }

    [Test]
    public void EvaluateWinState_OneAlive_OneWinNeeded_ReturnsGoToOvers()
    {
        var result = ArenaLogic.EvaluateWinState(new[] { false, true }, 0, 1);
        Assert.That(result.Outcome, Is.EqualTo(WinOutcome.GoToOvers));
        Assert.That(result.LastAliveIndex, Is.EqualTo(1));
    }

    [Test]
    public void WinStateResult_StaticFactories_ReturnCorrectOutcomes()
    {
        var noChange = WinStateResult.NoChange();
        Assert.That(noChange.Outcome, Is.EqualTo(WinOutcome.NoChange));
        Assert.That(noChange.LastAliveIndex, Is.Null);

        var standingsWithIndex = WinStateResult.GoToStandings(2);
        Assert.That(standingsWithIndex.Outcome, Is.EqualTo(WinOutcome.GoToStandings));
        Assert.That(standingsWithIndex.LastAliveIndex, Is.EqualTo(2));

        var standingsNoIndex = WinStateResult.GoToStandings();
        Assert.That(standingsNoIndex.Outcome, Is.EqualTo(WinOutcome.GoToStandings));
        Assert.That(standingsNoIndex.LastAliveIndex, Is.Null);

        var overs = WinStateResult.GoToOvers(1);
        Assert.That(overs.Outcome, Is.EqualTo(WinOutcome.GoToOvers));
        Assert.That(overs.LastAliveIndex, Is.EqualTo(1));
    }
}
