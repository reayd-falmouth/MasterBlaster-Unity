using Core;
using NUnit.Framework;

public class SceneFlowMapperTests
{
    private SceneNamesConfig _config;

    [SetUp]
    public void SetUp()
    {
        _config = new SceneNamesConfig();
    }

    [Test]
    public void StateForSceneName_WithDefaultConfig_ReturnsCorrectStateForEachScene()
    {
        Assert.That(
            SceneFlowMapper.StateForSceneName("Credits", _config),
            Is.EqualTo(FlowState.Credits)
        );
        Assert.That(
            SceneFlowMapper.StateForSceneName("Title", _config),
            Is.EqualTo(FlowState.Title)
        );
        Assert.That(SceneFlowMapper.StateForSceneName("Menu", _config), Is.EqualTo(FlowState.Menu));
        Assert.That(
            SceneFlowMapper.StateForSceneName("Countdown", _config),
            Is.EqualTo(FlowState.Countdown)
        );
        Assert.That(SceneFlowMapper.StateForSceneName("Game", _config), Is.EqualTo(FlowState.Game));
        Assert.That(
            SceneFlowMapper.StateForSceneName("Standings", _config),
            Is.EqualTo(FlowState.Standings)
        );
        Assert.That(
            SceneFlowMapper.StateForSceneName("Wheel", _config),
            Is.EqualTo(FlowState.Wheel)
        );
        Assert.That(SceneFlowMapper.StateForSceneName("Shop", _config), Is.EqualTo(FlowState.Shop));
        Assert.That(
            SceneFlowMapper.StateForSceneName("Overs", _config),
            Is.EqualTo(FlowState.Overs)
        );
    }

    [Test]
    public void StateForSceneName_UnknownSceneName_ReturnsMenu()
    {
        Assert.That(
            SceneFlowMapper.StateForSceneName("UnknownScene", _config),
            Is.EqualTo(FlowState.Menu)
        );
    }

    [Test]
    public void StateForSceneName_NullConfig_ReturnsMenu()
    {
        Assert.That(SceneFlowMapper.StateForSceneName("Credits", null), Is.EqualTo(FlowState.Menu));
    }

    [Test]
    public void SceneFor_WithDefaultConfig_ReturnsCorrectSceneNameForEachState()
    {
        Assert.That(SceneFlowMapper.SceneFor(FlowState.Credits, _config), Is.EqualTo("Credits"));
        Assert.That(SceneFlowMapper.SceneFor(FlowState.Title, _config), Is.EqualTo("Title"));
        Assert.That(SceneFlowMapper.SceneFor(FlowState.Menu, _config), Is.EqualTo("Menu"));
        Assert.That(
            SceneFlowMapper.SceneFor(FlowState.Countdown, _config),
            Is.EqualTo("Countdown")
        );
        Assert.That(SceneFlowMapper.SceneFor(FlowState.Game, _config), Is.EqualTo("Game"));
        Assert.That(
            SceneFlowMapper.SceneFor(FlowState.Standings, _config),
            Is.EqualTo("Standings")
        );
        Assert.That(SceneFlowMapper.SceneFor(FlowState.Wheel, _config), Is.EqualTo("Wheel"));
        Assert.That(SceneFlowMapper.SceneFor(FlowState.Shop, _config), Is.EqualTo("Shop"));
        Assert.That(SceneFlowMapper.SceneFor(FlowState.Overs, _config), Is.EqualTo("Overs"));
    }

    [Test]
    public void SceneFor_InvalidFlowState_ReturnsMenu()
    {
        var invalidState = (FlowState)(-1);
        Assert.That(SceneFlowMapper.SceneFor(invalidState, _config), Is.EqualTo("Menu"));
    }

    [Test]
    public void SceneFor_NullConfig_ReturnsMenu()
    {
        Assert.That(SceneFlowMapper.SceneFor(FlowState.Game, null), Is.EqualTo("Menu"));
    }

    [Test]
    public void StateForSceneName_And_SceneFor_AreInverse_ForAllStates()
    {
        foreach (FlowState state in System.Enum.GetValues(typeof(FlowState)))
        {
            string sceneName = SceneFlowMapper.SceneFor(state, _config);
            Assert.That(
                SceneFlowMapper.StateForSceneName(sceneName, _config),
                Is.EqualTo(state),
                $"Round-trip failed for {state} -> {sceneName}"
            );
        }
    }
}
