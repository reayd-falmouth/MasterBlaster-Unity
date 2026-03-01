using Core;
using NUnit.Framework;
using Scenes.Arena.Bomb;
using Scenes.Arena.Player;
using Scenes.Shop;
using UnityEngine;

public class BombControllerApplyUpgradesTests
{
    private GameObject _sessionGo;
    private GameObject _playerGo;
    private SessionManager _sessionManager;
    private BombController _bombController;

    [SetUp]
    public void SetUp()
    {
        // Ensure no leftover SessionManager from another test so our new one becomes Instance
        if (SessionManager.Instance != null)
            Object.DestroyImmediate(SessionManager.Instance.gameObject);

        _sessionGo = new GameObject("SessionManager");
        _sessionManager = _sessionGo.AddComponent<SessionManager>();
        _sessionManager.Initialize(2);
        _sessionManager.SetUpgradeLevel(1, ShopItemType.ExtraBomb, 1);
        _sessionManager.SetUpgradeLevel(1, ShopItemType.PowerUp, 1);

        _playerGo = new GameObject("Player");
        _playerGo.AddComponent<PlayerController>();
        _bombController = _playerGo.AddComponent<BombController>();
        _bombController.bombAmount = 1;
        _bombController.explosionRadius = 1;
    }

    [TearDown]
    public void TearDown()
    {
        if (_playerGo != null)
            Object.DestroyImmediate(_playerGo);
        if (_sessionGo != null)
            Object.DestroyImmediate(_sessionGo);
    }

    [Test]
    public void ApplyUpgrades_WithSessionManager_AppliesExtraBombAndPowerUp()
    {
        _bombController.ApplyUpgrades(1);

        Assert.That(_bombController.bombAmount, Is.EqualTo(2), "base 1 + 1 extra bomb");
        Assert.That(_bombController.explosionRadius, Is.EqualTo(2), "base 1 + 1 power up");
    }

    [Test]
    public void ApplyUpgrades_CalledTwice_IsIdempotent()
    {
        _bombController.ApplyUpgrades(1);
        Assert.That(_bombController.bombAmount, Is.EqualTo(2));
        Assert.That(_bombController.explosionRadius, Is.EqualTo(2));

        _bombController.ApplyUpgrades(1);
        Assert.That(
            _bombController.bombAmount,
            Is.EqualTo(2),
            "second call should not double-apply"
        );
        Assert.That(_bombController.explosionRadius, Is.EqualTo(2));
    }

    [Test]
    public void ApplyUpgrades_WhenSessionManagerNull_DoesNotThrow()
    {
        Object.DestroyImmediate(_sessionGo);
        _sessionGo = null;

        Assert.DoesNotThrow(() => _bombController.ApplyUpgrades(1));
    }
}
