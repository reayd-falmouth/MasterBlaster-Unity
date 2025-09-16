// Assets/Editor/EnsureRectTransforms.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class EnsureRectTransforms
{
    [MenuItem("Tools/Board/Ensure RectTransforms")]
    private static void AddRectTransforms()
    {
        // 1) Grab whatever you have selected in the Hierarchy
        var boardGO = Selection.activeGameObject;
        if (boardGO == null)
        {
            EditorUtility.DisplayDialog(
                "No Board Selected", 
                "Please select your Board GameObject before running this tool.", 
                "OK"
            );
            return;
        }

        // 2) Register an undo group (so you can Ctrl+Z if needed)
        Undo.SetCurrentGroupName("Add RectTransforms to Board");
        int undoGroup = Undo.GetCurrentGroup();

        // 3) Pull **all** Transforms under that GO (including itself)
        var allTransforms = boardGO.GetComponentsInChildren<Transform>(true);

        foreach (var t in allTransforms)
        {
            var go = t.gameObject;

            // 4) If it doesn't already have a RectTransform...
            if (go.GetComponent<RectTransform>() == null)
            {
                // —preserve its old position/rotation/scale—
                Vector3 oldPos   = go.transform.localPosition;
                Quaternion oldRot= go.transform.localRotation;
                Vector3 oldScale = go.transform.localScale;

                // —add the RectTransform (replaces the old Transform under the hood)—
                Undo.AddComponent<RectTransform>(go);

                // —re-apply your original transform values—
                var rt = go.GetComponent<RectTransform>();
                rt.localPosition = oldPos;
                rt.localRotation = oldRot;
                rt.localScale    = oldScale;
            }
        }

        // 5) Collapse the undo group and report
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"✅ Added RectTransforms to all {allTransforms.Length} children of “{boardGO.name}”.");
    }
}
#endif
