#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Hides the built-in AudioSource speaker gizmo in Scene and Game view
/// so player (and other) AudioSource components don't show the white speaker icon.
/// </summary>
[InitializeOnLoad]
public static class DisableAudioSourceGizmo
{
    private const string PrefsKey = "DisableAudioSourceGizmo.Enabled";

    static DisableAudioSourceGizmo()
    {
        EditorApplication.delayCall += Apply;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            Apply();
    }

    private static void Apply()
    {
        if (!EditorPrefs.GetBool(PrefsKey, true))
            return;

        // Unity 2022.2+ / Unity 6: GizmoUtility lives in UnityEditor
        var gizmoUtility = typeof(Editor).Assembly.GetType("UnityEditor.GizmoUtility");
        if (gizmoUtility != null)
        {
            var setEnabled = gizmoUtility.GetMethod("SetGizmoEnabled",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null,
                new[] { typeof(System.Type), typeof(bool), typeof(bool) },
                null);
            if (setEnabled != null)
            {
                setEnabled.Invoke(null, new object[] { typeof(AudioSource), false, false });
                return;
            }
        }

        // Fallback: try AnnotationUtility (older Unity) to set icon draw to none
        var annotationUtility = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AnnotationUtility");
        if (annotationUtility != null)
        {
            var setGizmoEnabled = annotationUtility.GetMethod("SetGizmoEnabled",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (setGizmoEnabled != null)
                setGizmoEnabled.Invoke(null, new object[] { typeof(AudioSource).GetHashCode(), false });
        }
    }

    [MenuItem("Tools/Disable AudioSource Gizmo (Speaker Icons)")]
    public static void Toggle()
    {
        bool current = EditorPrefs.GetBool(PrefsKey, true);
        EditorPrefs.SetBool(PrefsKey, !current);
        Apply();
        Debug.Log($"AudioSource gizmo (speaker icons) are now {(EditorPrefs.GetBool(PrefsKey, true) ? "hidden" : "shown")}. Use menu Tools > Disable AudioSource Gizmo to toggle.");
    }
}
#endif
