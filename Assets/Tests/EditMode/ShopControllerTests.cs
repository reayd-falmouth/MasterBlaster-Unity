using System.Reflection;
using Core;
using NUnit.Framework;
using Scenes.Shop;
using UnityEngine;

public class ShopControllerTests
{
    private GameObject _sessionManagerGo;

    [SetUp]
    public void SetUp()
    {
        if (SessionManager.Instance != null)
            Object.DestroyImmediate(SessionManager.Instance.gameObject);

        _sessionManagerGo = new GameObject("SessionManagerForShopTests");
        var sessionManager = _sessionManagerGo.AddComponent<SessionManager>();
        SetSessionManagerInstance(sessionManager);
    }

    private static void SetSessionManagerInstance(SessionManager instance)
    {
        var singletonType = typeof(SessionManager).BaseType?.BaseType;
        if (singletonType == null)
            return;
        var field = singletonType.GetField(
            "s_instance",
            BindingFlags.Static | BindingFlags.NonPublic
        );
        field?.SetValue(null, instance);
    }

    [TearDown]
    public void TearDown()
    {
        if (_sessionManagerGo != null)
            Object.DestroyImmediate(_sessionManagerGo);
    }

    [Test]
    public void GetPointerTextForIndex_WhenSelected_ReturnsPointer()
    {
        Assert.That(ShopController.GetPointerTextForIndex(0, 0), Is.EqualTo("> "));
        Assert.That(ShopController.GetPointerTextForIndex(2, 2), Is.EqualTo("> "));
    }

    [Test]
    public void GetPointerTextForIndex_WhenNotSelected_ReturnsSpaces()
    {
        Assert.That(ShopController.GetPointerTextForIndex(1, 0), Is.EqualTo("  "));
        Assert.That(ShopController.GetPointerTextForIndex(0, 1), Is.EqualTo("  "));
    }

    [Test]
    public void GetPointerTextForIndex_OnlyOneIndexShowsPointerForGivenSelectedIndex()
    {
        int selectedIndex = 2;
        int itemCount = 5;
        int pointerCount = 0;
        for (int i = 0; i < itemCount; i++)
        {
            string text = ShopController.GetPointerTextForIndex(i, selectedIndex);
            if (text == "> ")
                pointerCount++;
        }
        Assert.That(
            pointerCount,
            Is.EqualTo(1),
            "Exactly one option should show the pointer for a given selectedIndex"
        );
    }

    [Test]
    public void GetCoinsToDisplayForPlayer_ReturnsSessionManagerCoinsForPlayer()
    {
        var sessionManager = SessionManager.Instance;
        sessionManager.Initialize(2);
        sessionManager.SetCoins(1, 5);
        sessionManager.SetCoins(2, 0);

        Assert.That(ShopController.GetCoinsToDisplayForPlayer(1), Is.EqualTo(5));
        Assert.That(ShopController.GetCoinsToDisplayForPlayer(2), Is.EqualTo(0));
    }

    [Test]
    public void GetCoinsToDisplayForPlayer_WhenSessionManagerNull_ReturnsZero()
    {
        Object.DestroyImmediate(_sessionManagerGo);
        _sessionManagerGo = null;
        // After destroy, Instance is null
        Assert.That(ShopController.GetCoinsToDisplayForPlayer(1), Is.EqualTo(0));
    }
}
