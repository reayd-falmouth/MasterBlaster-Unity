using NUnit.Framework;
using Scenes.Shop;

public class ShopItemExtensionsTests
{
    [Test]
    public void ToDisplayName_ExtraBomb_ReturnsEXTRABOMB()
    {
        Assert.That(ShopItemType.ExtraBomb.ToDisplayName(), Is.EqualTo("EXTRABOMB"));
    }

    [Test]
    public void ToDisplayName_PowerUp_ReturnsPOWERUP()
    {
        Assert.That(ShopItemType.PowerUp.ToDisplayName(), Is.EqualTo("POWERUP"));
    }

    [Test]
    public void ToDisplayName_Superman_ReturnsSUPERMAN()
    {
        Assert.That(ShopItemType.Superman.ToDisplayName(), Is.EqualTo("SUPERMAN"));
    }

    [Test]
    public void ToDisplayName_Ghost_ReturnsGHOST()
    {
        Assert.That(ShopItemType.Ghost.ToDisplayName(), Is.EqualTo("GHOST"));
    }

    [Test]
    public void ToDisplayName_Timebomb_ReturnsTIMEBOMB()
    {
        Assert.That(ShopItemType.Timebomb.ToDisplayName(), Is.EqualTo("TIMEBOMB"));
    }

    [Test]
    public void ToDisplayName_Protection_ReturnsPROTECTION()
    {
        Assert.That(ShopItemType.Protection.ToDisplayName(), Is.EqualTo("PROTECTION"));
    }

    [Test]
    public void ToDisplayName_Controller_ReturnsCONTROLLER()
    {
        Assert.That(ShopItemType.Controller.ToDisplayName(), Is.EqualTo("CONTROLLER"));
    }

    [Test]
    public void ToDisplayName_SpeedUp_ReturnsSPEED_UP()
    {
        Assert.That(ShopItemType.SpeedUp.ToDisplayName(), Is.EqualTo("SPEED-UP"));
    }

    [Test]
    public void ToDisplayName_Exit_ReturnsEXIT()
    {
        Assert.That(ShopItemType.Exit.ToDisplayName(), Is.EqualTo("EXIT"));
    }

    [Test]
    public void ToDisplayName_UnknownEnumValue_ReturnsToStringToUpper()
    {
        var unknown = (ShopItemType)999;
        Assert.That(unknown.ToDisplayName(), Is.EqualTo("999"));
    }
}
