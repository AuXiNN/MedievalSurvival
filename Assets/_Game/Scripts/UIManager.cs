using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Health Bar")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFill;

    [Header("Score & Wave")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI waveCompleteText;

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverTitleText;
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Strong Attack Timer")]
    [SerializeField] private GameObject strongAttackTimerRoot;
    [SerializeField] private Slider strongAttackTimerSlider;

    [Header("Pickup Message")]
    [SerializeField] private TextMeshProUGUI pickupMessageText;

    // Sets up the singleton so any script can reach the HUD via UIManager.Instance.
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject); // enforce a single UIManager - destroy any duplicate
    }

    // Hides every panel that should start closed, wires up the end-screen buttons, and
    // applies the HUD's custom styling.
    void Start()
    {
        // Hide game over panel at start
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (waveCompleteText != null)
            waveCompleteText.gameObject.SetActive(false);

        if (strongAttackTimerRoot != null)
            strongAttackTimerRoot.SetActive(false);

        if (pickupMessageText != null)
            pickupMessageText.gameObject.SetActive(false);

        // Connect restart button
        if (restartButton != null)
            restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        StyleHudCounters();
        StyleHealthAndTimer();
    }

    /// <summary>
    /// Moves the health bar and the Strong Attack cooldown bar to the bottom-left corner and
    /// makes them noticeably larger. Done in code so the scene asset isn't touched.
    /// </summary>
    private void StyleHealthAndTimer()
    {
        // Health bar - bottom-left, wide and tall.
        if (healthSlider != null)
        {
            RectTransform rt = healthSlider.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.sizeDelta = new Vector2(600f, 56f);
                rt.anchoredPosition = new Vector2(36f, 40f);
            }
        }

        // Strong Attack cooldown bar - just above the health bar, slightly shorter.
        if (strongAttackTimerRoot != null)
        {
            RectTransform rt = strongAttackTimerRoot.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.sizeDelta = new Vector2(480f, 34f);
                rt.anchoredPosition = new Vector2(36f, 112f);
            }
        }
    }

    /// <summary>
    /// Fixes the score readout clipping its last digit on big numbers (1000+) and gives the
    /// score / wave text a gold, medieval look instead of flat white. Done in code so the
    /// scene asset doesn't need touching.
    /// </summary>
    private void StyleHudCounters()
    {
        // Score: sits top-right. Anchor + right-align it so large numbers grow left, on-screen,
        // and never wrap.
        if (scoreText != null)
        {
            RectTransform rt = scoreText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(420f, 60f);
            rt.anchoredPosition = new Vector2(-28f, -18f);
            scoreText.alignment = TextAlignmentOptions.Right;
            scoreText.enableWordWrapping = false;
            scoreText.overflowMode = TextOverflowModes.Overflow;
            StylizeCounter(scoreText);
        }

        // Wave: sits top-left.
        if (waveText != null)
        {
            RectTransform rt = waveText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(420f, 60f);
            rt.anchoredPosition = new Vector2(28f, -18f);
            waveText.alignment = TextAlignmentOptions.Left;
            waveText.enableWordWrapping = false;
            waveText.overflowMode = TextOverflowModes.Overflow;
            StylizeCounter(waveText);
        }
    }

    // Applies a shared gold-with-dark-outline look to a HUD counter (score/wave text).
    private static void StylizeCounter(TextMeshProUGUI t)
    {
        t.fontStyle = FontStyles.Bold;
        t.fontSize = 32f;
        t.characterSpacing = 5f;

        // one flat, warm gold - reads cleanly over bright terrain or dark sky
        t.enableVertexGradient = false;
        t.color = new Color(1f, 0.82f, 0.30f, 1f);

        // strong dark outline for contrast against any background
        Material mat = t.fontMaterial; // getter returns a per-instance material, shared asset untouched
        mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.08f, 0.04f, 0f, 1f));
    }

    // Wired to the end-screen's "Main Menu" button.
    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // un-pause before leaving, so the menu scene doesn't start frozen
        SceneManager.LoadScene(0); // MainMenu is scene index 0 in Build Settings
    }

    /// <summary>
    /// Updates health bar (0-100)
    /// </summary>
    public void UpdateHealthBar(int current, int max)
    {
        if (healthSlider != null)
            healthSlider.value = (float)current / max;

        // Change color based on health
        if (healthFill != null)
        {
            if (current > 60) healthFill.color = Color.green;
            else if (current > 30) healthFill.color = Color.yellow;
            else healthFill.color = Color.red;
        }
    }

    // Called by GameManager whenever the score changes.
    public void UpdateScoreText(int score)
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    // Called by GameManager at the start of each wave.
    public void UpdateWaveText(int wave)
    {
        if (waveText != null)
            waveText.text = "Wave: " + wave;
    }

    // Briefly flashes a "Wave X Complete!" banner, then hides it automatically.
    public void ShowWaveComplete(int wave)
    {
        if (waveCompleteText != null)
        {
            waveCompleteText.gameObject.SetActive(true);
            waveCompleteText.text = "Wave " + wave + " Complete!";
            Invoke(nameof(HideWaveComplete), 3f);
        }
    }

    private void HideWaveComplete()
    {
        if (waveCompleteText != null)
            waveCompleteText.gameObject.SetActive(false);
    }

    // Called by GameManager when the player dies.
    public void ShowGameOver(int finalScore)
    {
        ShowEndScreen("GAME OVER", finalScore);
    }

    /// <summary>Shown when all waves are cleared - reuses the Game Over panel with different text.</summary>
    public void ShowVictory(int finalScore)
    {
        ShowEndScreen("VICTORY!", finalScore);
    }

    // Shared logic for both the Game Over and Victory screens - same panel, different title.
    private void ShowEndScreen(string title, int finalScore)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverTitleText != null)
            gameOverTitleText.text = title;

        if (gameOverScoreText != null)
            gameOverScoreText.text = "Final Score: " + finalScore;

        // Unlock and show cursor so player can click restart
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f; // freeze gameplay behind the end screen
    }

    // Hides the Game Over / Victory panel - called when restarting.
    public void HideGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    /// <summary>
    /// Shows the Strong Attack buff bar, full, and ready to be counted down via
    /// UpdateStrongAttackTimer. Called by PlayerCombat when the buff is applied.
    /// </summary>
    public void ShowStrongAttackTimer(float duration)
    {
        if (strongAttackTimerRoot != null)
            strongAttackTimerRoot.SetActive(true);

        if (strongAttackTimerSlider != null)
            strongAttackTimerSlider.value = 1f;
    }

    /// <summary>Called every frame while the buff is active. 1 = just picked up, 0 = about to expire.</summary>
    public void UpdateStrongAttackTimer(float normalizedRemaining)
    {
        if (strongAttackTimerSlider != null)
            strongAttackTimerSlider.value = Mathf.Clamp01(normalizedRemaining);
    }

    public void HideStrongAttackTimer()
    {
        if (strongAttackTimerRoot != null)
            strongAttackTimerRoot.SetActive(false);
    }

    /// <summary>
    /// Briefly shows a message on screen, e.g. "Full Health!" when a power-up is picked up.
    /// </summary>
    public void ShowPickupMessage(string message)
    {
        if (pickupMessageText == null) return;

        pickupMessageText.text = message;
        pickupMessageText.gameObject.SetActive(true);

        CancelInvoke(nameof(HidePickupMessage));
        Invoke(nameof(HidePickupMessage), 2f);
    }

    // Called automatically 2 seconds after ShowPickupMessage via Invoke.
    private void HidePickupMessage()
    {
        if (pickupMessageText != null)
            pickupMessageText.gameObject.SetActive(false);
    }
}
