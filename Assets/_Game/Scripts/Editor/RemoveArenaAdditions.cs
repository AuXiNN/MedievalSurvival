using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Undoes everything BoundaryWallsSetup and EnrichArenaEnvironment added to the scene: deletes
/// the "Boundary Walls" and "Arena Environment Props" GameObjects, removes the tree instances
/// EnrichArenaEnvironment placed (identified by its marker color, so hand-placed trees are left
/// alone), and reverses the grass height/width bump back toward its original values.
///
/// Run:  Tools ▸ Medieval Survival ▸ Remove Arena Additions     (then save the scene)
/// </summary>
public static class RemoveArenaAdditions
{
    private static readonly Color TreeMarkerColor = new Color(0.501f, 0.502f, 0.503f, 1f);

    [MenuItem("Tools/Medieval Survival/Remove Arena Additions")]
    public static void Run()
    {
        int removedObjects = 0;
        removedObjects += DestroyIfExists("Boundary Walls");
        removedObjects += DestroyIfExists("Arena Environment Props");

        int treesRemoved = 0;
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        if (terrain != null)
        {
            TerrainData data = terrain.terrainData;

            var kept = new System.Collections.Generic.List<TreeInstance>();
            foreach (TreeInstance instance in data.treeInstances)
            {
                if (IsOurMarker(instance.color)) treesRemoved++;
                else kept.Add(instance);
            }
            data.treeInstances = kept.ToArray();

            DetailPrototype[] details = data.detailPrototypes;
            if (details != null)
            {
                for (int i = 0; i < details.Length; i++)
                {
                    details[i].minHeight /= 1.35f;
                    details[i].maxHeight /= 1.35f;
                    details[i].minWidth /= 1.15f;
                    details[i].maxWidth /= 1.15f;
                }
                data.detailPrototypes = details;
            }

            terrain.Flush();
            EditorUtility.SetDirty(data);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Remove Arena Additions",
            $"Removed {removedObjects} object group(s), {treesRemoved} added tree(s), " +
            "and reverted the grass size bump.\n\nSave the scene to keep this.", "OK");
    }

    private static int DestroyIfExists(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing == null) return 0;
        Undo.DestroyObjectImmediate(existing);
        return 1;
    }

    private static bool IsOurMarker(Color c) =>
        Mathf.Abs(c.r - TreeMarkerColor.r) < 0.001f &&
        Mathf.Abs(c.g - TreeMarkerColor.g) < 0.001f &&
        Mathf.Abs(c.b - TreeMarkerColor.b) < 0.001f;
}
