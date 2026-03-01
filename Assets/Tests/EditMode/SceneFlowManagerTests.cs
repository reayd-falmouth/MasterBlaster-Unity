using Core;
using NUnit.Framework;
using UnityEngine;

public class SceneFlowManagerTests
{
    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey("Gambling");
        PlayerPrefs.DeleteKey("Shop");
    }

    // -------- GetNextState (transition logic; countdown always before arena) --------

    [Test]
    public void GetNextState_Credits_ReturnsTitle()
    {
        Assert.That(SceneFlowManager.GetNextState(FlowState.Credits), Is.EqualTo(FlowState.Title));
    }

    [Test]
    public void GetNextState_Title_ReturnsMenu()
    {
        Assert.That(SceneFlowManager.GetNextState(FlowState.Title), Is.EqualTo(FlowState.Menu));
    }

    [Test]
    public void GetNextState_Menu_ReturnsCountdown()
    {
        Assert.That(SceneFlowManager.GetNextState(FlowState.Menu), Is.EqualTo(FlowState.Countdown));
    }

    [Test]
    public void GetNextState_Countdown_ReturnsGame()
    {
        Assert.That(SceneFlowManager.GetNextState(FlowState.Countdown), Is.EqualTo(FlowState.Game));
    }

    [Test]
    public void GetNextState_Shop_ReturnsCountdown()
    {
        Assert.That(SceneFlowManager.GetNextState(FlowState.Shop), Is.EqualTo(FlowState.Countdown));
    }

    [Test]
    public void GetNextState_Standings_GamblingOn_ReturnsWheel()
    {
        PlayerPrefs.SetInt("Gambling", 1);
        try
        {
            Assert.That(SceneFlowManager.GetNextState(FlowState.Standings), Is.EqualTo(FlowState.Wheel));
        }
        finally
        {
            PlayerPrefs.DeleteKey("Gambling");
        }
    }

    [Test]
    public void GetNextState_Standings_GamblingOff_ShopOn_ReturnsShop()
    {
        PlayerPrefs.SetInt("Gambling", 0);
        PlayerPrefs.SetInt("Shop", 1);
        try
        {
            Assert.That(SceneFlowManager.GetNextState(FlowState.Standings), Is.EqualTo(FlowState.Shop));
        }
        finally
        {
            PlayerPrefs.DeleteKey("Gambling");
            PlayerPrefs.DeleteKey("Shop");
        }
    }

    [Test]
    public void GetNextState_Standings_BothOff_ReturnsCountdown()
    {
        PlayerPrefs.SetInt("Gambling", 0);
        PlayerPrefs.SetInt("Shop", 0);
        try
        {
            Assert.That(SceneFlowManager.GetNextState(FlowState.Standings), Is.EqualTo(FlowState.Countdown));
        }
        finally
        {
            PlayerPrefs.DeleteKey("Gambling");
            PlayerPrefs.DeleteKey("Shop");
        }
    }

    [Test]
    public void GetNextState_Wheel_ShopOn_ReturnsShop()
    {
        PlayerPrefs.SetInt("Shop", 1);
        try
        {
            Assert.That(SceneFlowManager.GetNextState(FlowState.Wheel), Is.EqualTo(FlowState.Shop));
        }
        finally
        {
            PlayerPrefs.DeleteKey("Shop");
        }
    }

    [Test]
    public void GetNextState_Wheel_ShopOff_ReturnsCountdown()
    {
        PlayerPrefs.SetInt("Shop", 0);
        try
        {
            Assert.That(SceneFlowManager.GetNextState(FlowState.Wheel), Is.EqualTo(FlowState.Countdown));
        }
        finally
        {
            PlayerPrefs.DeleteKey("Shop");
        }
    }

    [Test]
    public void GetNextState_StandingsAndWheel_NeverReturnGame()
    {
        PlayerPrefs.SetInt("Gambling", 0);
        PlayerPrefs.SetInt("Shop", 0);
        try
        {
            Assert.That(SceneFlowManager.GetNextState(FlowState.Standings), Is.Not.EqualTo(FlowState.Game));
            Assert.That(SceneFlowManager.GetNextState(FlowState.Wheel), Is.Not.EqualTo(FlowState.Game));
        }
        finally
        {
            PlayerPrefs.DeleteKey("Gambling");
            PlayerPrefs.DeleteKey("Shop");
        }
    }

    // -------- ShouldAdvanceOnAnyInput --------

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
