#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapTools
{
    [MenuItem("Tools/Arena/Compress All Tilemap Bounds")]
    public static void CompressAll()
    {
        foreach (var tm in Object.FindObjectsOfType<Tilemap>())
        {
            Undo.RecordObject(tm, "Compress Tilemap Bounds");
            tm.CompressBounds();
        }
        Debug.Log("Compressed bounds on all Tilemaps in scene.");
    }
}
#endif
