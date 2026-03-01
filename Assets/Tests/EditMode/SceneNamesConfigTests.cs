using Core;
using NUnit.Framework;

public class SceneNamesConfigTests
{
    [Test]
    public void DefaultConfig_HasExpectedSceneNames()
    {
        var config = new SceneNamesConfig();
        Assert.That(config.Credits, Is.EqualTo("Credits"));
        Assert.That(config.Title, Is.EqualTo("Title"));
        Assert.That(config.Menu, Is.EqualTo("Menu"));
        Assert.That(config.Countdown, Is.EqualTo("Countdown"));
        Assert.That(config.Game, Is.EqualTo("Game"));
        Assert.That(config.Standings, Is.EqualTo("Standings"));
        Assert.That(config.Wheel, Is.EqualTo("Wheel"));
        Assert.That(config.Shop, Is.EqualTo("Shop"));
        Assert.That(config.Overs, Is.EqualTo("Overs"));
    }
}
