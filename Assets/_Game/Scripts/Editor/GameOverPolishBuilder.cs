using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reworks the Game Over / Victory screen: restyles Restart + Main Menu buttons to match the
/// Main Menu's bronze/gold Cinzel theme exactly, makes the background solid black, fixes the
/// title/score text that were overlapping at the same position, widens the Wave Complete text
/// so it never wraps to 2 lines, and lays everything out as a single centered vertical stack
/// dead in the middle of the screen.
///
/// Usage: open the gameplay scene (Assets/_Game/Scenes/Gameplay.unity), then
/// Tools > Medieval Survival > Polish Game Over Screen. Save the scene afterwards.
/// </summary>
public static class GameOverPolishBuilder
{
    // Exact values lifted from MainMenu.unity's Start/Settings/Quit buttons.
    private static readonly Color Bronze = new Color(0.23529412f, 0.15686275f, 0.078431375f, 0.78431374f);
    private static readonly Color Gold = new Color(1f, 0.78431374f, 0f, 1f);
    private static readonly Color GoldPressed = new Color(0.7058824f, 0.54901963f, 0f, 1f);
    private static readonly Color Disabled = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f);
    private const string CinzelFontGuid = "c798f34c4405b4a02b7617ba5c0833d2";

    [MenuItem("Tools/Medieval Survival/Polish Game Over Screen")]
    public static void Build()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Polish Game Over Screen", "No Canvas found in the open scene.", "OK");
            return;
        }

        Transform panel = canvas.transform.Find("GameOverPanel");
        if (panel == null)
        {
            EditorUtility.DisplayDialog("Polish Game Over Screen", "No GameOverPanel found under Canvas.", "OK");
            return;
        }

        // Solid black background instead of the old translucent one.
        Image panelBg = panel.GetComponent<Image>();
        if (panelBg != null) panelBg.color = new Color(0f, 0f, 0f, 0.92f);

        // Title and Final Score text were both anchored at the exact same position (0,0),
        // stacked directly on top of each other. Give them their own vertical slots.
        Transform title = panel.Find("GameOverText");
        Transform scoreText = panel.Find("GameOverScoreText");
        Transform restartButton = panel.Find("RestartButton");
        Transform mainMenuButton = panel.Find("MainMenuButton");

        if (title != null)
            SetAnchoredY(title.GetComponent<RectTransform>(), 170f);

        if (scoreText != null)
        {
            RectTransform scoreRT = scoreText.GetComponent<RectTransform>();
            SetAnchoredY(scoreRT, 70f);
            // Was accidentally sized to a NEGATIVE height (-99.9), which is why it looked broken.
            scoreRT.sizeDelta = new Vector2(400f, 60f);
        }

        if (restartButton != null)
        {
            StyleButton(restartButton, new Vector2(220f, 60f), new Vector2(0f, -30f));
            SetLabelText(restartButton, "Restart");
        }

        if (mainMenuButton != null)
        {
            StyleButton(mainMenuButton, new Vector2(220f, 60f), new Vector2(0f, -110f));
            SetLabelText(mainMenuButton, "Main Menu");
        }

        // "Wave X Complete!" was wrapping onto 2 lines in a box too narrow for it.
        Transform waveComplete = canvas.transform.Find("WaveCompleteText");
        if (waveComplete != null)
        {
            RectTransform waveRT = waveComplete.GetComponent<RectTransform>();
            waveRT.sizeDelta = new Vector2(760f, 70f);
            TextMeshProUGUI waveTMP = waveComplete.GetComponent<TextMeshProUGUI>();
            if (waveTMP != null)
            {
                waveTMP.enableWordWrapping = false;
                waveTMP.alignment = TextAlignmentOptions.Center;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Polish Game Over Screen",
            "Restyled Restart/Main Menu to match the Main Menu's bronze/gold theme, made the " +
            "background solid black, fixed the title/score text that were overlapping, widened " +
            "Wave Complete so it stays on one line, and centered everything as a single stack. " +
            "Save the scene.", "OK");
    }

    private static void SetAnchoredY(RectTransform rt, float y)
    {
        Vector2 pos = rt.anchoredPosition;
        pos.y = y;
        rt.anchoredPosition = pos;
    }

    private static void StyleButton(Transform buttonTransform, Vector2 size, Vector2 anchoredPos)
    {
        RectTransform rt = buttonTransform.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        Image img = buttonTransform.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = Color.white; // tint comes from the Button's ColorTint below, not the Image itself
        }

        Button btn = buttonTransform.GetComponent<Button>();
        if (btn != null)
        {
            btn.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = btn.colors;
            colors.normalColor = Bronze;
            colors.highlightedColor = Gold;
            colors.pressedColor = GoldPressed;
            colors.selectedColor = GoldPressed;
            colors.disabledColor = Disabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
        }
    }

    private static void SetLabelText(Transform buttonTransform, string text)
    {
        TextMeshProUGUI label = buttonTransform.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null) return;

        label.text = text;
        label.fontSize = 28f;
        label.color = Color.white;
        label.enableAutoSizing = false;

        TMP_FontAsset cinzel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(CinzelFontGuid));
        if (cinzel != null) label.font = cinzel;
    }
}
