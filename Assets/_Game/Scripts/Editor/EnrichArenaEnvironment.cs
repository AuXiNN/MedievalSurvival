using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Makes the area around the ruin platform feel as populated as the forest further out. Instead
/// of scattering one prop type uniformly at random, this places natural-looking CLUSTERS - rock
/// outcrops, bush thickets, fern/grass patches - rotating between prop categories that already
/// exist in the project (Rocks/ and Details/ folders from the TerrainDemoScene_URP package), so
/// the result reads as designed set-dressing rather than noise. Also scatters extra trees using
/// the Terrain's own tree system, and makes every grass detail prototype taller/wider.
///
/// Run:  Tools ▸ Medieval Survival ▸ Enrich Arena Environment     (then save the scene)
/// Re-runnable: deletes everything it placed last time (its own "Arena Environment Props" parent
/// and its marked tree instances) before scattering a fresh batch, so it never piles up more and
/// more on each run.
/// </summary>
public static class EnrichArenaEnvironment
{
    private const string RocksFolder = "Assets/_ThirdParty/TerrainDemoScene_URP/Prefabs/Rocks";
    private const string DetailsFolder = "Assets/_ThirdParty/TerrainDemoScene_URP/Prefabs/Details";
    private const string PropsParentName = "Arena Environment Props";

    private const float RingWidth = 20f;      // how far out past the arena boundary props/trees scatter

    private const int ClusterCount = 14;
    private const int PropsPerClusterMin = 3;
    private const int PropsPerClusterMax = 7;
    private const float ClusterSpread = 3f;   // how far a cluster's props jitter from its center
    private const float MinPropGap = 1.2f;    // minimum distance between any two placed props

    private const int TreeCount = 25;
    private const float MinTreeGap = 3f;

    private static readonly Color TreeMarkerColor = new Color(0.501f, 0.502f, 0.503f, 1f);

    [MenuItem("Tools/Medieval Survival/Enrich Arena Environment")]
    public static void Run()
    {
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Enrich Arena Environment", "No Terrain found in the open scene.", "OK");
            return;
        }
        if (!ArenaBounds.TryCompute(out Vector3 center, out float innerRadius))
        {
            EditorUtility.DisplayDialog("Enrich Arena Environment",
                "No EnemySpawner with spawn points found in the open scene (and nothing usable selected).", "OK");
            return;
        }

        TerrainData data = terrain.terrainData;
        float outerRadius = innerRadius + RingWidth;

        GameObject[] rocks = LoadPrefabs(RocksFolder);
        GameObject[] bushes = LoadPrefabs(DetailsFolder, "Bush", "Shrub", "Heather");
        GameObject[] undergrowth = LoadPrefabs(DetailsFolder, "Fern", "Grass");

        Transform propsParent = GetOrCreateCleanParent(PropsParentName);
        int placedProps = ScatterClusters(terrain, propsParent, center, innerRadius, outerRadius, rocks, bushes, undergrowth);
        int treesAdded = ScatterTrees(terrain, data, center, innerRadius, outerRadius);
        int prototypesBumped = BumpGrassSize(data);

