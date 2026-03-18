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

        // Use Unity's null check: after destroy, Instance may be C# non-null but a destroyed object (Unity treats as null)
        Assert.That(
            TestSingleton.Instance == null,
            Is.True,
            "Instance should be null or destroyed after DestroyImmediate"
        );
        Assert.That(TestSingleton.s_InstanceExists, Is.False);
    }

    private class TestSingleton : Singleton<TestSingleton> { }
}
