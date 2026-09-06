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
        audioSource = gameObject.AddComponent<AudioSource>();

    // Set sword cursor
    if (swordCursor != null)
        Cursor.SetCursor(swordCursor, Vector2.zero, CursorMode.Auto);

    // Sub-panels start closed
    if (settingsPanel != null)
        settingsPanel.SetActive(false);
    if (howToPlayPanel != null)
        howToPlayPanel.SetActive(false);
    }

    public void PlayHoverSound()
    {
        if (buttonHoverSound != null)
            audioSource.PlayOneShot(buttonHoverSound, 0.5f * GameSettings.SfxVolume);
    }

    public void PlayClickSound()
    {
        if (buttonClickSound != null)
            audioSource.PlayOneShot(buttonClickSound, 1f * GameSettings.SfxVolume);
    }

    public void StartGame()
    {
        PlayClickSound();
        Invoke(nameof(LoadGame), 0.2f);
    }

    private void LoadGame()
    {
        SceneManager.LoadScene(1);
    }

    public void OpenSettings()
    {
        PlayClickSound();

        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
    }

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

    public void QuitGame()
    {
        PlayClickSound();
        Invoke(nameof(Quit), 0.2f);
    }

    private void Quit()
    {
        Application.Quit();
    }
}