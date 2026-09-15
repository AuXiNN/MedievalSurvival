using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Adds a "How To Play" button to the Main Menu's button column and builds the panel it
/// opens - a themed controls reference (move / look / sprint / jump / attack / block / ...)
/// with a Back button. Matches the Main Menu's bronze/gold Cinzel styling by cloning an
/// existing menu button, so hover/click sounds and colours come along for free.
///
/// Usage: open Assets/_Game/Scenes/MainMenu.unity, then
/// Tools > Medieval Survival > Build How To Play. Save the scene afterwards.
///
/// Re-runnable: it deletes the previous button/panel first, so tweak the Controls table
/// below and rebuild any time.
/// </summary>
public static class HowToPlayBuilder
{
    // Lifted from the Main Menu's Start/Settings/Quit buttons (see GameOverPolishBuilder).
    private static readonly Color Bronze = new Color(0.23529412f, 0.15686275f, 0.078431375f, 0.78431374f);
    private static readonly Color Gold = new Color(1f, 0.78431374f, 0f, 1f);
    private const string CinzelFontGuid = "c798f34c4405b4a02b7617ba5c0833d2";

    // action (left column)  ->  key/button (right column). Edit freely and rebuild.
    private static readonly (string action, string key)[] Controls =
    {
        ("Move",                 "W  A  S  D"),
        ("Look around",          "Mouse"),
        ("Sprint",               "Left Shift  (hold)"),
        ("Jump",                 "Space"),
        ("Attack",               "Left Mouse  /  E"),
        ("Block",                "Right Mouse  (hold)"),
        ("Use inventory item",   "1  -  9"),
        ("Pause",                "Esc"),
    };

