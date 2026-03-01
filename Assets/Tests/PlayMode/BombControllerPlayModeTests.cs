using System.Collections;
using NUnit.Framework;
using Scenes.Arena.Bomb;
using Scenes.Arena.Player;
using UnityEngine;
using UnityEngine.TestTools;

public class BombControllerPlayModeTests
{
    private GameObject _playerGo;

    [TearDown]
    public void TearDown()
    {
        if (_playerGo != null)
            Object.DestroyImmediate(_playerGo);
    }

    [UnityTest]
    public IEnumerator AddBomb_IncreasesBombAmountAndRemaining()
    {
        _playerGo = new GameObject("Player");
        var pc = _playerGo.AddComponent<PlayerController>();
        pc.playerId = 1;
        var bombController = _playerGo.AddComponent<BombController>();
        bombController.bombAmount = 1;

        yield return null;

        int amountBefore = bombController.bombAmount;
        bombController.AddBomb();

        Assert.That(bombController.bombAmount, Is.EqualTo(amountBefore + 1));
    }

    [UnityTest]
    public IEnumerator IncreaseBlastRadius_IncrementsExplosionRadius()
    {
        _playerGo = new GameObject("Player");
        _playerGo.AddComponent<PlayerController>();
        var bombController = _playerGo.AddComponent<BombController>();
        bombController.explosionRadius = 1;

        yield return null;

        bombController.IncreaseBlastRadius();
        Assert.That(bombController.explosionRadius, Is.EqualTo(2));
    }
}
