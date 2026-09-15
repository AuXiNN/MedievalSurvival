using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click setup for the project's Physics Materials
/// (rubric 5 - "Use Physics Materials to adjust the interaction between objects"):
///
///   • StoneGround  - firm, zero-bounce footing. Assigned to the Terrain collider so every
///                    Rigidbody in the game (enemies, arrows that land, anything knocked
///                    around) grips the packed earth instead of skating across it.
///   • EnemyBody    - low friction + a little bounce. Assigned to the melee/archer capsule
///                    colliders so a cluster of enemies slides apart on contact instead of
///                    grinding into one stack, and they give a small hop when they land.
///
/// Run:  Tools ▸ Medieval Survival ▸ Setup Physics Materials     (then save the scene)
/// Re-runnable: it updates the existing materials/assignments instead of duplicating them.
/// </summary>
public static class PhysicsSetup
{
    private const string Folder = "Assets/_Game/Physics";

    private static readonly string[] EnemyPrefabs =
    {
        "Assets/_Game/Prefabs/Enemies/Enemy 1.prefab",
        "Assets/_Game/Prefabs/Enemies/ArcherEnemy.prefab",
    };

    [MenuItem("Tools/Medieval Survival/Setup Physics Materials")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/_Game", "Physics");

        PhysicsMaterial stoneGround = GetOrCreate("StoneGround",
            dynamicFriction: 0.62f, staticFriction: 0.68f, bounciness: 0f,
            frictionCombine: PhysicsMaterialCombine.Average,
            bounceCombine: PhysicsMaterialCombine.Average);

        PhysicsMaterial enemyBody = GetOrCreate("EnemyBody",
            dynamicFriction: 0.12f, staticFriction: 0.12f, bounciness: 0.35f,
            frictionCombine: PhysicsMaterialCombine.Minimum,
            bounceCombine: PhysicsMaterialCombine.Average);

        AssetDatabase.SaveAssets();

        // --- StoneGround -> every TerrainCollider in the open scene ---
        int terrains = 0;
        foreach (TerrainCollider tc in Object.FindObjectsByType<TerrainCollider>(FindObjectsSortMode.None))
        {
            tc.sharedMaterial = stoneGround;
            EditorUtility.SetDirty(tc);
            terrains++;
        }

        // --- EnemyBody -> the enemy prefab capsule colliders ---
        int enemyColliders = 0;
        foreach (string path in EnemyPrefabs)
            enemyColliders += AssignToEnemyCapsule(path, enemyBody);

        if (terrains > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Physics Materials",
            $"Created / updated StoneGround + EnemyBody in {Folder}.\n\n" +
            $"StoneGround → {terrains} terrain collider(s) in this scene\n" +
            $"EnemyBody → {enemyColliders} enemy capsule collider(s)\n\n" +
            (terrains > 0 ? "Save the scene to keep the terrain assignment." : ""), "OK");
    }

    private static PhysicsMaterial GetOrCreate(string name, float dynamicFriction, float staticFriction,
        float bounciness, PhysicsMaterialCombine frictionCombine, PhysicsMaterialCombine bounceCombine)
    {
        string path = $"{Folder}/{name}.physicMaterial";
        PhysicsMaterial m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (m == null)
        {
            m = new PhysicsMaterial(name);
            AssetDatabase.CreateAsset(m, path);
        }

        m.dynamicFriction = dynamicFriction;
        m.staticFriction = staticFriction;
        m.bounciness = bounciness;
        m.frictionCombine = frictionCombine;
        m.bounceCombine = bounceCombine;
        EditorUtility.SetDirty(m);
        return m;
    }

    private static int AssignToEnemyCapsule(string prefabPath, PhysicsMaterial mat)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            Debug.LogWarning($"PhysicsSetup: prefab not found - {prefabPath}");
            return 0;
        }

        int n = 0;
        foreach (CapsuleCollider capsule in root.GetComponentsInChildren<CapsuleCollider>(true))
        {
            if (capsule.isTrigger) continue; // leave detection/hitbox triggers alone
            capsule.sharedMaterial = mat;
            n++;
        }

        if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        return n;
    }
}
