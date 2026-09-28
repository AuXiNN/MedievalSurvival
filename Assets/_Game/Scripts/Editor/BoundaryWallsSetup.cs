using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click setup for invisible boundary walls around the fighting area, so the player can't
/// wander off the ruin platform into the open field around it.
///
/// Creates a ring of tall, thin BoxColliders (no renderer - invisible) approximating a circle
/// around the arena (see ArenaBounds), matching the round shape of the ruin platform instead of
/// boxing it in with 4 flat walls.
///
/// Run:  Tools ▸ Medieval Survival ▸ Setup Boundary Walls     (then save the scene)
/// Re-runnable: clears out whatever it placed last time and rebuilds fresh.
/// </summary>
public static class BoundaryWallsSetup
{
    private const string ParentName = "Boundary Walls";
    private const float WallHeight = 15f;
    private const float WallThickness = 2f;
    private const int Segments = 20; // more segments = smoother circle

    [MenuItem("Tools/Medieval Survival/Setup Boundary Walls")]
    public static void Run()
    {
        if (!ArenaBounds.TryCompute(out Vector3 center, out float radius))
        {
            EditorUtility.DisplayDialog("Boundary Walls",
                "No EnemySpawner with spawn points found in the open scene.", "OK");
            return;
        }

        Transform parent = GetOrCreateParent();
        ClearChildren(parent);

        float centerY = center.y + WallHeight * 0.5f;
        // Slightly over-length each segment so neighboring segments overlap a bit and there's
        // no gap at the seams for the player (or an arrow) to slip through.
        float segmentLength = (2f * Mathf.PI * radius / Segments) * 1.1f;

        for (int i = 0; i < Segments; i++)
        {
            float angleDeg = i * (360f / Segments);
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(
                center.x + Mathf.Sin(angleRad) * radius,
                centerY,
                center.z + Mathf.Cos(angleRad) * radius);

            PlaceWallSegment(parent, $"Wall_{i:00}", pos, Quaternion.Euler(0f, angleDeg, 0f),
                new Vector3(segmentLength, WallHeight, WallThickness));
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Boundary Walls",
            $"Placed {Segments} invisible wall segments in a ring around the arena " +
            $"(radius ~{radius:0}m, centered on the spawn points).\n\n" +
            "Save the scene to keep them.", "OK");
    }

    private static Transform GetOrCreateParent()
    {
        GameObject existing = GameObject.Find(ParentName);
        if (existing != null) return existing.transform;

        GameObject parent = new GameObject(ParentName);
        Undo.RegisterCreatedObjectUndo(parent, "Create Boundary Walls");
        return parent.transform;
    }

    // Removes whatever walls a previous run placed (e.g. the old 4-sided box layout) so
    // re-running always leaves a clean, correctly-shaped ring behind.
    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
    }

    private static void PlaceWallSegment(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject go = new GameObject(name, typeof(BoxCollider));
        go.transform.SetParent(parent);
        Undo.RegisterCreatedObjectUndo(go, "Create Boundary Wall");

        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = scale;

        BoxCollider box = go.GetComponent<BoxCollider>();
        box.isTrigger = false; // solid - physically blocks the player's CharacterController
        box.size = Vector3.one;
        box.center = Vector3.zero;

        EditorUtility.SetDirty(go);
    }
}
