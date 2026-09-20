using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click setup for invisible boundary walls around the level's Terrain, so the player
/// can't wander out into the open/empty world past the edge of the playable area.
///
/// Creates 4 tall, thin BoxColliders (no renderer - invisible) hugging each edge of the
/// Terrain, sized from the Terrain's actual dimensions so it works no matter how big the
/// terrain is.
///
/// Run:  Tools ▸ Medieval Survival ▸ Setup Boundary Walls     (then save the scene)
/// Re-runnable: it repositions the existing walls instead of duplicating them.
/// </summary>
public static class BoundaryWallsSetup
{
    private const string ParentName = "Boundary Walls";
    private const float WallHeight = 40f;
    private const float WallThickness = 2f;

    [MenuItem("Tools/Medieval Survival/Setup Boundary Walls")]
    public static void Run()
    {
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Boundary Walls", "No Terrain found in the open scene.", "OK");
            return;
        }

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size; // x = width, z = length, y = max height

        float minX = origin.x;
        float maxX = origin.x + size.x;
        float minZ = origin.z;
        float maxZ = origin.z + size.z;
        float centerY = origin.y + WallHeight * 0.5f;

        Transform parent = GetOrCreateParent();

        // North / South walls run along X, sit just outside the Z edges.
        PlaceWall(parent, "Wall_North", new Vector3((minX + maxX) * 0.5f, centerY, maxZ),
            new Vector3(size.x + WallThickness * 2f, WallHeight, WallThickness));
        PlaceWall(parent, "Wall_South", new Vector3((minX + maxX) * 0.5f, centerY, minZ),
            new Vector3(size.x + WallThickness * 2f, WallHeight, WallThickness));

        // East / West walls run along Z, sit just outside the X edges.
        PlaceWall(parent, "Wall_East", new Vector3(maxX, centerY, (minZ + maxZ) * 0.5f),
            new Vector3(WallThickness, WallHeight, size.z + WallThickness * 2f));
        PlaceWall(parent, "Wall_West", new Vector3(minX, centerY, (minZ + maxZ) * 0.5f),
            new Vector3(WallThickness, WallHeight, size.z + WallThickness * 2f));

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Boundary Walls",
            $"Placed 4 invisible boundary walls around the terrain ({size.x:0}m x {size.z:0}m).\n\n" +
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

    private static void PlaceWall(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        Transform wall = parent.Find(name);
        GameObject go;
        if (wall != null)
        {
            go = wall.gameObject;
        }
        else
        {
            go = new GameObject(name, typeof(BoxCollider));
            go.transform.SetParent(parent);
            Undo.RegisterCreatedObjectUndo(go, "Create Boundary Wall");
        }

        go.transform.position = position;
        go.transform.localScale = scale;

        BoxCollider box = go.GetComponent<BoxCollider>();
        if (box == null) box = go.AddComponent<BoxCollider>();
        box.isTrigger = false; // solid - physically blocks the player's CharacterController
        box.size = Vector3.one;
        box.center = Vector3.zero;

        EditorUtility.SetDirty(go);
    }
}
