using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// An Easy / Normal / Hard selector shown on the main-menu screen. The pick is stored in
/// <see cref="GameSettings"/> (persisted to PlayerPrefs); <see cref="GameManager"/> reads it
/// when the gameplay scene loads and scales the wave timing and enemy counts accordingly.
///
/// Builds its own small UI at runtime and adds itself to the menu scene on load, so there
/// is nothing to wire up in the menu scene.
/// </summary>
public class DifficultySelector : MonoBehaviour
{
    private static readonly Color Gold     = new Color(1f, 0.784f, 0f, 1f);
    private static readonly Color GoldEdge = new Color(0.62f, 0.5f, 0.22f, 0.9f);
    private static readonly Color DarkFill = new Color(0.11f, 0.08f, 0.05f, 0.92f);
    private static readonly Color DarkText = new Color(0.10f, 0.08f, 0.04f, 1f);

    private static readonly string[] Names = { "EASY", "NORMAL", "HARD" };

    private MainMenu mainMenu;
    private readonly Image[] fill = new Image[3];
    private readonly TextMeshProUGUI[] label = new TextMeshProUGUI[3];

    // Runs automatically once after every scene load, with no manual wiring needed - this is
    // what lets the selector appear on the menu without anyone dragging it into the scene.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // avoid double-subscribing if Bootstrap ever reruns
        SceneManager.sceneLoaded += OnSceneLoaded;
        TrySpawn();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TrySpawn();

    // Creates the selector only once, and only on the main menu (never during gameplay).
    private static void TrySpawn()
    {
        if (FindFirstObjectByType<DifficultySelector>() != null) return; // already exists
        if (FindFirstObjectByType<MainMenu>() == null) return; // menu screen only

        new GameObject("DifficultySelector").AddComponent<DifficultySelector>();
    }

    private void Start()
    {
        mainMenu = FindFirstObjectByType<MainMenu>();
        Build(); // construct the Easy/Normal/Hard buttons from scratch
        Highlight(GameSettings.Difficulty); // visually mark whichever difficulty is already saved
        Debug.Log($"[DifficultySelector] ready - current: {GameSettings.Difficulty}");
    }

    // Builds the entire difficulty-selector UI (canvas, heading, and the 3 buttons) purely in
    // code, since this component isn't placed in the scene by hand.
    private void Build()
    {
        TMP_FontAsset font = FindFont();

        GameObject canvasGO = new GameObject("DifficultyCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Row anchored to the bottom-centre of the screen.
        const float bw = 168f, bh = 50f, gap = 12f; // button width/height and the gap between buttons
        float totalW = bw * 3 + gap * 2; // total width of all 3 buttons plus the gaps, used to center the row

        GameObject row = NewRect("Row", canvasGO.transform);
        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0f);
        rrt.pivot = new Vector2(0.5f, 0f);
        rrt.anchoredPosition = new Vector2(0f, 44f);
        rrt.sizeDelta = new Vector2(totalW, bh + 34f);

        TextMeshProUGUI heading = NewText("Heading", row.transform, "DIFFICULTY", 19f, font, Gold);
        heading.alignment = TextAlignmentOptions.Center;
        heading.characterSpacing = 7f;
        RectTransform hrt = heading.rectTransform;
        hrt.anchorMin = new Vector2(0f, 1f); hrt.anchorMax = new Vector2(1f, 1f); hrt.pivot = new Vector2(0.5f, 1f);
        hrt.sizeDelta = new Vector2(0f, 24f);
        hrt.anchoredPosition = Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
            GameObject frame = NewRect("Btn_" + Names[i], row.transform);
            RectTransform frt = frame.GetComponent<RectTransform>();
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0f);
            frt.pivot = new Vector2(0.5f, 0f);
            frt.sizeDelta = new Vector2(bw, bh);
            frt.anchoredPosition = new Vector2(-totalW * 0.5f + bw * 0.5f + i * (bw + gap), 0f);

            Image edge = frame.AddComponent<Image>();
            edge.color = GoldEdge;

            Button btn = frame.AddComponent<Button>();
            btn.targetGraphic = edge;
            btn.transition = Selectable.Transition.None; // no built-in color/scale tween, we handle highlighting ourselves
            GameDifficulty choice = (GameDifficulty)i; // capture this loop's index for the click callback below
            btn.onClick.AddListener(() => Choose(choice));

            GameObject inner = NewRect("Fill", frame.transform);
            RectTransform irt = inner.GetComponent<RectTransform>();
            Stretch(irt);
            irt.offsetMin = new Vector2(2f, 2f);
            irt.offsetMax = new Vector2(-2f, -2f);
            Image f = inner.AddComponent<Image>();
            f.color = DarkFill;
            f.raycastTarget = false;
            fill[i] = f;

            TextMeshProUGUI lbl = NewText("Label", inner.transform, Names[i], 20f, font, Gold);
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.characterSpacing = 2f;
            Stretch(lbl.rectTransform);
            label[i] = lbl;
        }
    }

    // Called when a difficulty button is clicked - saves the choice and updates the visuals.
    private void Choose(GameDifficulty d)
    {
        GameSettings.SetDifficulty(d); // persists to PlayerPrefs so GameManager can read it later
        Highlight(d);
        if (mainMenu != null) mainMenu.PlayClickSound();
        Debug.Log($"[DifficultySelector] difficulty -> {d}");
    }

    // Recolors the three buttons so only the selected difficulty appears "filled in".
    private void Highlight(GameDifficulty d)
    {
        for (int i = 0; i < 3; i++)
        {
            bool on = i == (int)d; // is this the currently selected button?
            if (fill[i] != null) fill[i].color = on ? Gold : DarkFill;
            if (label[i] != null) label[i].color = on ? DarkText : Gold;
        }
    }

    // ---- helpers ----

    // Reuses whatever font the rest of the menu's text is already using, so this runtime-built
    // UI visually matches the hand-designed menu instead of falling back to a generic font.
    private static TMP_FontAsset FindFont()
    {
        foreach (TMP_Text t in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            if (t.font != null) return t.font;
        return TMP_Settings.defaultFontAsset;
    }

    // Creates an empty UI GameObject with just a RectTransform, parented under the given transform.
    private static GameObject NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    // Creates a styled TextMeshPro label as a child of the given transform.
    private static TextMeshProUGUI NewText(string name, Transform parent, string text, float size,
        TMP_FontAsset font, Color color)
    {
        GameObject go = NewRect(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false; // purely decorative text, shouldn't block clicks on the button behind it
        tmp.enableWordWrapping = false;
        return tmp;
    }

    // Expands a RectTransform to fill its entire parent (all anchors/offsets at the edges).
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
