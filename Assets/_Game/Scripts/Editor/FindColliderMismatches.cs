using UnityEditor;
using UnityEngine;

/// <summary>
/// Scans every MeshRenderer in the open scene (skipping the player, enemies, and UI) and flags
/// two things reviewers keep tripping over: objects with no Collider at all (the player/arrows
/// pass straight through, e.g. Rock_A_01.prefab), and objects whose Collider's world-space bounds
/// are far smaller than their visual mesh's bounds (the "invisible wall in the wrong place" feel -
/// you see the stairs/rock but bump into empty air a step early, or walk through part of it).
///
/// This only reports candidates - it doesn't touch anything, since fixing the actual collider
/// shape needs your eyes on the object in the Scene view (usually: add/replace with a MeshCollider,
/// or resize the BoxCollider to match).
///
/// Run:  Tools ▸ Medieval Survival ▸ Find Collider Mismatches
/// </summary>
public static class FindColliderMismatches
{
    // Below this fraction of the renderer's bounds volume, the collider is flagged as undersized.
    private const float VolumeRatioThreshold = 0.5f;

    [MenuItem("Tools/Medieval Survival/Find Collider Mismatches")]
    public static void Run()
    {
        MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        int missing = 0;
        int undersized = 0;

        foreach (MeshRenderer renderer in renderers)
        {
            GameObject go = renderer.gameObject;
            if (go.CompareTag("Player") || go.CompareTag("Enemy")) continue;
            if (go.GetComponentInParent<Canvas>() != null) continue;

            Collider col = go.GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"[ColliderCheck] No collider at all: '{GetPath(go)}'", go);
                missing++;
                continue;
            }

            float rendererVolume = BoundsVolume(renderer.bounds);
            float colliderVolume = BoundsVolume(col.bounds);
            if (rendererVolume > 0.01f && colliderVolume / rendererVolume < VolumeRatioThreshold)
            {
                Debug.LogWarning(
                    $"[ColliderCheck] Collider looks undersized vs. its mesh on '{GetPath(go)}' " +
                    $"(collider ~{colliderVolume:0.0}m³ vs. mesh ~{rendererVolume:0.0}m³) - " +
                    "select it and compare the Collider gizmo to the mesh in the Scene view.", go);
                undersized++;
            }
        }

        EditorUtility.DisplayDialog("Collider Check",
            $"Checked {renderers.Length} renderers.\n\n" +
            $"Missing collider: {missing}\n" +
            $"Possibly undersized collider: {undersized}\n\n" +
            "See the Console for the full list (click each entry to select the object).", "OK");
    }

    private static float BoundsVolume(Bounds b) => b.size.x * b.size.y * b.size.z;

    private static string GetPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}
