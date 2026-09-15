using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip buttonHoverSound;
    private AudioSource audioSource;
    [Header("Cursor")]
    [SerializeField] private Texture2D swordCursor;

    [Header("Panels")]
    [SerializeField] private GameObject mainButtonsPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject howToPlayPanel;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    if (audioSource == null)
        audioSource = gameObject.AddComponent<AudioSource>(); // add one if the menu object doesn't already have it

    // Set sword cursor
    if (swordCursor != null)
        Cursor.SetCursor(swordCursor, Vector2.zero, CursorMode.Auto); // replace the OS arrow with a themed cursor

    // Sub-panels start closed
    if (settingsPanel != null)
        settingsPanel.SetActive(false);
    if (howToPlayPanel != null)
        howToPlayPanel.SetActive(false);
    }

    // Plays the UI hover blip - wired to each button's PointerEnter event.
    public void PlayHoverSound()
    {
        if (buttonHoverSound != null)
            audioSource.PlayOneShot(buttonHoverSound, 0.5f * GameSettings.SfxVolume);
    }

    // Plays the UI click sound - called at the start of every button action below.
    public void PlayClickSound()
    {
        if (buttonClickSound != null)
            audioSource.PlayOneShot(buttonClickSound, 1f * GameSettings.SfxVolume);
    }

    // Wired to the Start button.
    public void StartGame()
    {
        PlayClickSound();
        Invoke(nameof(LoadGame), 0.2f); // short delay so the click sound isn't cut off by the scene change
    }

    // Loads the Gameplay scene (build index 1).
    private void LoadGame()
    {
        SceneManager.LoadScene(1);
    }

    // Wired to the Settings button - swaps the main menu buttons out for the settings panel.
    public void OpenSettings()
    {
        PlayClickSound();

        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
    }

    // Wired to the How To Play button - swaps the main menu buttons out for the controls panel.
    public void OpenHowToPlay()
    {
        PlayClickSound();

        if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
    }

    /// <summary>Wired to the How To Play panel's Back button.</summary>
    public void CloseHowToPlay()
    {
        PlayClickSound();

        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    // Wired to the Quit button.
    public void QuitGame()
    {
        PlayClickSound();
        Invoke(nameof(Quit), 0.2f); // short delay so the click sound plays before the app closes
    }

    // Closes the application (does nothing in the Editor - only works in a real build).
    private void Quit()
    {
        Application.Quit();
    }
}