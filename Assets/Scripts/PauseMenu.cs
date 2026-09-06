using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using StarterAssets;

/// <summary>
/// Handles the Esc pause menu in the gameplay scene: Resume / Settings / Quit to Main Menu.
/// Lives on an always-active "PauseMenuController" object (built by
/// Tools > Medieval Survival > Build Pause Menu) so it keeps listening for Esc even while
/// its child panels are hidden.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanelRoot;    // Resume / Settings / Quit buttons
    [SerializeField] private GameObject settingsPanelRoot; // the shared Settings panel

    [Header("Audio")]
    [Tooltip("Same click sound used by the Main Menu buttons, for consistency.")]
    [SerializeField] private AudioClip clickSound;
    private AudioSource audioSource;

    public bool IsPaused { get; private set; }

    // MainMenu is scene 0, the gameplay scene is scene 1 in Build Settings - Esc should only
    // ever do anything in the latter, regardless of which scenes this component ends up in.
    private const int GameSceneBuildIndex = 1;

    // ThirdPersonController reads mouse delta straight off the hardware every LateUpdate,
    // unscaled by Time.timeScale - unlocking the cursor alone doesn't stop it, so the camera
    // kept spinning while paused/in Settings. Gating cursorInputForLook off is what actually
    // stops it (StarterAssetsInputs.OnLook no-ops while it's false).
    private StarterAssetsInputs playerInput;
    private StarterAssetsInputs PlayerInput
    {
        get
        {
            if (playerInput == null)
                playerInput = FindFirstObjectByType<StarterAssetsInputs>();
            return playerInput;
        }
    }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().buildIndex != GameSceneBuildIndex) return;
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            PlayClickSound();

            if (settingsPanelRoot != null && settingsPanelRoot.activeSelf)
            {
                // Back out of Settings to the pause menu instead of resuming outright.
                settingsPanelRoot.SetActive(false);
                if (pausePanelRoot != null) pausePanelRoot.SetActive(true);
            }
            else if (IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    /// <summary>
    /// Plays on every Esc press (open, resume, or back-out-of-settings). Public so it can also
    /// be wired directly onto the pause menu's own Resume/Settings/Quit buttons in the Inspector,
    /// alongside their existing onClick calls, so both paths make the same sound.
    /// </summary>
    public void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
            audioSource.PlayOneShot(clickSound, GameSettings.SfxVolume);
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (PlayerInput != null)
        {
            PlayerInput.cursorInputForLook = false;
            PlayerInput.look = Vector2.zero; // clear any leftover delta so it can't keep spinning the camera
        }

        if (settingsPanelRoot != null) settingsPanelRoot.SetActive(false);
        if (pausePanelRoot != null) pausePanelRoot.SetActive(true);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (PlayerInput != null)
            PlayerInput.cursorInputForLook = true;

        if (pausePanelRoot != null) pausePanelRoot.SetActive(false);
        if (settingsPanelRoot != null) settingsPanelRoot.SetActive(false);
    }

    /// <summary>Wired to the pause menu's Settings button.</summary>
    public void OpenSettings()
    {
        if (pausePanelRoot != null) pausePanelRoot.SetActive(false);
        if (settingsPanelRoot != null) settingsPanelRoot.SetActive(true);
    }

    /// <summary>Wired to the pause menu's Quit button.</summary>
    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(0); // MainMenu is scene index 0 in Build Settings
    }
}
