using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds a real MeshCollider back to rock or rubble props under "Platform Surface Props" -
/// wherever they currently sit, including anything you've since moved, rotated or rescaled by
/// hand. Does NOT regenerate or reposition anything, and doesn't touch grass/bushes.
///
/// Run:  Tools ▸ Medieval Survival ▸ Add Collision To Rocks
///       Tools ▸ Medieval Survival ▸ Add Collision To Rubble     (then save the scene)
/// Safe to re-run: skips anything that already has a collider.
///
/// Note: rubble is smaller and far more numerous than rocks, and sits anywhere across the
/// platform (not pushed out to the edges like rocks are) - giving it real collision has a
/// real chance of an enemy (no pathfinding in this project) clipping a piece and getting stuck.
/// Worth a playtest after running this.
/// </summary>
public static class AddRockCollision
{
    private const string PlatformPropsParentName = "Platform Surface Props";

    [MenuItem("Tools/Medieval Survival/Add Collision To Rocks")]
    public static void RunRocks() =>
        AddCollision("Add Collision To Rocks", "rock", go => IsFromFolder(go, "/Rocks/") || go.name.Contains("Rock"));

    [MenuItem("Tools/Medieval Survival/Add Collision To Rubble")]
    public static void RunRubble() =>
        AddCollision("Add Collision To Rubble", "rubble piece", go => IsFromFolder(go, "Rubble") || go.name.Contains("Rubble"));

    private static void AddCollision(string title, string label, Func<GameObject, bool> matches)
    {
        GameObject parent = GameObject.Find(PlatformPropsParentName);
        if (parent == null)
        {
            EditorUtility.DisplayDialog(title, $"No \"{PlatformPropsParentName}\" group found in the open scene.", "OK");
            return;
        }

        int added = 0;
        int alreadyHadOne = 0;
        int skipped = 0;

        // Walk every descendant, not just direct children, in case things got reorganized by hand.
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
        {
            if (t == parent.transform) continue;
            if (!matches(t.gameObject)) { skipped++; continue; }

            if (t.GetComponentInChildren<Collider>() != null) { alreadyHadOne++; continue; }

            MeshFilter filter = t.GetComponentInChildren<MeshFilter>();
            if (filter == null) continue;

            Undo.AddComponent<MeshCollider>(filter.gameObject);
            added++;
        }

        EditorUtility.DisplayDialog(title,
            $"Added a collider to {added} {label}(s). {alreadyHadOne} already had one, " +
            $"{skipped} object(s) skipped (not a match).\n\nSave the scene to keep this.", "OK");
    }

    // Checks whether this object's source prefab lives under a path fragment like "/Rocks/" or
    // contains "Rubble" in its asset path (the Rubble prefabs live under a longer, shared path).
    private static bool IsFromFolder(GameObject go, string pathFragment)
    {
        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(go);
        if (source == null) return false;
        string path = AssetDatabase.GetAssetPath(source);
        return !string.IsNullOrEmpty(path) && path.Contains(pathFragment);
    }
}
