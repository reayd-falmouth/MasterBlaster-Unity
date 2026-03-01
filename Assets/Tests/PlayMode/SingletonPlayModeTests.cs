using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Utilities;

public class SingletonPlayModeTests
{
    private GameObject _gameObject;

    [TearDown]
    public void TearDown()
    {
        if (_gameObject != null)
            Object.DestroyImmediate(_gameObject);
    }

    [UnityTest]
    public IEnumerator PersistentSingleton_AfterAddComponent_InstanceIsSetAndExists()
    {
        if (!Application.isPlaying)
        {
            Assert.Ignore(
                "PlayMode test must be run from the PlayMode tab (or with -testPlatform playmode)."
            );
        }
        _gameObject = new GameObject("PersistentSingletonTest");
        var singleton = _gameObject.AddComponent<TestPersistentSingleton>();

        yield return null;

        Assert.That(TestPersistentSingleton.Instance, Is.Not.Null);
        Assert.That(TestPersistentSingleton.Instance, Is.SameAs(singleton));
        Assert.That(TestPersistentSingleton.s_InstanceExists, Is.True);
    }

    [UnityTest]
    public IEnumerator PersistentSingleton_AfterDestroy_InstanceIsNull()
    {
        if (!Application.isPlaying)
        {
            Assert.Ignore(
                "PlayMode test must be run from the PlayMode tab (or with -testPlatform playmode)."
            );
        }
        _gameObject = new GameObject("PersistentSingletonTest");
        _gameObject.AddComponent<TestPersistentSingleton>();

        yield return null;

        Assert.That(TestPersistentSingleton.s_InstanceExists, Is.True);

        Object.DestroyImmediate(_gameObject);
        _gameObject = null;

        yield return null;

        Assert.That(TestPersistentSingleton.Instance, Is.Null);
        Assert.That(TestPersistentSingleton.s_InstanceExists, Is.False);
    }

    private class TestPersistentSingleton : PersistentSingleton<TestPersistentSingleton> { }
}
