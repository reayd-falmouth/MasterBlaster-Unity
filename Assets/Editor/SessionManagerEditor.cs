#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Core;
using Scenes.Shop;

[CustomEditor(typeof(SessionManager))]
public class SessionManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var sm = (SessionManager)target;
        if (sm == null)
            return;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Session state (runtime)", EditorStyles.boldLabel);

        if (sm.MatchWinnerPlayerId != 0)
            EditorGUILayout.LabelField("Match winner", $"Player {sm.MatchWinnerPlayerId} – {sm.GetMatchWinnerName()}");

        EditorGUILayout.LabelField("Player coins");
        EditorGUI.indentLevel++;
        if (sm.PlayerCoins != null)
        {
            foreach (var kv in sm.PlayerCoins)
                EditorGUILayout.LabelField($"Player {kv.Key}", kv.Value.ToString());
        }
        else
            EditorGUILayout.LabelField("(none)");
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Player wins");
        EditorGUI.indentLevel++;
        if (sm.PlayerWins != null)
        {
            foreach (var kv in sm.PlayerWins)
                EditorGUILayout.LabelField($"Player {kv.Key}", kv.Value.ToString());
        }
        else
            EditorGUILayout.LabelField("(none)");
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Player upgrades");
        EditorGUI.indentLevel++;
        if (sm.PlayerUpgrades != null)
        {
            foreach (var playerKv in sm.PlayerUpgrades)
            {
                EditorGUILayout.LabelField($"Player {playerKv.Key}");
                EditorGUI.indentLevel++;
                if (playerKv.Value != null)
                {
                    foreach (var upgradeKv in playerKv.Value)
                    {
                        if (upgradeKv.Value != 0)
                            EditorGUILayout.LabelField(
                                upgradeKv.Key.ToString(),
                                upgradeKv.Value.ToString()
                            );
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
        else
            EditorGUILayout.LabelField("(none)");
        EditorGUI.indentLevel--;

        if (Application.isPlaying)
            Repaint();
    }
}
#endif
