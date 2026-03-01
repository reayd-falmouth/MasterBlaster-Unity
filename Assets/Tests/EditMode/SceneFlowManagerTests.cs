using Core;
using NUnit.Framework;

public class SceneFlowManagerTests
{
    [Test]
    public void ShouldAdvanceOnAnyInput_Credits_ReturnsTrue()
    {
        Assert.That(SceneFlowManager.ShouldAdvanceOnAnyInput(FlowState.Credits), Is.True);
    }

    [Test]
    public void ShouldAdvanceOnAnyInput_Title_ReturnsTrue()
    {
        Assert.That(SceneFlowManager.ShouldAdvanceOnAnyInput(FlowState.Title), Is.True);
    }

    [Test]
    public void ShouldAdvanceOnAnyInput_Menu_ReturnsFalse()
    {
        Assert.That(SceneFlowManager.ShouldAdvanceOnAnyInput(FlowState.Menu), Is.False);
    }

    [Test]
    public void ShouldAdvanceOnAnyInput_Countdown_ReturnsFalse()
    {
        Assert.That(SceneFlowManager.ShouldAdvanceOnAnyInput(FlowState.Countdown), Is.False);
    }

    [Test]
    public void ShouldAdvanceOnAnyInput_OtherStates_ReturnFalse()
    {
        Assert.That(SceneFlowManager.ShouldAdvanceOnAnyInput(FlowState.Game), Is.False);
        Assert.That(SceneFlowManager.ShouldAdvanceOnAnyInput(FlowState.Standings), Is.False);
        Assert.That(SceneFlowManager.ShouldAdvanceOnAnyInput(FlowState.Wheel), Is.False);
        Assert.That(SceneFlowManager.ShouldAdvanceOnAnyInput(FlowState.Shop), Is.False);
        Assert.That(SceneFlowManager.ShouldAdvanceOnAnyInput(FlowState.Overs), Is.False);
    }
}
