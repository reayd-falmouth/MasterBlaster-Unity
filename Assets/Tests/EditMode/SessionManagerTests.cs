using Core;
using NUnit.Framework;
using Scenes.Shop;
using UnityEngine;

public class SessionManagerTests
{
    private GameObject _gameObject;
    private SessionManager _sessionManager;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("SessionManagerTest");
        _sessionManager = _gameObject.AddComponent<SessionManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_gameObject != null)
            Object.DestroyImmediate(_gameObject);
    }

    [Test]
    public void Initialize_WithTwoPlayers_AllUpgradesZeroForBothPlayers()
    {
        _sessionManager.Initialize(2);

        Assert.That(_sessionManager.GetUpgradeLevel(1, ShopItemType.ExtraBomb), Is.EqualTo(0));
        Assert.That(_sessionManager.GetUpgradeLevel(2, ShopItemType.PowerUp), Is.EqualTo(0));

        foreach (ShopItemType type in System.Enum.GetValues(typeof(ShopItemType)))
        {
            if (type == ShopItemType.Exit)
                continue;
            Assert.That(
                _sessionManager.GetUpgradeLevel(1, type),
                Is.EqualTo(0),
                $"Player 1, {type}"
            );
            Assert.That(
                _sessionManager.GetUpgradeLevel(2, type),
                Is.EqualTo(0),
                $"Player 2, {type}"
            );
        }
    }

    [Test]
    public void SetUpgradeLevel_ThenGetUpgradeLevel_ReturnsSetValue()
    {
        _sessionManager.Initialize(2);
        _sessionManager.SetUpgradeLevel(1, ShopItemType.ExtraBomb, 3);

        Assert.That(_sessionManager.GetUpgradeLevel(1, ShopItemType.ExtraBomb), Is.EqualTo(3));
        Assert.That(_sessionManager.GetUpgradeLevel(2, ShopItemType.ExtraBomb), Is.EqualTo(0));
    }

    [Test]
    public void GetUpgradeLevel_UnknownPlayer_ReturnsZero()
    {
        _sessionManager.Initialize(2);
        Assert.That(_sessionManager.GetUpgradeLevel(99, ShopItemType.ExtraBomb), Is.EqualTo(0));
    }

    [Test]
    public void SetUpgradeLevel_UnknownPlayer_DoesNotThrow()
    {
        _sessionManager.Initialize(2);
        Assert.DoesNotThrow(() => _sessionManager.SetUpgradeLevel(99, ShopItemType.ExtraBomb, 5));
        Assert.That(_sessionManager.GetUpgradeLevel(99, ShopItemType.ExtraBomb), Is.EqualTo(0));
    }

    [Test]
    public void GetUpgradeLevel_ExitType_NotStoredInInitialize_ReturnsZero()
    {
        _sessionManager.Initialize(2);
        Assert.That(_sessionManager.GetUpgradeLevel(1, ShopItemType.Exit), Is.EqualTo(0));
    }

    [Test]
    public void Initialize_ThenPlayerUpgrades_HasEntryPerPlayer()
    {
        _sessionManager.Initialize(3);
        Assert.That(_sessionManager.PlayerUpgrades.Count, Is.EqualTo(3));
        Assert.That(_sessionManager.PlayerUpgrades.ContainsKey(1), Is.True);
        Assert.That(_sessionManager.PlayerUpgrades.ContainsKey(2), Is.True);
        Assert.That(_sessionManager.PlayerUpgrades.ContainsKey(3), Is.True);
    }
}
