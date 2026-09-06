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

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

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
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
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

    public void UpdateScoreText(int score)
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    public void UpdateWaveText(int wave)
    {
        if (waveText != null)
            waveText.text = "Wave: " + wave;
    }

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

    public void ShowGameOver(int finalScore)
    {
        ShowEndScreen("GAME OVER", finalScore);
    }

    /// <summary>Shown when all waves are cleared - reuses the Game Over panel with different text.</summary>
    public void ShowVictory(int finalScore)
    {
        ShowEndScreen("VICTORY!", finalScore);
    }

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

        Time.timeScale = 0f;
    }

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

    private void HidePickupMessage()
    {
        if (pickupMessageText != null)
            pickupMessageText.gameObject.SetActive(false);
    }
}
