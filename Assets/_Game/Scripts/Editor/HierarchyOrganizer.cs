using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using StarterAssets;

/// <summary>
/// Sweeps the open scene's loose root objects into a handful of labelled group objects
/// (_Systems, _UI, _Lighting, _Spawns, _Environment) so the Hierarchy is readable.
///
/// Nothing moves in world space and nothing is renamed - reparenting only. All lookups in
/// code are by tag / plain name / component type, so grouping doesn't break anything.
/// The player is left at the root on purpose.
///
/// Usage: open Assets/_Game/Scenes/Gameplay.unity, then
/// Tools > Medieval Survival > Organize Gameplay Hierarchy. Undo (Ctrl+Z) reverts it;
/// save the scene to keep it.
/// </summary>
public static class HierarchyOrganizer
{
    private const string Systems = "_Systems";
    private const string UI = "_UI";
    private const string Lighting = "_Lighting";
    private const string Spawns = "_Spawns";
    private const string Environment = "_Environment";

    private static readonly string[] GroupOrder = { Systems, UI, Lighting, Spawns, Environment };

    [MenuItem("Tools/Medieval Survival/Organize Gameplay Hierarchy")]
    public static void Organize()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("Organize Hierarchy", "No scene is open.", "OK");
            return;
        }

        var buckets = new Dictionary<string, List<GameObject>>
        {
            { Systems, new List<GameObject>() },
            { UI, new List<GameObject>() },
            { Lighting, new List<GameObject>() },
            { Spawns, new List<GameObject>() },
            { Environment, new List<GameObject>() },
        };

        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (GroupOrder.Contains(go.name)) continue; // an existing group header
            if (StaysAtRoot(go)) continue;             // player + camera rig - leave alone

            buckets[Classify(go)].Add(go);
        }

        int moved = 0;
        foreach (string group in GroupOrder)
            moved += Nest(group, buckets[group]);

        // Push the group headers to the top of the Hierarchy, in a sensible order.
        for (int i = GroupOrder.Length - 1; i >= 0; i--)
        {
            GameObject g = GameObject.Find(GroupOrder[i]);
            if (g != null) g.transform.SetAsFirstSibling();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("Organize Hierarchy",
            moved == 0
                ? "Nothing left to group - the hierarchy is already organized."
                : $"Grouped {moved} root objects into {GroupOrder.Length} sections.\n\n" +
                  "Check it looks right, then save the scene (Ctrl+Z undoes it).", "OK");
    }

    private static string Classify(GameObject go)
    {
        if (go.GetComponentInChildren<Canvas>(true) != null || go.name == "EventSystem")
            return UI;
        if (IsSystem(go))
            return Systems;
        if (IsLighting(go))
            return Lighting;
        if (go.name.ToLower().Contains("spawn"))
            return Spawns;
        return Environment;
    }

    // The player and the camera rig are load-bearing for movement/Cinemachine and are found
    // by tag/name anyway - safest to leave them untouched at the root.
    private static bool StaysAtRoot(GameObject go)
    {
        return go.CompareTag("Player")
            || go.GetComponentInChildren<ThirdPersonController>(true) != null
            || go.GetComponentInChildren<Camera>(true) != null
            || go.name.Contains("PlayerArmature")
            || go.name.Contains("NestedParentArmature")
            || go.name.ToLower().Contains("camera");
    }

    private static readonly HashSet<string> SystemNames = new HashSet<string>
    {
        "GameManager", "EnemySpawner", "UIManager", "GameController",
        "AudioManager", "MusicManager", "SoundManager", "PauseMenuController", "Managers",
    };

    private static bool IsSystem(GameObject go)
    {
        if (SystemNames.Contains(go.name)) return true;
        return go.GetComponent<GameManager>() != null
            || go.GetComponent<EnemySpawner>() != null
            || go.GetComponent<UIManager>() != null;
    }

    private static bool IsLighting(GameObject go)
    {
        if (go.GetComponentInChildren<Light>(true) != null) return true;
        if (go.GetComponentInChildren<ReflectionProbe>(true) != null) return true;
        if (go.GetComponentInChildren<LightProbeGroup>(true) != null) return true;

        string n = go.name.ToLower();
        return n.Contains("light") || n.Contains("sky") || n.Contains("volume")
            || n.Contains("postprocess") || n.Contains("post process") || n.Contains("probe");
    }

    private static int Nest(string groupName, List<GameObject> objects)
    {
        if (objects.Count == 0) return 0;

        GameObject group = GameObject.Find(groupName);
        if (group == null)
        {
            group = new GameObject(groupName);
            Undo.RegisterCreatedObjectUndo(group, "Create " + groupName);
        }

        int count = 0;
        foreach (GameObject go in objects)
        {
            if (go == group || go.transform.parent == group.transform) continue;
            Undo.SetTransformParent(go.transform, group.transform, "Group " + go.name);
            count++;
        }
        return count;
    }
}