    [MenuItem("Tools/Medieval Survival/Build How To Play")]
    public static void Build()
    {
        MainMenu mainMenu = Object.FindFirstObjectByType<MainMenu>();
        if (mainMenu == null)
        {
            EditorUtility.DisplayDialog("Build How To Play",
                "No MainMenu component in the open scene. Open Assets/_Game/Scenes/MainMenu.unity first.", "OK");
            return;
        }

        SerializedObject so = new SerializedObject(mainMenu);
        GameObject buttonsPanel = so.FindProperty("mainButtonsPanel").objectReferenceValue as GameObject;
        GameObject settingsPanel = so.FindProperty("settingsPanel").objectReferenceValue as GameObject;

        if (buttonsPanel == null || settingsPanel == null)
        {
            EditorUtility.DisplayDialog("Build How To Play",
                "MainMenu is missing its Main Buttons Panel / Settings Panel references.", "OK");
            return;
        }

        Transform uiParent = settingsPanel.transform.parent;
        TMP_FontAsset cinzel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            AssetDatabase.GUIDToAssetPath(CinzelFontGuid));
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // --- wipe any previous build so this is safe to re-run ---
        Transform oldPanel = uiParent.Find("HowToPlayPanel");
        if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);
        Transform oldButton = buttonsPanel.transform.Find("HowToPlayButton");
        if (oldButton != null) Object.DestroyImmediate(oldButton.gameObject);

        // --- the button, cloned from an existing menu button ---
        Button exemplar = FindExemplarButton(buttonsPanel.transform);
        if (exemplar == null)
        {
            EditorUtility.DisplayDialog("Build How To Play",
                "Couldn't find an existing button under the Main Buttons Panel to copy the style from.", "OK");
            return;
        }

        GameObject buttonGO = Object.Instantiate(exemplar.gameObject);
        buttonGO.name = "HowToPlayButton";
        buttonGO.transform.SetParent(buttonsPanel.transform, false);
        buttonGO.transform.SetSiblingIndex(exemplar.transform.GetSiblingIndex() + 1);
        SetButtonLabel(buttonGO, "How To Play", cinzel);
        RewireClick(buttonGO.GetComponent<Button>(), mainMenu.OpenHowToPlay);

        // --- the panel ---
        const float FrameW = 700f;
        const float FrameH = 620f;

        GameObject panel = NewRect("HowToPlayPanel", uiParent);
        Stretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);

        // Gold border = a slightly oversized plate behind the dark content plate.
        GameObject border = NewRect("Frame", panel.transform);
        RectTransform borderRT = border.GetComponent<RectTransform>();
        borderRT.anchorMin = borderRT.anchorMax = borderRT.pivot = new Vector2(0.5f, 0.5f);
        borderRT.sizeDelta = new Vector2(FrameW + 8f, FrameH + 8f);
        Image borderImg = border.AddComponent<Image>();
        if (uiSprite != null) { borderImg.sprite = uiSprite; borderImg.type = Image.Type.Sliced; }
        borderImg.color = Gold;

        GameObject content = NewRect("Content", border.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = contentRT.anchorMax = contentRT.pivot = new Vector2(0.5f, 0.5f);
        contentRT.sizeDelta = new Vector2(FrameW, FrameH);
        Image contentImg = content.AddComponent<Image>();
        if (uiSprite != null) { contentImg.sprite = uiSprite; contentImg.type = Image.Type.Sliced; }
        contentImg.color = new Color(0.11f, 0.08f, 0.05f, 0.98f);

        TextMeshProUGUI title = NewText("Title", content.transform, "How to Play", 46f, cinzel, Gold);
        title.alignment = TextAlignmentOptions.Center;
        title.enableWordWrapping = false;
        RectTransform titleRT = title.rectTransform;
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(-80f, 76f);
        titleRT.anchoredPosition = new Vector2(0f, -34f);

        GameObject rule = NewRect("Rule", content.transform);
        RectTransform ruleRT = rule.GetComponent<RectTransform>();
        ruleRT.anchorMin = ruleRT.anchorMax = ruleRT.pivot = new Vector2(0.5f, 1f);
        ruleRT.sizeDelta = new Vector2(560f, 2f);
        ruleRT.anchoredPosition = new Vector2(0f, -120f);
        rule.AddComponent<Image>().color = new Color(1f, 0.78431374f, 0f, 0.35f);

        const float RowH = 44f;
        GameObject list = NewRect("Controls", content.transform);
        RectTransform listRT = list.GetComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0.5f, 1f);
        listRT.anchorMax = new Vector2(0.5f, 1f);
        listRT.pivot = new Vector2(0.5f, 1f);
        listRT.sizeDelta = new Vector2(560f, RowH * Controls.Length);
        listRT.anchoredPosition = new Vector2(0f, -140f);
        VerticalLayoutGroup vlg = list.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 0f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        for (int i = 0; i < Controls.Length; i++)
            BuildRow(list.transform, Controls[i].action, Controls[i].key, cinzel, RowH, i);

        GameObject backGO = Object.Instantiate(buttonGO);
        backGO.name = "BackButton";
        backGO.transform.SetParent(content.transform, false);
        SetButtonLabel(backGO, "Back", cinzel);
        RectTransform backRT = backGO.GetComponent<RectTransform>();
        backRT.anchorMin = backRT.anchorMax = new Vector2(0.5f, 0f);
        backRT.pivot = new Vector2(0.5f, 0f);
        backRT.sizeDelta = new Vector2(240f, 58f);
        backRT.anchoredPosition = new Vector2(0f, 30f);
        RewireClick(backGO.GetComponent<Button>(), mainMenu.CloseHowToPlay);

        // --- wire it to MainMenu and hide it ---
        so.Update();
        so.FindProperty("howToPlayPanel").objectReferenceValue = panel;
        so.ApplyModifiedProperties();
        panel.SetActive(false);

        EditorUtility.SetDirty(mainMenu);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Build How To Play",
            "Added the How To Play button to the menu column and built its panel " +
            $"({Controls.Length} control rows) with a Back button. Save the scene.", "OK");
    }

    private static Button FindExemplarButton(Transform buttonsPanel)
    {
        foreach (string name in new[] { "SettingsButton", "StartButton", "QuitButton" })
        {
            Transform t = buttonsPanel.Find(name);
            if (t != null && t.GetComponent<Button>() != null)
                return t.GetComponent<Button>();
        }
        return buttonsPanel.GetComponentInChildren<Button>();
    }

    // A table row: action flush-left, key flush-right, split down the middle. Fixed height,
    // no per-row background - every row sits on the panel's own colour.
    private static void BuildRow(Transform parent, string action, string key, TMP_FontAsset font,
        float rowHeight, int index)
    {
        GameObject row = NewRect("Row", parent);
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.minHeight = rowHeight;
        rowLE.preferredHeight = rowHeight;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(28, 28, 0, 0);
        hlg.spacing = 24f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        TextMeshProUGUI actionText = NewText("Action", row.transform, action, 23f, font, Color.white);
        actionText.alignment = TextAlignmentOptions.MidlineLeft;
        actionText.enableWordWrapping = false;
        actionText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        TextMeshProUGUI keyText = NewText("Key", row.transform, key, 23f, font, Gold);
        keyText.alignment = TextAlignmentOptions.MidlineRight;
        keyText.enableWordWrapping = false;
        keyText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    // ---- tiny UGUI helpers ----

    private static GameObject NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        return go;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, string text, float size,
        TMP_FontAsset font, Color color)
    {
        GameObject go = NewRect(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.enableAutoSizing = false;
        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetButtonLabel(GameObject button, string text, TMP_FontAsset font)
    {
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null) return;
        label.text = text;
        if (font != null) label.font = font;
    }

    private static void RewireClick(Button button, UnityEngine.Events.UnityAction call)
    {
        if (button == null) return;
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        UnityEventTools.AddPersistentListener(button.onClick, call);
    }
}
