using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// The knight's field-pack inventory - a two-pane panel styled to match the game's menus
/// (bronze / gold / parchment, semi-transparent):
///   - Walk near a power-up and press  F  to store it instead of using it on touch.
///     (While the game runs, the normal walk-over instant pickup is disabled so F is
///     the only way to collect one.)
///   - Press  I  or  Tab  to open / close. The game does NOT pause - you keep moving.
///     It can't be opened while the game is paused or in the pause menu.
///   - Press  1 - 0  to select a slot; the right pane shows that item's record
///     (a live 3-D preview of the real pickup plus a description).
///   - Press  E  to use the selected item.
///
/// Every pickup takes its own slot - duplicates never stack. 10 slots.
///
/// The slot icons are live renders of the actual HealthPickup / StrongAttackPickup
/// prefab shapes (red cross, gold flanged mace), rebuilt in code from their prefab
/// dimensions and filmed by tiny off-screen cameras.
///
/// Self-contained: it builds its own Canvas and adds itself to the gameplay scene on
/// load, so there is nothing to wire up in the Inspector.
/// </summary>
public class Inventory : MonoBehaviour
{
    private const int SlotCount = 10;
    private const int Columns = 5;
    private const float PickupRange = 3.5f;
    private const int PreviewLayer = 31;

    // ---- palette (lifted from HowToPlayBuilder / the Settings panel) ----
    private static readonly Color Gold        = new Color(1f, 0.784f, 0f, 1f);
    private static readonly Color GoldFaint   = new Color(0.55f, 0.45f, 0.22f, 0.55f);
    private static readonly Color PanelBg     = new Color(0.11f, 0.08f, 0.05f, 0.90f);
    private static readonly Color InsetBg     = new Color(0.06f, 0.05f, 0.04f, 0.85f);
    private static readonly Color SlotBg      = new Color(0.09f, 0.08f, 0.06f, 0.80f);
    private static readonly Color SlotBgSel   = new Color(0.17f, 0.14f, 0.09f, 0.95f);
    private static readonly Color Teal        = new Color(0.32f, 0.72f, 0.66f, 1f);
    private static readonly Color Parchment   = new Color(0.90f, 0.85f, 0.72f, 1f);
    private static readonly Color DimText     = new Color(0.62f, 0.58f, 0.48f, 1f);
    private static readonly Color EmptySlotNo = new Color(0.45f, 0.42f, 0.35f, 0.7f);
    private static readonly Color UseGreen    = new Color(0.20f, 0.42f, 0.34f, 1f);
    private static readonly Color UseGreenOff = new Color(0.18f, 0.18f, 0.16f, 0.9f);

    // ---- model: one slot holds at most one item, no stacking ----
    // Plain data holder for a single inventory slot's contents (or emptiness).
    private class Slot
    {
        public bool used;              // false = slot is empty
        public PowerUpType type;       // which kind of power-up this slot holds
        public float multiplier;       // damage multiplier, only meaningful for StrongAttack
        public float duration;         // buff duration in seconds, only meaningful for StrongAttack
        public bool Empty => !used;
        public string Name => type == PowerUpType.Health ? "Healing Sigil" : "Gilded Mace";
        public string Description => type == PowerUpType.Health
            ? "A blood-red field cross blessed by the village healer. Shatters on use and knits the knight back to full health in an instant."
            : $"A gold flanged war-mace. Heft it and the next blows fall like a portcullis - x{multiplier:0.#} weapon damage for {duration:0} seconds.";
    }

    private readonly Slot[] slots = new Slot[SlotCount];
    private int selected;

    // ---- refs ----
    private Transform player;
    private PlayerHealth playerHealth;
    private PlayerCombat playerCombat;
    private PauseMenu pauseMenu;
    private bool combatWasEnabled = true;
    private bool pauseWasEnabled = true;
    private bool wasGameOver;
    private PowerUp nearest;
    private bool open;

