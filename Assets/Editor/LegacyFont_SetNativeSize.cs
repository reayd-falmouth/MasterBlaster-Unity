using UnityEditor;
using UnityEngine;

public static class LegacyFont_SetNativeSize {
    [MenuItem("Tools/Legacy Text/Set Native Size to 7 (Selected Font)")]
    public static void Set7() {
        var f = Selection.activeObject as Font;
        if (!f) { EditorUtility.DisplayDialog("Set Native Size", "Select a Font asset (.fontsettings) in Project.", "OK"); return; }
        var so = new SerializedObject(f);
        so.FindProperty("m_FontSize").intValue = 7;   // your bitmap’s pixel height
        so.FindProperty("m_LineSpacing").floatValue = 7f;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(f);
        AssetDatabase.SaveAssets();
    }
}