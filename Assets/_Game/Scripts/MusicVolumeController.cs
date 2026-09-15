using UnityEngine;

/// <summary>
/// Keeps a looping background-music AudioSource's volume synced to GameSettings.MusicVolume.
/// AudioListener.volume (Master) already scales every sound in the scene, and each SFX call
/// multiplies in GameSettings.SfxVolume itself - but nothing was ever applying MusicVolume to
/// the music track's own AudioSource, so its slider did nothing. This is that missing link.
///
/// Add to whichever GameObject holds the looping music AudioSource
/// (Tools > Medieval Survival > Wire Music Volume does this automatically).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicVolumeController : MonoBehaviour
{
    private AudioSource musicSource; // the looping background-music AudioSource on this object

    void Awake()
    {
        musicSource = GetComponent<AudioSource>();
    }

    // Apply the saved volume immediately when this object becomes active, and keep
    // listening so the music updates live whenever the player changes the slider.
    void OnEnable()
    {
        ApplyVolume();
        GameSettings.OnSettingsChanged += ApplyVolume;
    }

    // Always unsubscribe when disabled/destroyed to avoid calling into a dead object.
    void OnDisable()
    {
        GameSettings.OnSettingsChanged -= ApplyVolume;
    }

    // Copies the current MusicVolume setting onto the actual AudioSource playing the music.
    private void ApplyVolume()
    {
        if (musicSource != null)
            musicSource.volume = GameSettings.MusicVolume;
    }
}
