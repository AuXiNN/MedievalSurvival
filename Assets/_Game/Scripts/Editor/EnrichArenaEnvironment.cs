using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Dresses the area around the ruin platform to match a real arid hilltop citadel (reference:
/// Jabal Al-Qal'a / Amman Citadel) instead of a lush European ruin - no moss, no ferns, sparse
/// dry vegetation, and a band of broken masonry right at the platform's base as if the ruin
/// itself is slowly spilling outward. Three concentric bands:
///
///   1. Debris band (right at the arena edge): scattered Rubble pieces from this same Altar
///      Ruins pack - already uses the corrected warm-limestone Altar.mat, so it matches the
///      platform with zero extra material work.
///   2. Full ring: natural rock outcrops (TerrainDemoScene_URP Rocks).
///   3. Full ring: sparse DRY scrub only (BushDry, GrassDry) - deliberately excludes the lush
///      Bush/Grass/Fern/Heather prefabs, which read as damp-climate and don't match the site.
///
/// Also scatters a small number of trees (a real hilltop citadel has very few, not a forest).
///
/// Run:  Tools ▸ Medieval Survival ▸ Enrich Arena Environment     (then save the scene)
/// Re-runnable: deletes everything it placed last time first.
/// </summary>
public static class EnrichArenaEnvironment
{
    private const string RocksFolder = "Assets/_ThirdParty/TerrainDemoScene_URP/Prefabs/Rocks";
    private const string DetailsFolder = "Assets/_ThirdParty/TerrainDemoScene_URP/Prefabs/Details";
    private const string RubbleFolder = "Assets/_ThirdParty/Altar_Ruins_FREE/Prefabs/Old/Meshes_with_the_bottom_faces";
    private const string PropsParentName = "Arena Environment Props";

    private const float DebrisBandWidth = 6f;   // how far past the arena edge the rubble band extends
    private const float RingWidth = 20f;        // how far out past the arena boundary rocks/scrub/trees scatter

    private const int DebrisCount = 10;
    private const float MinDebrisGap = 2f;

    private const int ClusterCount = 10;
    private const int PropsPerClusterMin = 2;
    private const int PropsPerClusterMax = 5;
    private const float ClusterSpread = 3f;
    private const float MinPropGap = 1.2f;

    private const int TreeCount = 10; // sparse - a real hilltop citadel isn't forested
    private const float MinTreeGap = 4f;

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

        GameObject[] rubble = LoadPrefabs(RubbleFolder, "Rubble");
        GameObject[] rocks = LoadPrefabs(RocksFolder);
        GameObject[] dryBushes = LoadPrefabs(DetailsFolder, "BushDry");
        GameObject[] dryGrass = LoadPrefabs(DetailsFolder, "GrassDry");

        Transform propsParent = GetOrCreateCleanParent(PropsParentName);

        int debrisPlaced = ScatterBand(terrain, propsParent, center, innerRadius, innerRadius + DebrisBandWidth,
            rubble, DebrisCount, MinDebrisGap);
        int placedProps = ScatterClusters(terrain, propsParent, center, innerRadius + DebrisBandWidth, outerRadius,
            rocks, dryBushes, dryGrass);
        int treesAdded = ScatterTrees(terrain, data, center, outerRadius * 0.5f, outerRadius);

        terrain.Flush();
        EditorUtility.SetDirty(data);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Enrich Arena Environment",
            $"Placed {debrisPlaced} rubble pieces right at the platform edge, " +
            $"{placedProps} rocks/dry-scrub props in {ClusterCount} clusters, " +
            $"and {treesAdded} trees further out.\n\n" +
            "Dry, sparse look to match the real site - no moss, no ferns, no lush grass this time.", "OK");
    }

    private const string PlatformPropsParentName = "Platform Surface Props";
    private const float PlatformEdgeMargin = 1.5f;   // keep clutter a little inside the true stone edge
    private const float SpawnPointClearance = 2.5f;  // don't block where enemies appear
    private const int PlatformPropCount = 100;       // generous - this is the "go all in" pass
    private const float MinPlatformPropGap = 1f;
    private const float PlatformRaycastHeight = 50f;

    /// <summary>
    /// Dresses the TOP of the platform itself - rubble, rocks and dry scrub resting directly on
    /// the stone, found by raycasting down onto the platform's own collider (so it follows the
    /// actual stairs/steps instead of assuming a flat surface). Every placed prop has its
    /// collider stripped immediately since this sits on the walkable fighting floor - purely
    /// decorative, never something the player or an enemy can get stuck on. Spawn points keep a
    /// clear radius so nothing blocks an enemy appearing. Leaves anything you placed by hand
    /// (e.g. a tree) alone - only touches its own "Platform Surface Props" parent.
    /// </summary>
    [MenuItem("Tools/Medieval Survival/Dress Platform Surface")]
    public static void RunOnPlatformSurface()
    {
        if (!ArenaBounds.TryCompute(out Vector3 center, out float radius))
        {
            EditorUtility.DisplayDialog("Dress Platform Surface",
                "No EnemySpawner with spawn points found in the open scene (and nothing usable selected).", "OK");
            return;
        }

        // Don't trust the selection bounding box's center Y as "the platform surface height" -
        // if the selection includes anything beyond just the platform (a parent group, the
        // terrain, etc.) that center can be wildly off. Find the real surface height by
        // raycasting straight down from the arena's horizontal center instead.
        Vector3 probeOrigin = new Vector3(center.x, center.y + PlatformRaycastHeight, center.z);
        if (!Physics.Raycast(probeOrigin, Vector3.down, out RaycastHit centerHit, PlatformRaycastHeight * 2f))
        {
            EditorUtility.DisplayDialog("Dress Platform Surface",
                $"Couldn't find the platform surface by raycasting straight down from the arena's " +
                $"center ({center.x:0.0}, {center.z:0.0}). Make sure the platform's collider is active " +
                "and there's nothing unexpected blocking the ray from directly above.", "OK");
            return;
        }
        float surfaceY = centerHit.point.y;

        // Don't trust the selection's radius either - it's measured a wildly wrong size twice
        // now depending on what happened to be selected. Find the platform's real edge by
        // physically probing outward from the center in a ring of directions and seeing how far
        // each one goes before the ground underneath stops being "the platform" (i.e. drops back
        // to the surrounding terrain's height). Using the narrowest direction keeps every prop
        // safely on the stone even if the platform isn't a perfect circle.
        float detectedRadius = DetectPlatformRadius(center.x, center.z, surfaceY);
        if (detectedRadius < 1f)
        {
            EditorUtility.DisplayDialog("Dress Platform Surface",
                "Could not detect the platform's edge by probing outward from its center - " +
                "the measured radius came out under 1m. Aborting rather than guess.", "OK");
            return;
        }
        radius = detectedRadius;

        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        List<Vector2> spawnPointsXZ = new List<Vector2>();
        if (spawner != null && spawner.SpawnPoints != null)
        {
            foreach (Transform point in spawner.SpawnPoints)
                if (point != null) spawnPointsXZ.Add(new Vector2(point.position.x, point.position.z));
        }

        GameObject[] rubble = LoadPrefabs(RubbleFolder, "Rubble");
        GameObject[] rocks = LoadPrefabs(RocksFolder);
        GameObject[] dryBushes = LoadPrefabs(DetailsFolder, "BushDry");
        GameObject[] dryGrass = LoadPrefabs(DetailsFolder, "GrassDry");
        GameObject[][] pools = { rubble, rubble, dryGrass, dryGrass, rocks, dryBushes, dryGrass }; // rubble + grey/dry grass weighted heaviest

        Transform parent = GetOrCreateCleanParent(PlatformPropsParentName);

        List<Vector2> placedXZ = new List<Vector2>();
        int placed = 0;
        int attempts = 0;
        int maxAttempts = PlatformPropCount * 30;
        int raysFired = 0;
        int raysHitSomething = 0;
        int hitsRejectedByHeight = 0;
        int hitsRejectedByEmptyPool = 0;

        while (placed < PlatformPropCount && attempts < maxAttempts)
        {
            attempts++;

            Vector2 offset = Random.insideUnitCircle * (radius - PlatformEdgeMargin);
            Vector2 posXZ = new Vector2(center.x + offset.x, center.z + offset.y);

            if (spawnPointsXZ.Any(p => Vector2.Distance(p, posXZ) < SpawnPointClearance)) continue;
            if (placedXZ.Any(p => Vector2.Distance(p, posXZ) < MinPlatformPropGap)) continue;

            Vector3 rayOrigin = new Vector3(posXZ.x, center.y + PlatformRaycastHeight, posXZ.y);
            raysFired++;
            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, PlatformRaycastHeight * 2f))
                continue;
            raysHitSomething++;
            // Must land on the platform itself, not fall through to the terrain far below it.
            // Generous tolerance since a platform with stairs can span several meters vertically.
            if (Mathf.Abs(hit.point.y - surfaceY) > 15f) { hitsRejectedByHeight++; continue; }

            GameObject[] pool = pools[placed % pools.Length];
            if (pool == null || pool.Length == 0) { hitsRejectedByEmptyPool++; continue; }
            GameObject prefab = pool[Random.Range(0, pool.Length)];

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent);
            instance.transform.position = hit.point;
            instance.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) *
                                           Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            instance.transform.localScale = prefab.transform.localScale * Random.Range(0.9f, 1.4f);

            foreach (Collider col in instance.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);

            placedXZ.Add(posXZ);
            placed++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        string diagnostics = $"Debug: {raysFired} rays fired, {raysHitSomething} hit something, " +
            $"{hitsRejectedByHeight} rejected by height check, {hitsRejectedByEmptyPool} rejected (empty prop pool). " +
            $"Measured platform surface Y: {surfaceY:0.00} (selection center Y was {center.y:0.00}), radius {radius:0.00}.";

        EditorUtility.DisplayDialog("Dress Platform Surface",
            $"Placed {placed} decorative props directly on the platform surface, keeping clear of all " +
            $"{spawnPointsXZ.Count} spawn points. All colliders stripped - purely visual, won't block " +
            $"movement.\n\n{diagnostics}\n\nSave the scene to keep this.", "OK");
    }

    private const int EdgeProbeDirections = 16;
    private const float EdgeProbeStep = 1f;
    private const float EdgeProbeMaxDistance = 150f;
    private const float EdgeProbeHeightTolerance = 15f; // same tolerance used when actually placing props
    private const int EdgeProbeMaxConsecutiveMisses = 3; // tolerate a small gap/crack before giving up on a direction

    // Probes outward from (centerX, centerZ) in a ring of directions, stepping out until the
    // ground stops being within height tolerance of the platform's surface, and returns the
    // SHORTEST of those distances - the safest radius that stays on the platform in every
    // direction, even if the platform isn't a perfect circle.
    private static float DetectPlatformRadius(float centerX, float centerZ, float surfaceY)
    {
        float shortestEdge = float.MaxValue;

        for (int d = 0; d < EdgeProbeDirections; d++)
        {
            float angle = d * (360f / EdgeProbeDirections) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            float lastGoodDistance = 0f;
            int consecutiveMisses = 0;

            for (float dist = EdgeProbeStep; dist <= EdgeProbeMaxDistance; dist += EdgeProbeStep)
            {
                float x = centerX + dir.x * dist;
                float z = centerZ + dir.y * dist;
                Vector3 origin = new Vector3(x, surfaceY + PlatformRaycastHeight, z);

                bool onPlatform = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, PlatformRaycastHeight * 2f) &&
                                   Mathf.Abs(hit.point.y - surfaceY) <= EdgeProbeHeightTolerance;

                if (onPlatform)
                {
                    lastGoodDistance = dist;
                    consecutiveMisses = 0;
                }
                else
                {
                    consecutiveMisses++;
                    if (consecutiveMisses >= EdgeProbeMaxConsecutiveMisses) break;
                }
            }

            shortestEdge = Mathf.Min(shortestEdge, lastGoodDistance);
        }

        return shortestEdge;
    }

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

    // Scatters pieces from one pool uniformly across an annulus (e.g. the rubble debris band),
    // rather than in clusters - debris from a crumbling structure spreads out, it doesn't clump.
    private static int ScatterBand(Terrain terrain, Transform parent, Vector3 center,
        float innerRadius, float outerRadius, GameObject[] pool, int count, float minGap)
    {
        if (pool == null || pool.Length == 0) return 0;

        List<Vector2> placed = new List<Vector2>();
        int attempts = 0;
        int maxAttempts = count * 20;
        int placedCount = 0;

        while (placedCount < count && attempts < maxAttempts)
        {
            attempts++;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(innerRadius, outerRadius);
            Vector2 pos = new Vector2(center.x + Mathf.Cos(angle) * dist, center.z + Mathf.Sin(angle) * dist);

            if (placed.Any(p => Vector2.Distance(p, pos) < minGap)) continue;

            PlaceProp(terrain, parent, pool[Random.Range(0, pool.Length)], pos);
            placed.Add(pos);
            placedCount++;
        }

        return placedCount;
    }

    private static int ScatterClusters(Terrain terrain, Transform parent, Vector3 center,
        float innerRadius, float outerRadius, GameObject[] rocks, GameObject[] dryBushes, GameObject[] dryGrass)
    {
        GameObject[][] categories = { rocks, dryBushes, dryGrass };
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
                if (distFromArena < innerRadius) continue;

                if (placed.Any(p => Vector2.Distance(p, pos) < MinPropGap)) continue;

                PlaceProp(terrain, parent, pool[Random.Range(0, pool.Length)], pos);
                placed.Add(pos);
                totalPlaced++;
            }
        }

        return totalPlaced;
    }

    private static void PlaceProp(Terrain terrain, Transform parent, GameObject prefab, Vector2 posXZ)
    {
        float groundY = terrain.SampleHeight(new Vector3(posXZ.x, 0f, posXZ.y)) + terrain.transform.position.y;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent);
        instance.transform.position = new Vector3(posXZ.x, groundY, posXZ.y);
        instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        instance.transform.localScale = prefab.transform.localScale * Random.Range(0.85f, 1.25f);
    }

    private static int ScatterTrees(Terrain terrain, TerrainData data, Vector3 center, float innerRadius, float outerRadius)
    {
        if (data.treePrototypes == null || data.treePrototypes.Length == 0)
            return 0;

        Vector3 terrainPos = terrain.transform.position;
        Vector3 size = data.size;

        List<TreeInstance> instances = new List<TreeInstance>();
        foreach (TreeInstance existing in data.treeInstances)
        {
            if (!IsOurMarker(existing.color))
                instances.Add(existing);
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
}