    // ---- ui ----
    private GameObject panel;
    private TextMeshProUGUI headerCount;
    private readonly Image[] cellFrame = new Image[SlotCount];
    private readonly Image[] cellBg = new Image[SlotCount];
    private readonly RawImage[] cellIcon = new RawImage[SlotCount];
    private readonly TextMeshProUGUI[] cellNo = new TextMeshProUGUI[SlotCount];
    private readonly TextMeshProUGUI[] cellBadge = new TextMeshProUGUI[SlotCount];

    private RawImage recordIcon;
    private TextMeshProUGUI recordName, recordDesc;
    private Image useButtonBg;
    private TextMeshProUGUI useButtonText;

    private GameObject promptRoot;
    private TextMeshProUGUI promptText;

    // ---- live prefab previews ----
    private RenderTexture healthRT, maceRT;
    private Transform healthModel, maceModel;
    private GameObject previewStage;

    // ---- reflection into PowerUp's private serialized fields ----
    private static readonly FieldInfo FType =
        typeof(PowerUp).GetField("type", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo FMultiplier =
        typeof(PowerUp).GetField("damageMultiplier", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo FDuration =
        typeof(PowerUp).GetField("buffDuration", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo FPickupSound =
        typeof(PowerUp).GetField("pickupSound", BindingFlags.NonPublic | BindingFlags.Instance);

    // Runs automatically once after every scene load, with no manual wiring needed.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // AfterSceneLoad only fires for the first scene. Re-check on every scene load
        // so it still appears when you reach Gameplay via the main menu's Play button.
        SceneManager.sceneLoaded -= OnSceneLoaded; // avoid double-subscribing if Bootstrap ever reruns
        SceneManager.sceneLoaded += OnSceneLoaded;
        TrySpawn(); // the scene that's already loaded (e.g. pressing Play in Gameplay directly)
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TrySpawn();

    // Creates the Inventory only once, and only in a gameplay scene (one with a tagged Player).
    private static void TrySpawn()
    {
        if (FindObjectOfType<Inventory>() != null) return; // already exists
        if (GameObject.FindGameObjectWithTag("Player") == null) return; // not a gameplay scene

        new GameObject("Inventory").AddComponent<Inventory>();
        Debug.Log("[Inventory] Ready.  F = pick up  ·  I / Tab = open  ·  1-0 = select  ·  E = use.");
    }

    // Sets up empty slots, finds the player and related components, then builds the preview
    // render textures and the UI itself.
    private void Start()
    {
        for (int i = 0; i < SlotCount; i++) slots[i] = new Slot();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            playerCombat = playerObj.GetComponent<PlayerCombat>();
        }
        pauseMenu = FindObjectOfType<PauseMenu>();

        BuildPreviews();
        BuildUI();
        Refresh();
    }

    // Handles all inventory input every frame: open/close, pick up nearby items, and (while
    // open) select a slot or use the selected item.
    private void Update()
    {
        if (player == null || Keyboard.current == null) return;

        // Wipe the pack whenever the game-over state flips (on death, and again on restart -
        // RestartGame() resets in place without reloading the scene, so nothing else clears us).
        bool gameOver = GameManager.Instance != null && GameManager.Instance.isGameOver;
        if (gameOver != wasGameOver)
        {
            wasGameOver = gameOver;
            ClearInventory();
            if (playerCombat != null) playerCombat.ClearDamageBuff();
            if (open) SetOpen(false);
        }

        // Mute the normal walk-over auto-pickup while the test runs.
        foreach (PowerUp pu in FindObjectsOfType<PowerUp>())
        {
            Collider col = pu.GetComponent<Collider>();
            if (col != null && col.isTrigger) col.isTrigger = false;
        }

        nearest = FindNearestPowerUp();
        UpdatePrompt();

        if (Keyboard.current.iKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (open) SetOpen(false);
            else if (CanOpen()) SetOpen(true);
        }

        if (!open)
        {
            if (nearest != null && Keyboard.current.fKey.wasPressedThisFrame)
                Collect(nearest);
            return;
        }

        // ---- open: number keys select a slot, E uses it ----
        for (int i = 0; i < SlotCount; i++)
        {
            Key k = i < 9 ? (Key)((int)Key.Digit1 + i) : Key.Digit0; // 1..9 then 0
            if (Keyboard.current[k].wasPressedThisFrame && selected != i)
            {
                selected = i;
                Refresh();
            }
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
            UseSlot(selected);
    }

    // Continuously spins the two preview models so their render-texture icons look alive.
    private void LateUpdate()
    {
        // Slow spin so the previews read as real 3-D pickups.
        float spin = Time.time * 42f;
        if (healthModel != null) healthModel.localRotation = Quaternion.Euler(16f, spin, 0f);
        if (maceModel != null) maceModel.localRotation = Quaternion.Euler(16f, spin, 0f);
    }

    /// <summary>The inventory can't be opened while the game is paused or in the pause menu.</summary>
    private bool CanOpen()
    {
        if (Time.timeScale == 0f) return false;
        if (pauseMenu != null && pauseMenu.IsPaused) return false;
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return false;
        return true;
    }

    // ---- inventory logic ----

    // Finds the closest PowerUp within PickupRange, or null if none are close enough.
    private PowerUp FindNearestPowerUp()
    {
        PowerUp best = null;
        float bestDist = PickupRange;
        foreach (PowerUp pu in FindObjectsOfType<PowerUp>())
        {
            float d = Vector3.Distance(player.position, pu.transform.position);
            if (d <= bestDist) { bestDist = d; best = pu; } // closer than anything found so far
        }
        return best;
    }

    // Moves a world power-up into the first free inventory slot and removes it from the level.
    // Uses reflection to read PowerUp's private fields since those weren't designed to be
    // read externally - this keeps PowerUp's own API simple for its normal walk-over use case.
    private void Collect(PowerUp pu)
    {
        // No stacking - every pickup takes the next free slot, duplicates included.
        Slot target = System.Array.Find(slots, s => s.Empty);
        if (target == null)
        {
            Debug.Log("[Inventory] Full - can't pick that up.");
            if (promptText != null) promptText.text = "Pack is full";
            return;
        }

        target.used = true;
        target.type = FType != null ? (PowerUpType)FType.GetValue(pu) : PowerUpType.Health;
        target.multiplier = FMultiplier != null ? (float)FMultiplier.GetValue(pu) : 2f;
        target.duration = FDuration != null ? (float)FDuration.GetValue(pu) : 10f;

        // Play the power-up's own pickup sound (the walk-over path that normally plays it
        // is disabled while this test runs).
        AudioClip snd = FPickupSound != null ? FPickupSound.GetValue(pu) as AudioClip : null;
        if (snd != null)
            AudioSource.PlayClipAtPoint(snd, pu.transform.position, GameSettings.SfxVolume);

        Debug.Log($"[Inventory] Picked up {target.Name}.");
        Destroy(pu.gameObject); // remove the world pickup now that it's stored
        Refresh();
    }

    // Empties every slot - called on death/restart so items don't carry over between runs.
    private void ClearInventory()
    {
        foreach (Slot s in slots) s.used = false;
        selected = 0;
        Refresh();
    }

    // Applies the effect of whichever item is in the given slot, then empties that slot.
    private void UseSlot(int index)
    {
        Slot s = slots[index];
        if (s.Empty) return;

        string message;
        switch (s.type)
        {
            case PowerUpType.Health:
                if (playerHealth != null) playerHealth.FullHeal();
                message = "Healing Sigil used  -  Full Health!";
                break;
            case PowerUpType.StrongAttack:
                if (playerCombat != null) playerCombat.ApplyDamageBuff(s.multiplier, s.duration);
                message = $"Gilded Mace used  -  x{s.multiplier:0.#} damage for {s.duration:0}s!";
                break;
            default:
                message = "Item used";
                break;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowPickupMessage(message);

        Debug.Log($"[Inventory] Used {s.Name} from slot {index + 1}.");
        s.used = false;
        Refresh();
    }

    // ---- open / close ----

    // Shows/hides the inventory panel, and temporarily disables attacking and pausing while
    // it's open (E is shared between "attack" and "use item", so they can't both be live).
    private void SetOpen(bool value)
    {
        open = value;
        if (panel != null) panel.SetActive(open);

        if (open)
        {
            // Free the E key (mute attack) and block the pause menu while browsing.
            if (playerCombat != null) { combatWasEnabled = playerCombat.enabled; playerCombat.enabled = false; }
            if (pauseMenu != null)    { pauseWasEnabled = pauseMenu.enabled;   pauseMenu.enabled = false; }
        }
        else
        {
            if (playerCombat != null) playerCombat.enabled = combatWasEnabled;
            if (pauseMenu != null)    pauseMenu.enabled = pauseWasEnabled;
        }

        if (open) Refresh();
    }

    // Shows/updates the "[F] Pick up ..." prompt when standing near a collectible power-up.
    private void UpdatePrompt()
    {
        bool show = !open && nearest != null;
        if (promptRoot != null && promptRoot.activeSelf != show)
            promptRoot.SetActive(show);

        if (show && promptText != null)
        {
            PowerUpType t = FType != null ? (PowerUpType)FType.GetValue(nearest) : PowerUpType.Health;
            promptText.text = $"<color=#FFC800>[ F ]</color>   Pick up  {(t == PowerUpType.Health ? "Healing Sigil" : "Gilded Mace")}";
        }
    }

    // ---- ui refresh ----

    // Redraws every slot cell (frame/background color, icon, empty-slot number, damage badge)
    // and the header's used-slot count, then refreshes the selected item's detail panel.
    private void Refresh()
    {
        int used = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            Slot s = slots[i];
            if (!s.Empty) used++;
            bool sel = i == selected;

            if (cellFrame[i] != null) cellFrame[i].color = sel ? Gold : GoldFaint;
            if (cellBg[i] != null) cellBg[i].color = sel ? SlotBgSel : SlotBg;

            if (cellIcon[i] != null)
            {
                cellIcon[i].enabled = !s.Empty;
                cellIcon[i].texture = s.Empty ? null
                    : (s.type == PowerUpType.Health ? healthRT : maceRT);
            }
            if (cellNo[i] != null)
            {
                cellNo[i].enabled = s.Empty;
                cellNo[i].text = (i + 1).ToString("00");
            }
            if (cellBadge[i] != null)
            {
                bool showMul = !s.Empty && s.type == PowerUpType.StrongAttack;
                cellBadge[i].enabled = showMul;
                if (showMul) cellBadge[i].text = $"x{s.multiplier:0.#}";
            }
        }

        if (headerCount != null)
            headerCount.text = $"<color=#{ColorHex(DimText)}>Slots</color>  {used} / {SlotCount}";

        UpdateRecord();
    }

    // Updates the right-hand "item record" panel to show the currently selected slot's
    // icon, name and description (or an "empty slot" message).
    private void UpdateRecord()
    {
        Slot s = slots[selected];

        if (s.Empty)
        {
            if (recordIcon != null) recordIcon.enabled = false;
            if (recordName != null) recordName.text = "Empty Slot";
            if (recordDesc != null)
                recordDesc.text = "<color=#8a8578>Nothing in this slot. Kill an enemy or find a power-up, then press F to store it.</color>";
            SetUseButton(false, "NOTHING TO USE");
            return;
        }

        if (recordIcon != null)
        {
            recordIcon.enabled = true;
            recordIcon.texture = s.type == PowerUpType.Health ? healthRT : maceRT;
        }
        if (recordName != null) recordName.text = s.Name;
        if (recordDesc != null) recordDesc.text = s.Description;
        SetUseButton(true, "USE          E");
    }

    // Enables/disables the "USE" button's active look depending on whether the selected slot has an item.
    private void SetUseButton(bool on, string label)
    {
        if (useButtonBg != null) useButtonBg.color = on ? UseGreen : UseGreenOff;
        if (useButtonText != null)
        {
            useButtonText.text = label;
            useButtonText.color = on ? Parchment : DimText;
        }
    }

    // ================================================================
    //  live 3-D previews of the real pickup prefabs
    // ================================================================

    // Builds two tiny hand-made 3-D models (a red cross for Health, a gold mace for Strong
    // Attack) far away from the real level, each filmed by its own mini camera into a
    // RenderTexture that's used as that item's slot icon.
    private void BuildPreviews()
    {
        previewStage = new GameObject("InventoryPreviewStage");
        previewStage.transform.SetParent(transform, false);
        previewStage.transform.position = new Vector3(4000f, -3000f, 4000f); // far from the real level

        GameObject lightGO = new GameObject("Light");
        lightGO.transform.SetParent(previewStage.transform, false);
        lightGO.transform.localPosition = new Vector3(1.4f, 2.2f, -2.6f);
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 40f;
        l.intensity = 2.6f;
        l.color = new Color(1f, 0.96f, 0.9f);
        l.cullingMask = 1 << PreviewLayer;

        // HEALTH: red cross - two crossed bars (from HealthPickup.prefab)
        healthModel = BuildModel("HealthModel", new Vector3(-2f, 0f, 0f),
            new Color(0.85f, 0.15f, 0.15f), new Color(0.5f, 0.10f, 0.10f), new[]
        {
            (PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.18f, 0.55f, 0.18f), Vector3.zero),
            (PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.55f, 0.18f, 0.18f), Vector3.zero),
        });
        healthRT = MakeRT();
        MakePreviewCamera("HealthCam", healthModel.parent.position + healthModel.localPosition, healthRT);

        // STRONG ATTACK: gold flanged mace - shaft + round head + crossed flanges + spike
        // (matches the reshaped StrongAttackPickup.prefab).
        maceModel = BuildModel("MaceModel", new Vector3(2f, 0f, 0f),
            new Color(1f, 0.80f, 0.14f), new Color(0.55f, 0.42f, 0f), new[]
        {
            (PrimitiveType.Cube,   new Vector3(0f, -0.06f, 0f), new Vector3(0.055f, 0.66f, 0.055f), Vector3.zero),   // shaft
            (PrimitiveType.Sphere, new Vector3(0f,  0.30f, 0f), new Vector3(0.30f, 0.30f, 0.30f),   Vector3.zero),   // head
            (PrimitiveType.Cube,   new Vector3(0f,  0.30f, 0f), new Vector3(0.46f, 0.11f, 0.11f),   new Vector3(0f, 0f, 45f)),  // flange
            (PrimitiveType.Cube,   new Vector3(0f,  0.30f, 0f), new Vector3(0.11f, 0.46f, 0.11f),   new Vector3(0f, 0f, 45f)),  // flange (crossed)
            (PrimitiveType.Cube,   new Vector3(0f,  0.30f, 0.28f), new Vector3(0.09f, 0.09f, 0.24f), Vector3.zero),  // front spike
        });
        maceRT = MakeRT();
        MakePreviewCamera("MaceCam", maceModel.parent.position + maceModel.localPosition, maceRT);
    }

    // Assembles a simple model out of primitive shapes (cubes/spheres), each positioned,
    // scaled and rotated as described in `parts`, all sharing one colored/emissive material.
    private Transform BuildModel(string name, Vector3 localPos, Color baseCol, Color emis,
        (PrimitiveType prim, Vector3 pos, Vector3 scale, Vector3 euler)[] parts)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(previewStage.transform, false);
        root.transform.localPosition = localPos;

        Material mat = MakeLitMaterial(baseCol, emis);
        foreach ((PrimitiveType prim, Vector3 pos, Vector3 scale, Vector3 euler) in parts)
        {
            GameObject piece = GameObject.CreatePrimitive(prim);
            piece.transform.SetParent(root.transform, false);
            piece.transform.localPosition = pos;
            piece.transform.localScale = scale;
            piece.transform.localEulerAngles = euler;
            Collider c = piece.GetComponent<Collider>();
            if (c != null) Destroy(c);
            piece.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
        SetLayer(root, PreviewLayer);
        return root.transform;
    }

    // Creates a small orthographic camera pointed at a preview model, rendering only the
    // preview layer into the given RenderTexture (with a transparent background).
    private void MakePreviewCamera(string name, Vector3 lookAtWorldPos, RenderTexture target)
    {
        GameObject camGO = new GameObject(name);
        camGO.transform.SetParent(previewStage.transform, false);
        camGO.transform.position = lookAtWorldPos + new Vector3(0f, 0.05f, -2.8f);
        camGO.transform.LookAt(lookAtWorldPos + new Vector3(0f, 0.02f, 0f));

        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 0.62f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 12f;
        cam.cullingMask = 1 << PreviewLayer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.allowHDR = false;
        cam.allowMSAA = false;
        cam.targetTexture = target;
    }

    // Creates a small transparent RenderTexture used as a slot icon's live preview surface.
    private static RenderTexture MakeRT()
    {
        RenderTexture rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 2,
            filterMode = FilterMode.Bilinear
        };
        rt.Create();
        return rt;
    }

