using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click setup for the project's particle effects
/// (rubric 5 - "Implement particle effects for attack and enemy destruction"):
///
///   • HitSpark   - a quick white/orange spark burst. Assigned to Projectile.hitEffect
///                  (arrow impact) and EnemyHealth.hitEffect (every sword hit lands one).
///   • DeathBurst - a bigger ash/dust poof, played once an enemy's health reaches zero
///                  (EnemyHealth.deathEffect).
///
/// Both prefabs are self-contained (main.stopAction = Destroy), so instantiating one and
/// walking away - which is all Projectile/EnemyHealth do - is enough; nothing needs to
/// clean them up afterwards.
///
/// Run:  Tools ▸ Medieval Survival ▸ Setup Particle Effects
/// Re-runnable: it overwrites the existing prefabs/materials/assignments instead of
/// duplicating them.
/// </summary>
public static class ParticleEffectsSetup
{
    private const string PrefabFolder = "Assets/_Game/Prefabs/Effects";
    private const string MatFolder = "Assets/_Game/Art/FX";

    private static readonly string[] EnemyPrefabs =
    {
        "Assets/_Game/Prefabs/Enemies/Enemy 1.prefab",
        "Assets/_Game/Prefabs/Enemies/ArcherEnemy.prefab",
    };

    private const string ArrowPrefab = "Assets/_Game/Prefabs/Projectiles/Arrow.prefab";

    [MenuItem("Tools/Medieval Survival/Setup Particle Effects")]
    public static void Run()
    {
        EnsureFolder("Assets/_Game/Prefabs", "Effects");
        EnsureFolder("Assets/_Game", "Art");
        EnsureFolder("Assets/_Game/Art", "FX");

        // Sprites/Default: always present, always alpha-blends, and respects the particle
        // system's per-particle vertex color/alpha out of the box - no URP surface/blend
        // keyword juggling required to get a transparent particle rendering correctly.
        Texture2D dot = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
        Material sparkMat = GetOrCreateMaterial("FX_Spark", dot);
        Material dustMat = GetOrCreateMaterial("FX_Dust", dot);

        GameObject hitSpark = BuildHitSpark(sparkMat);
        GameObject deathBurst = BuildDeathBurst(dustMat);

        AssetDatabase.SaveAssets();

        int hits = 0;
        hits += AssignField<Projectile>(ArrowPrefab, "hitEffect", hitSpark);
        foreach (string path in EnemyPrefabs)
        {
            hits += AssignField<EnemyHealth>(path, "hitEffect", hitSpark);
            hits += AssignField<EnemyHealth>(path, "deathEffect", deathBurst);
        }

        Debug.Log($"[ParticleEffectsSetup] Created {PrefabFolder}/HitSpark.prefab + DeathBurst.prefab, " +
                  $"wired {hits} field(s) across {EnemyPrefabs.Length} enemy prefab(s) + the arrow.");
    }

    // ---- prefab construction ----

    private static GameObject BuildHitSpark(Material mat)
    {
        GameObject go = new GameObject("HitSpark");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.92f, 0.6f), new Color(1f, 0.55f, 0.08f));
        main.gravityModifier = 0.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)10, (short)14) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.06f;

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = FadeOutGradient(new Color(1f, 0.55f, 0.05f));

        ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));

        ConfigureRenderer(ps, mat);

        return SavePrefab(go, "HitSpark");
    }

    private static GameObject BuildDeathBurst(Material mat)
    {
        GameObject go = new GameObject("DeathBurst");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.42f, 0.34f, 0.24f), new Color(0.62f, 0.52f, 0.4f));
        main.gravityModifier = 0.1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)22, (short)28) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = FadeOutGradient(new Color(0.5f, 0.42f, 0.3f));

        // Dust puffs up and expands rather than shrinking like a spark does.
        ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.3f));

        ConfigureRenderer(ps, mat);

        return SavePrefab(go, "DeathBurst");
    }

    private static Gradient FadeOutGradient(Color tint)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(tint, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    private static void ConfigureRenderer(ParticleSystem ps, Material mat)
    {
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static GameObject SavePrefab(GameObject sceneObject, string name)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(sceneObject, path);
        Object.DestroyImmediate(sceneObject);
        return asset;
    }

    // ---- material ----

    private static Material GetOrCreateMaterial(string name, Texture2D texture)
    {
        string path = $"{MatFolder}/{name}.mat";
        Shader shader = Shader.Find("Sprites/Default")
                        ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                        ?? Shader.Find("Universal Render Pipeline/Unlit");

        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else
        {
            m.shader = shader;
        }

        if (texture != null)
        {
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texture);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture);
        }
        if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);

        EditorUtility.SetDirty(m);
        return m;
    }

    // ---- field wiring ----

    /// <summary>
    /// Finds the first component of type T anywhere in the prefab and assigns its private
    /// [SerializeField] <paramref name="fieldName"/> to <paramref name="value"/> via
    /// SerializedObject, then saves the prefab. Returns 1 on success, 0 if the prefab,
    /// component or field couldn't be found.
    /// </summary>
    private static int AssignField<T>(string prefabPath, string fieldName, GameObject value) where T : Component
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            Debug.LogWarning($"ParticleEffectsSetup: prefab not found - {prefabPath}");
            return 0;
        }

        T comp = root.GetComponentInChildren<T>(true);
        if (comp == null)
        {
            Debug.LogWarning($"ParticleEffectsSetup: no {typeof(T).Name} on {prefabPath}");
            PrefabUtility.UnloadPrefabContents(root);
            return 0;
        }

        SerializedObject so = new SerializedObject(comp);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"ParticleEffectsSetup: field '{fieldName}' not found on {typeof(T).Name} ({prefabPath})");
            PrefabUtility.UnloadPrefabContents(root);
            return 0;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        return 1;
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            AssetDatabase.CreateFolder(parent, child);
    }
}