        terrain.Flush();
        EditorUtility.SetDirty(data);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Enrich Arena Environment",
            $"Placed {placedProps} rocks/bushes/ferns in {ClusterCount} clusters, " +
            $"{treesAdded} trees, between {innerRadius:0}m-{outerRadius:0}m from the arena center.\n" +
            $"Made {prototypesBumped} grass detail type(s) taller/wider.\n\n" +
            "For denser grass right around the arena, use the Terrain's Paint Details brush " +
            "by hand - save the scene once you're happy with it.", "OK");
    }

    // Loads every prefab in a folder, optionally restricted to filenames containing any of the
    // given substrings (e.g. "Bush", "Fern") so callers can pull a themed subset.
    private static GameObject[] LoadPrefabs(string folder, params string[] nameFilters)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        List<GameObject> result = new List<GameObject>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (nameFilters.Length > 0 && !nameFilters.Any(f => fileName.Contains(f)))
                continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) result.Add(prefab);
        }
        return result.ToArray();
    }

    private static Transform GetOrCreateCleanParent(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        GameObject parent = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(parent, "Create Arena Environment Props");
        return parent.transform;
    }

    // Picks a handful of cluster centers around the ring, then jitters 3-7 props of ONE
    // category (rocks, or bushes, or undergrowth - rotating per cluster) around each center,
    // so the result reads as distinct outcrops/thickets/patches instead of uniform noise.
    private static int ScatterClusters(Terrain terrain, Transform parent, Vector3 center,
        float innerRadius, float outerRadius, GameObject[] rocks, GameObject[] bushes, GameObject[] undergrowth)
    {
        GameObject[][] categories = { rocks, bushes, undergrowth };
        List<Vector2> placed = new List<Vector2>();
        int totalPlaced = 0;

        for (int c = 0; c < ClusterCount; c++)
        {
            GameObject[] pool = categories[c % categories.Length];
            if (pool == null || pool.Length == 0) continue;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(innerRadius, outerRadius);
            Vector2 clusterCenter = new Vector2(center.x + Mathf.Cos(angle) * dist, center.z + Mathf.Sin(angle) * dist);

            int propCount = Random.Range(PropsPerClusterMin, PropsPerClusterMax + 1);
            for (int i = 0; i < propCount; i++)
            {
                Vector2 pos = clusterCenter + Random.insideUnitCircle * ClusterSpread;

                float distFromArena = Vector2.Distance(pos, new Vector2(center.x, center.z));
                if (distFromArena < innerRadius) continue; // never place inside the arena itself

                bool tooClose = placed.Any(p => Vector2.Distance(p, pos) < MinPropGap);
                if (tooClose) continue;

                GameObject prefab = pool[Random.Range(0, pool.Length)];
                float groundY = terrain.SampleHeight(new Vector3(pos.x, 0f, pos.y)) + terrain.transform.position.y;

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetParent(parent);
                instance.transform.position = new Vector3(pos.x, groundY, pos.y);
                instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                instance.transform.localScale = prefab.transform.localScale * Random.Range(0.85f, 1.25f);

                placed.Add(pos);
                totalPlaced++;
            }
        }

        return totalPlaced;
    }

    private static int ScatterTrees(Terrain terrain, TerrainData data, Vector3 center, float innerRadius, float outerRadius)
    {
        if (data.treePrototypes == null || data.treePrototypes.Length == 0)
            return 0; // nothing to scatter with

        Vector3 terrainPos = terrain.transform.position;
        Vector3 size = data.size;

        List<TreeInstance> instances = new List<TreeInstance>();
        foreach (TreeInstance existing in data.treeInstances)
        {
            if (!IsOurMarker(existing.color))
                instances.Add(existing); // keep everything except trees we added on a previous run
        }

        List<Vector2> placedWorldXZ = new List<Vector2>();
        int added = 0;
        int attempts = 0;
        int maxAttempts = TreeCount * 20;

        while (added < TreeCount && attempts < maxAttempts)
        {
            attempts++;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(innerRadius, outerRadius);
            float worldX = center.x + Mathf.Cos(angle) * r;
            float worldZ = center.z + Mathf.Sin(angle) * r;

            float normX = (worldX - terrainPos.x) / size.x;
            float normZ = (worldZ - terrainPos.z) / size.z;
            if (normX < 0.02f || normX > 0.98f || normZ < 0.02f || normZ > 0.98f)
                continue;

            Vector2 candidate = new Vector2(worldX, worldZ);
            if (placedWorldXZ.Any(p => Vector2.Distance(p, candidate) < MinTreeGap))
                continue;

            float terrainHeight = data.GetInterpolatedHeight(normX, normZ);

            instances.Add(new TreeInstance
            {
                position = new Vector3(normX, terrainHeight / size.y, normZ),
                prototypeIndex = Random.Range(0, data.treePrototypes.Length),
                widthScale = Random.Range(0.85f, 1.3f),
                heightScale = Random.Range(0.85f, 1.3f),
                color = TreeMarkerColor,
                lightmapColor = Color.white,
                rotation = Random.Range(0f, Mathf.PI * 2f)
            });

            placedWorldXZ.Add(candidate);
            added++;
        }

        data.treeInstances = instances.ToArray();
        return added;
    }

    private static bool IsOurMarker(Color c) =>
        Mathf.Abs(c.r - TreeMarkerColor.r) < 0.001f &&
        Mathf.Abs(c.g - TreeMarkerColor.g) < 0.001f &&
        Mathf.Abs(c.b - TreeMarkerColor.b) < 0.001f;

    private static int BumpGrassSize(TerrainData data)
    {
        DetailPrototype[] details = data.detailPrototypes;
        if (details == null || details.Length == 0) return 0;

        for (int i = 0; i < details.Length; i++)
        {
            details[i].minHeight *= 1.35f;
            details[i].maxHeight *= 1.35f;
            details[i].minWidth *= 1.15f;
            details[i].maxWidth *= 1.15f;
        }
        data.detailPrototypes = details;
        return details.Length;
    }
}
