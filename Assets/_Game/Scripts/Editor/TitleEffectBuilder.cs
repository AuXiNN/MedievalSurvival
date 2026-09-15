using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

/// <summary>
/// Attaches <see cref="TitleTextEffect"/> to the Main Menu's game-title text (metallic
/// gradient + sweeping glint + breathing glow + float + drop shadow + reveal).
///
/// Usage: open Assets/_Game/Scenes/MainMenu.unity, then
/// Tools > Medieval Survival > Add Title Effect. Save the scene afterwards.
/// </summary>
public static class TitleEffectBuilder
{
    [MenuItem("Tools/Medieval Survival/Add Title Effect")]
    public static void Add()
    {
        TMP_Text title = FindTitle();
        if (title == null)
        {
            EditorUtility.DisplayDialog("Add Title Effect",
                "Couldn't find a title TextMeshPro in the open scene. Open MainMenu.unity first.", "OK");
            return;
        }

        TitleTextEffect fx = title.GetComponent<TitleTextEffect>();
        if (fx == null)
            fx = Undo.AddComponent<TitleTextEffect>(title.gameObject);

        EditorUtility.SetDirty(title.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Selection.activeGameObject = title.gameObject;
        EditorUtility.DisplayDialog("Add Title Effect",
            $"Added TitleTextEffect to \"{title.gameObject.name}\" (\"{title.text}\").\n\n" +
            "Press Play to see it, tweak the values in the Inspector, then save the scene.", "OK");
    }

    // Prefer an object literally named "TitleText"; otherwise the biggest TMP in the scene.
    private static TMP_Text FindTitle()
    {
        TMP_Text[] all = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (TMP_Text t in all)
            if (t.gameObject.name == "TitleText")
                return t;

        TMP_Text biggest = null;
        foreach (TMP_Text t in all)
            if (biggest == null || t.fontSize > biggest.fontSize)
                biggest = t;
        return biggest;
    }
}
