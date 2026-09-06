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
    private AudioSource musicSource;

    void Awake()
    {
        musicSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        ApplyVolume();
        GameSettings.OnSettingsChanged += ApplyVolume;
    }

    void OnDisable()
    {
        GameSettings.OnSettingsChanged -= ApplyVolume;
    }

    private void ApplyVolume()
    {
        if (musicSource != null)
            musicSource.volume = GameSettings.MusicVolume;
    }
}
