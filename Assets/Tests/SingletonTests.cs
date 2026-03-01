using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Utilities;

public class SingletonTests
{
    private GameObject _gameObject;

    [TearDown]
    public void TearDown()
    {
        if (_gameObject != null)
            Object.DestroyImmediate(_gameObject);
    }

    private static void InvokeAwake(MonoBehaviour behaviour)
    {
        var method = behaviour
            .GetType()
            .BaseType?.GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy
            );
        method?.Invoke(behaviour, null);
    }

    [Test]
    public void Singleton_AfterAddComponent_InstanceIsSetAndExists()
    {
        _gameObject = new GameObject("SingletonTest");
        var singleton = _gameObject.AddComponent<TestSingleton>();
        InvokeAwake(singleton);

        Assert.That(TestSingleton.Instance, Is.Not.Null);
        Assert.That(TestSingleton.Instance, Is.SameAs(singleton));
        Assert.That(TestSingleton.s_InstanceExists, Is.True);
    }

    [Test]
    public void Singleton_AfterDestroy_InstanceIsNull()
    {
        _gameObject = new GameObject("SingletonTest");
        var singleton = _gameObject.AddComponent<TestSingleton>();
        InvokeAwake(singleton);
        Assert.That(TestSingleton.s_InstanceExists, Is.True);

        Object.DestroyImmediate(_gameObject);
        _gameObject = null;

        Assert.That(TestSingleton.Instance, Is.Null);
        Assert.That(TestSingleton.s_InstanceExists, Is.False);
    }

    [Test]
    public void PersistentSingleton_AfterAddComponent_InstanceIsSetAndExists()
    {
        _gameObject = new GameObject("PersistentSingletonTest");
        var singleton = _gameObject.AddComponent<TestPersistentSingleton>();
        InvokeAwake(singleton);

        Assert.That(TestPersistentSingleton.Instance, Is.Not.Null);
        Assert.That(TestPersistentSingleton.Instance, Is.SameAs(singleton));
        Assert.That(TestPersistentSingleton.s_InstanceExists, Is.True);
    }

    [Test]
    public void PersistentSingleton_AfterDestroy_InstanceIsNull()
    {
        _gameObject = new GameObject("PersistentSingletonTest");
        var singleton = _gameObject.AddComponent<TestPersistentSingleton>();
        InvokeAwake(singleton);
        Assert.That(TestPersistentSingleton.s_InstanceExists, Is.True);

        Object.DestroyImmediate(_gameObject);
        _gameObject = null;

        Assert.That(TestPersistentSingleton.Instance, Is.Null);
        Assert.That(TestPersistentSingleton.s_InstanceExists, Is.False);
    }

    private class TestSingleton : Singleton<TestSingleton> { }

    private class TestPersistentSingleton : PersistentSingleton<TestPersistentSingleton> { }
}
