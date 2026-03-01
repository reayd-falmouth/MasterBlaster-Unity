using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Scenes.Arena;

public class CountdownControllerPlayModeTests
{
    private GameObject _canvasGo;
    private GameObject _countdownGo;
    private CountdownController _countdown;
    private Text _countdownText;

    [SetUp]
    public void SetUp()
    {
        _canvasGo = new GameObject("Canvas");
        var canvas = _canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var textGo = new GameObject("CountdownText");
        textGo.transform.SetParent(_canvasGo.transform, false);
        _countdownText = textGo.AddComponent<Text>();

        _countdownGo = new GameObject("CountdownController");
        _countdown = _countdownGo.AddComponent<CountdownController>();
        _countdown.countdownText = _countdownText;
        _countdown.interval = 0.05f;
    }

    [TearDown]
    public void TearDown()
    {
        if (_countdownGo != null)
            Object.DestroyImmediate(_countdownGo);
        if (_canvasGo != null)
            Object.DestroyImmediate(_canvasGo);
    }

    [UnityTest]
    public IEnumerator Countdown_UpdatesTextFrom3To2AfterOneInterval()
    {
        Assert.That(_countdownText.text, Is.EqualTo(""));
        yield return null;
        yield return null;
        Assert.That(_countdownText.text, Is.EqualTo("3"));
        yield return new WaitForSeconds(0.06f);
        Assert.That(_countdownText.text, Is.EqualTo("2"));
    }
}
