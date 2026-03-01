using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Scenes.Arena;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class CountdownControllerPlayModeTests
{
    private static void InvokeStart(MonoBehaviour behaviour)
    {
        var method = behaviour
            .GetType()
            .GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(behaviour, null);
    }

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
        if (!Application.isPlaying)
        {
            Assert.Ignore(
                "PlayMode test must be run from the PlayMode tab (or with -testPlatform playmode)."
            );
        }
        Assert.That(_countdownText.text, Is.EqualTo(""));
        // Start() is not reliably called on runtime-created objects in the test runner; invoke it so the coroutine runs
        InvokeStart(_countdown);
        yield return null;
        Assert.That(
            _countdownText.text,
            Is.EqualTo("3"),
            "Countdown should show 3 when coroutine starts"
        );
        yield return new WaitForSeconds(0.06f);
        Assert.That(_countdownText.text, Is.EqualTo("2"));
    }
}
