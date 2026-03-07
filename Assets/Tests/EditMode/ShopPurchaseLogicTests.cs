using NUnit.Framework;
using Scenes.Shop;

public class ShopPurchaseLogicTests
{
    [Test]
    public void GetNewLevelAfterPurchase_StackableExtraBomb_IncrementsLevel()
    {
        Assert.That(
            ShopPurchaseLogic.GetNewLevelAfterPurchase(ShopItemType.ExtraBomb, 0),
            Is.EqualTo(1)
        );
        Assert.That(
            ShopPurchaseLogic.GetNewLevelAfterPurchase(ShopItemType.ExtraBomb, 2),
            Is.EqualTo(3)
        );
    }

    [Test]
    public void GetNewLevelAfterPurchase_StackablePowerUp_IncrementsLevel()
    {
        Assert.That(
            ShopPurchaseLogic.GetNewLevelAfterPurchase(ShopItemType.PowerUp, 0),
            Is.EqualTo(1)
        );
        Assert.That(
            ShopPurchaseLogic.GetNewLevelAfterPurchase(ShopItemType.PowerUp, 5),
            Is.EqualTo(6)
        );
    }

    [Test]
    public void GetNewLevelAfterPurchase_StackableSpeedUp_IncrementsLevel()
    {
        Assert.That(
            ShopPurchaseLogic.GetNewLevelAfterPurchase(ShopItemType.SpeedUp, 1),
            Is.EqualTo(2)
        );
    }

    [Test]
    public void GetNewLevelAfterPurchase_ToggleItems_ReturnsOne()
    {
        Assert.That(
            ShopPurchaseLogic.GetNewLevelAfterPurchase(ShopItemType.Superman, 0),
            Is.EqualTo(1)
        );
        Assert.That(
            ShopPurchaseLogic.GetNewLevelAfterPurchase(ShopItemType.Superman, 1),
            Is.EqualTo(1)
        );
        Assert.That(
            ShopPurchaseLogic.GetNewLevelAfterPurchase(ShopItemType.Ghost, 0),
            Is.EqualTo(1)
        );
        Assert.That(
            ShopPurchaseLogic.GetNewLevelAfterPurchase(ShopItemType.Protection, 0),
            Is.EqualTo(1)
        );
        Assert.That(
            ShopPurchaseLogic.GetNewLevelAfterPurchase(ShopItemType.Controller, 0),
            Is.EqualTo(1)
        );
        Assert.That(
            ShopPurchaseLogic.GetNewLevelAfterPurchase(ShopItemType.Timebomb, 0),
            Is.EqualTo(1)
        );
    }

    [Test]
    public void GetNewLevelAfterPurchase_Exit_ReturnsCurrentLevel()
    {
        Assert.That(
            ShopPurchaseLogic.GetNewLevelAfterPurchase(ShopItemType.Exit, 0),
            Is.EqualTo(0)
        );
        Assert.That(
            ShopPurchaseLogic.GetNewLevelAfterPurchase(ShopItemType.Exit, 3),
            Is.EqualTo(3)
        );
    }

    [Test]
    public void CanAfford_CoinsGreaterOrEqualCost_ReturnsTrue()
    {
        Assert.That(ShopPurchaseLogic.CanAfford(5, 3), Is.True);
        Assert.That(ShopPurchaseLogic.CanAfford(3, 3), Is.True);
    }

    [Test]
    public void CanAfford_CoinsLessThanCost_ReturnsFalse()
    {
        Assert.That(ShopPurchaseLogic.CanAfford(2, 3), Is.False);
        Assert.That(ShopPurchaseLogic.CanAfford(0, 1), Is.False);
    }
}
