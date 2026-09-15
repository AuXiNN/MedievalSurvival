using UnityEngine;

public enum GameDifficulty { Easy, Normal, Hard }

/// <summary>Per-difficulty multipliers applied to the wave system (see GameManager).</summary>
public readonly struct DifficultyScale
{
    public readonly float waveGap;    // × time between waves
    public readonly float spawnGap;   // × time between individual spawns in a wave
    public readonly float enemyCount; // × enemies per wave

    public DifficultyScale(float waveGap, float spawnGap, float enemyCount)
    {
        this.waveGap = waveGap;
        this.spawnGap = spawnGap;
        this.enemyCount = enemyCount;
    }
}

/// <summary>
/// Central, scene-independent store for player settings (audio, graphics, controls).
/// Loads from PlayerPrefs on boot and applies engine-level settings (volume, quality,
/// resolution, fullscreen) automatically, so it works correctly no matter which scene
/// is loaded first. UI (SettingsPanelUI) reads/writes through this class - it never
/// touches PlayerPrefs or the engine APIs directly.
/// </summary>
public static class GameSettings
{
    // ---- PlayerPrefs keys ----
    private const string KeyMaster = "Settings_MasterVolume";
    private const string KeyMusic = "Settings_MusicVolume";
    private const string KeySfx = "Settings_SfxVolume";
    private const string KeyQuality = "Settings_QualityLevel";
    private const string KeyFullscreen = "Settings_Fullscreen";
    private const string KeyResWidth = "Settings_ResWidth";
    private const string KeyResHeight = "Settings_ResHeight";
    private const string KeySensitivity = "Settings_MouseSensitivity";
    private const string KeyInvertY = "Settings_InvertY";
    private const string KeyDifficulty = "Settings_Difficulty";

    // ---- Current values ----
    public static float MasterVolume { get; private set; } = 1f;
    public static float MusicVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;
    public static int QualityLevel { get; private set; }
    public static bool Fullscreen { get; private set; } = true;
    public static int ResolutionWidth { get; private set; }
    public static int ResolutionHeight { get; private set; }
    public static float MouseSensitivity { get; private set; } = 1f;
    public static bool InvertY { get; private set; } = false;
    public static GameDifficulty Difficulty { get; private set; } = GameDifficulty.Normal;

    /// <summary>Wave-timing multipliers for the current difficulty. Read by GameManager.</summary>
    public static DifficultyScale SpawnScale => Difficulty switch
    {
        GameDifficulty.Easy => new DifficultyScale(1.5f, 1.4f, 0.7f),
        GameDifficulty.Hard => new DifficultyScale(0.65f, 0.6f, 1.4f),
        _                   => new DifficultyScale(1f, 1f, 1f),
    };

    private static bool _loaded;

    /// <summary>Raised whenever any setting changes, so open UI can refresh itself.</summary>
    public static event System.Action OnSettingsChanged;

    // Runs before any scene loads, in every play session (editor Play or a build),
    // so audio/quality/resolution are always correct even if the game boots straight
    // into a non-menu scene.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        Load();
        Apply();
    }

    // Reads every setting from PlayerPrefs, falling back to sensible defaults (often the
    // current engine/screen state) the very first time the game ever runs on this machine.
    public static void Load()
    {
        MasterVolume = PlayerPrefs.GetFloat(KeyMaster, 1f);
        MusicVolume = PlayerPrefs.GetFloat(KeyMusic, 1f);
        SfxVolume = PlayerPrefs.GetFloat(KeySfx, 1f);
        QualityLevel = PlayerPrefs.GetInt(KeyQuality, QualitySettings.GetQualityLevel());
        Fullscreen = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
        ResolutionWidth = PlayerPrefs.GetInt(KeyResWidth, Screen.currentResolution.width);
        ResolutionHeight = PlayerPrefs.GetInt(KeyResHeight, Screen.currentResolution.height);
        MouseSensitivity = PlayerPrefs.GetFloat(KeySensitivity, 1f);
        InvertY = PlayerPrefs.GetInt(KeyInvertY, 0) == 1;
        Difficulty = (GameDifficulty)PlayerPrefs.GetInt(KeyDifficulty, (int)GameDifficulty.Normal);
        _loaded = true;
    }

    /// <summary>Applies the current values to the engine (call once at boot, or to re-sync).</summary>
    public static void Apply()
    {
        if (!_loaded) Load();

        AudioListener.volume = MasterVolume;
        QualitySettings.SetQualityLevel(Mathf.Clamp(QualityLevel, 0, QualitySettings.names.Length - 1), true);
        Screen.SetResolution(ResolutionWidth, ResolutionHeight,
            Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    // Wired to the Settings panel's "Restore Defaults" button.
    public static void ResetToDefaults()
    {
        MasterVolume = 1f;
        MusicVolume = 1f;
        SfxVolume = 1f;
        QualityLevel = QualitySettings.names.Length - 1;
        Fullscreen = true;
        ResolutionWidth = Screen.currentResolution.width;
        ResolutionHeight = Screen.currentResolution.height;
        MouseSensitivity = 1f;
        InvertY = false;
        Difficulty = GameDifficulty.Normal;

        Apply();
        SaveAll();
        OnSettingsChanged?.Invoke();
    }

    // ---- Setters: update the value, apply it live, persist it, and notify listeners ----
    // Every setter below follows the same 3-4 step pattern: clamp/validate the incoming value,
    // apply it to the relevant engine API immediately (so the change is felt right away),
    // save it to PlayerPrefs (so it survives restarting the game), then fire OnSettingsChanged
    // so any open settings UI can refresh and show the new value.

    public static void SetDifficulty(GameDifficulty value)
    {
        Difficulty = value;
        PlayerPrefs.SetInt(KeyDifficulty, (int)value);
        OnSettingsChanged?.Invoke();
    }

    public static void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        AudioListener.volume = MasterVolume;
        PlayerPrefs.SetFloat(KeyMaster, MasterVolume);
        OnSettingsChanged?.Invoke();
    }

    public static void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeyMusic, MusicVolume);
        OnSettingsChanged?.Invoke();
    }

    public static void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeySfx, SfxVolume);
        OnSettingsChanged?.Invoke();
    }

    public static void SetQualityLevel(int index)
    {
        QualityLevel = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(QualityLevel, true);
        PlayerPrefs.SetInt(KeyQuality, QualityLevel);
        OnSettingsChanged?.Invoke();
    }

    public static void SetFullscreen(bool value)
    {
        Fullscreen = value;
        Screen.fullScreen = value;
        PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0);
        OnSettingsChanged?.Invoke();
    }

    public static void SetResolution(int width, int height)
    {
        ResolutionWidth = width;
        ResolutionHeight = height;
        Screen.SetResolution(width, height, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        PlayerPrefs.SetInt(KeyResWidth, width);
        PlayerPrefs.SetInt(KeyResHeight, height);
        OnSettingsChanged?.Invoke();
    }

    public static void SetMouseSensitivity(float value)
    {
        MouseSensitivity = Mathf.Clamp(value, 0.1f, 3f);
        PlayerPrefs.SetFloat(KeySensitivity, MouseSensitivity);
        OnSettingsChanged?.Invoke();
    }

    public static void SetInvertY(bool value)
    {
        InvertY = value;
        PlayerPrefs.SetInt(KeyInvertY, value ? 1 : 0);
        OnSettingsChanged?.Invoke();
    }

    public static void SaveAll() => PlayerPrefs.Save();
}
