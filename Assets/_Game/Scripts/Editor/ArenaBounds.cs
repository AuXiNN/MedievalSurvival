using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared helper: computes the fighting arena's center/radius. Used by both BoundaryWallsSetup
/// and EnrichArenaEnvironment so the two tools always agree on where "the arena" actually is.
///
/// Prefers whatever is currently selected in the Hierarchy - select the platform's actual
/// pieces (floor, columns, stairs, whatever makes up the ruin) before running either tool and
/// the circle is sized exactly to that geometry. Falls back to a guess from EnemySpawner's
/// spawn points only if nothing relevant is selected.
/// </summary>
public static class ArenaBounds
{
    // Extra clearance added past the measured edge so walls don't clip into the platform itself.
    private const float SelectionPadding = 0.1f;
    // Shrinks the measured radius itself (applied before padding) - use this once padding alone
    // isn't enough, e.g. because the selected geometry (stairs, decorative rim) extends past
    // where the walls should actually sit.
    private const float SelectionRadiusScale = 0.7f;
    private const float SpawnPointPadding = 6f;

    public static bool TryCompute(out Vector3 center, out float radius)
    {
        if (TryFromSelection(out center, out radius)) return true;
        return TryFromSpawnPoints(out center, out radius);
    }

    // Combines the world-space Renderer bounds of everything selected (and its children) into
    // one box, then sizes a circle that fully circumscribes it (touches the corners, not just
    // the sides) so the platform is never clipped by a wall on its long axis.
    private static bool TryFromSelection(out Vector3 center, out float radius)
    {
        center = Vector3.zero;
        radius = 0f;

        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0) return false;

        Bounds combined = default;
        bool hasAny = false;
        foreach (GameObject go in selected)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (!hasAny) { combined = r.bounds; hasAny = true; }
                else combined.Encapsulate(r.bounds);
            }
        }
        if (!hasAny) return false;

        // For a round platform, its bounding box is (roughly) a square whose half-width already
        // equals the platform's true radius - using the box's diagonal instead would overshoot
        // by ~40%, sizing the circle to the box's corners rather than the platform's actual edge.
        center = combined.center;
        radius = Mathf.Max(combined.extents.x, combined.extents.z) * SelectionRadiusScale + SelectionPadding;
        return true;
    }

    private static bool TryFromSpawnPoints(out Vector3 center, out float radius)
    {
        center = Vector3.zero;
        radius = 0f;

        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner == null || spawner.SpawnPoints == null) return false;

        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Transform point in spawner.SpawnPoints)
        {
            if (point == null) continue;
            sum += point.position;
            count++;
        }
        if (count == 0) return false;

        center = sum / count;

        float maxDist = 0f;
        foreach (Transform point in spawner.SpawnPoints)
        {
            if (point == null) continue;
            float dist = Vector2.Distance(new Vector2(point.position.x, point.position.z), new Vector2(center.x, center.z));
            maxDist = Mathf.Max(maxDist, dist);
        }

        radius = maxDist + SpawnPointPadding;
        return true;
    }
}