    // Creates a material with a base color and a glowing emission color, trying URP's Lit
    // shader first and falling back to older/simpler shaders if it isn't available.
    private static Material MakeLitMaterial(Color baseCol, Color emis)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Standard")
                        ?? Shader.Find("Unlit/Color");
        Material m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseCol);
        if (m.HasProperty("_Color")) m.SetColor("_Color", baseCol);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.25f);
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.SetColor("_EmissionColor", emis);
        }
        return m;
    }

    // Recursively sets this GameObject and every child to the given layer - used so the
    // preview models only get rendered by their own dedicated preview camera.
    private static void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayer(child.gameObject, layer);
    }

    // ================================================================
    //  ui construction
    // ================================================================

    // Builds the entire inventory UI (pickup prompt, main panel, slot grid, item record pane,
    // and footer) purely in code, since this component isn't placed in the scene by hand.
    // The layout numbers below are hand-tuned pixel positions/sizes, not derived from anything.
    private void BuildUI()
    {
        TMP_FontAsset font = FindFont();

        GameObject canvasGO = new GameObject("InventoryCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ---------- pickup prompt (bottom centre) ----------
        promptRoot = NewImage("Prompt", canvasGO.transform, new Color(0f, 0f, 0f, 0.62f));
        RectTransform pr = promptRoot.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0f);
        pr.pivot = new Vector2(0.5f, 0f);
        pr.anchoredPosition = new Vector2(0f, 150f);
        pr.sizeDelta = new Vector2(460f, 52f);
        Outline(promptRoot, GoldFaint);
        promptText = NewTextChild("Text", promptRoot.transform, "[ F ]", 22f, font, Parchment, TextAlignmentOptions.Center);
        Stretch(promptText.rectTransform);
        promptRoot.SetActive(false);

        // ---------- main panel ----------
        const float W = 1040f, H = 600f;
        panel = NewImage("Panel", canvasGO.transform, PanelBg);
        RectTransform panRT = panel.GetComponent<RectTransform>();
        panRT.anchorMin = panRT.anchorMax = panRT.pivot = new Vector2(0.5f, 0.5f);
        panRT.sizeDelta = new Vector2(W, H);
        Outline(panel, Gold, 3f);

        // header (title only - subtitle left blank on purpose)
        TextMeshProUGUI title = PanelText("Title", "THE KNIGHT'S  SATCHEL", 30f, font, Gold, TextAlignmentOptions.Left,
            32f, 26f, 620f, 40f);
        title.characterSpacing = 4f;
        headerCount = PanelText("HeaderCount", "", 16f, font, Parchment, TextAlignmentOptions.Right,
            W - 32f - 320f, 34f, 320f, 22f);

        // divider
        GameObject rule = NewImage("Rule", panel.transform, new Color(Gold.r, Gold.g, Gold.b, 0.35f));
        PlaceTL(rule.GetComponent<RectTransform>(), 32f, 84f, W - 64f, 2f);

        // ---------- slot grid ----------
        const float gridX = 32f, gridY = 200f, cell = 116f, gap = 12f;
        for (int i = 0; i < SlotCount; i++)
        {
            int c = i % Columns, r = i / Columns;
            float x = gridX + c * (cell + gap);
            float y = gridY + r * (cell + gap);

            Image frame = NewImage("Cell" + i, panel.transform, GoldFaint).GetComponent<Image>();
            PlaceTL(frame.rectTransform, x, y, cell, cell);
            cellFrame[i] = frame;

            Image bg = NewImage("Bg", frame.transform, SlotBg).GetComponent<Image>();
            Stretch(bg.rectTransform);
            bg.rectTransform.offsetMin = new Vector2(2f, 2f);
            bg.rectTransform.offsetMax = new Vector2(-2f, -2f);
            cellBg[i] = bg;

            RawImage icon = NewRawImage("Icon", bg.transform);
            RectTransform iconRT = icon.rectTransform;
            iconRT.anchorMin = iconRT.anchorMax = iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.sizeDelta = new Vector2(cell - 22f, cell - 22f);
            cellIcon[i] = icon;

            cellNo[i] = NewTextChild("No", bg.transform, "", 24f, font, EmptySlotNo, TextAlignmentOptions.Center);
            Stretch(cellNo[i].rectTransform);

            // "x2" damage-multiplier badge, bottom-right, shown only for the mace
            cellBadge[i] = NewTextChild("Badge", bg.transform, "", 17f, font,
                new Color(1f, 0.86f, 0.36f), TextAlignmentOptions.BottomRight);
            cellBadge[i].fontStyle = FontStyles.Bold;
            Stretch(cellBadge[i].rectTransform);
            cellBadge[i].rectTransform.offsetMin = new Vector2(0f, 6f);
            cellBadge[i].rectTransform.offsetMax = new Vector2(-8f, 0f);
        }

        // ---------- vertical divider ----------
        float splitX = gridX + Columns * (cell + gap) + 8f;
        GameObject vrule = NewImage("VRule", panel.transform, new Color(Gold.r, Gold.g, Gold.b, 0.28f));
        PlaceTL(vrule.GetComponent<RectTransform>(), splitX, 104f, 2f, H - 104f - 40f);

        // ---------- right pane: item record ----------
        float rx = splitX + 22f;
        float rw = W - rx - 30f;

        PanelText("RecordLabel", "ITEM  RECORD", 15f, font, Teal, TextAlignmentOptions.Left, rx, 108f, rw, 20f)
            .characterSpacing = 3f;

        GameObject recBox = NewImage("RecordBox", panel.transform, InsetBg);
        PlaceTL(recBox.GetComponent<RectTransform>(), rx, 136f, rw, 170f);
        Outline(recBox, new Color(Teal.r, Teal.g, Teal.b, 0.35f));

        recordIcon = NewRawImage("RecordIcon", recBox.transform);
        RectTransform riRT = recordIcon.rectTransform;
        riRT.anchorMin = riRT.anchorMax = riRT.pivot = new Vector2(0.5f, 0.5f);
        riRT.sizeDelta = new Vector2(150f, 150f);

        recordName = PanelText("RecordName", "", 27f, font, Gold, TextAlignmentOptions.Left, rx, 324f, rw, 38f);
        recordDesc = PanelText("RecordDesc", "", 16f, font, Parchment, TextAlignmentOptions.TopLeft, rx, 372f, rw, 150f);
        recordDesc.enableWordWrapping = true;

        useButtonBg = NewImage("UseButton", panel.transform, UseGreen).GetComponent<Image>();
        PlaceTL(useButtonBg.rectTransform, rx, 526f, rw, 36f);
        Outline(useButtonBg.gameObject, new Color(1f, 1f, 1f, 0.15f));
        useButtonText = NewTextChild("Label", useButtonBg.transform, "USE          E", 17f, font, Parchment,
            TextAlignmentOptions.Center);
        Stretch(useButtonText.rectTransform);
        useButtonText.characterSpacing = 2f;

        // ---------- footer ----------
        PanelText("Footer", "1 - 0  select        E  use        I / Tab  close", 14f, font, DimText,
            TextAlignmentOptions.Left, 32f, H - 30f, W - 64f, 20f);

        panel.SetActive(false);
    }

    // ---- tiny uGUI helpers ----

    // Reuses whatever font the rest of the scene's text is already using, so this runtime-built
    // UI visually matches the hand-designed menus instead of falling back to a generic font.
    private static TMP_FontAsset FindFont()
    {
        foreach (TMP_Text t in FindObjectsOfType<TMP_Text>())
            if (t.font != null) return t.font;
        if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset;
        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    // Creates a plain colored UI panel/box as a child of the given transform.
    private static GameObject NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return go;
    }

    // Creates a RawImage (used to display a RenderTexture preview) as a child of the given transform.
    private static RawImage NewRawImage(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RawImage ri = go.AddComponent<RawImage>();
        ri.raycastTarget = false; // purely decorative, shouldn't block clicks
        return ri;
    }

    // Creates a styled TextMeshPro label as a child of the given transform.
    private static TextMeshProUGUI NewTextChild(string name, Transform parent, string text, float size,
        TMP_FontAsset font, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.richText = true;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    // Shortcut for creating a text label directly inside the main panel, placed at a
    // top-left-anchored position/size.
    private TextMeshProUGUI PanelText(string name, string text, float size, TMP_FontAsset font, Color color,
        TextAlignmentOptions align, float x, float y, float w, float h)
    {
        TextMeshProUGUI t = NewTextChild(name, panel.transform, text, size, font, color, align);
        PlaceTL(t.rectTransform, x, y, w, h);
        return t;
    }

    // Anchor a rect to its parent's top-left corner and offset it (x right, y down).
    private static void PlaceTL(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, -y);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // A cheap "border": four gold edge strips just outside the target rect.
    private static void Outline(GameObject target, Color color, float thickness = 2f)
    {
        RectTransform t = target.GetComponent<RectTransform>();
        for (int i = 0; i < 4; i++)
        {
            GameObject edge = new GameObject("Edge" + i, typeof(RectTransform));
            edge.transform.SetParent(t, false);
            edge.transform.SetAsFirstSibling();
            edge.AddComponent<Image>().color = color;
            RectTransform e = edge.GetComponent<RectTransform>();
            switch (i)
            {
                case 0: e.anchorMin = new Vector2(0, 1); e.anchorMax = new Vector2(1, 1);
                        e.pivot = new Vector2(0.5f, 1); e.sizeDelta = new Vector2(thickness * 2, thickness);
                        e.anchoredPosition = new Vector2(0, thickness); break;
                case 1: e.anchorMin = new Vector2(0, 0); e.anchorMax = new Vector2(1, 0);
                        e.pivot = new Vector2(0.5f, 0); e.sizeDelta = new Vector2(thickness * 2, thickness);
                        e.anchoredPosition = new Vector2(0, -thickness); break;
                case 2: e.anchorMin = new Vector2(0, 0); e.anchorMax = new Vector2(0, 1);
                        e.pivot = new Vector2(0, 0.5f); e.sizeDelta = new Vector2(thickness, thickness * 2);
                        e.anchoredPosition = new Vector2(-thickness, 0); break;
                case 3: e.anchorMin = new Vector2(1, 0); e.anchorMax = new Vector2(1, 1);
                        e.pivot = new Vector2(1, 0.5f); e.sizeDelta = new Vector2(thickness, thickness * 2);
                        e.anchoredPosition = new Vector2(thickness, 0); break;
            }
        }
    }

    // Converts a Color to a "RRGGBB" hex string for embedding in TextMeshPro rich-text tags.
    private static string ColorHex(Color c) => ColorUtility.ToHtmlStringRGB(c);

    // Cleanup when this object is destroyed (e.g. scene unload): restore whatever we disabled
    // while the inventory was open, and release the RenderTextures/preview stage so they
    // don't leak memory.
    private void OnDestroy()
    {
        if (open)
        {
            if (playerCombat != null) playerCombat.enabled = combatWasEnabled;
            if (pauseMenu != null) pauseMenu.enabled = pauseWasEnabled;
        }
        if (healthRT != null) healthRT.Release();
        if (maceRT != null) maceRT.Release();
        if (previewStage != null) Destroy(previewStage);
    }
}
