using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>How many melee/archer enemies spawn in a given wave.</summary>
[System.Serializable]
public class WaveDefinition
{
    public int meleeCount;
    public int archerCount;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public bool isGameOver = false;
    public int currentWave = 0;
    public int score = 0;

    [Header("Wave Settings")]
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField]
    private WaveDefinition[] waves = new WaveDefinition[]
    {
        new WaveDefinition { meleeCount = 1, archerCount = 0 },
        new WaveDefinition { meleeCount = 2, archerCount = 0 },
        new WaveDefinition { meleeCount = 2, archerCount = 1 },
        new WaveDefinition { meleeCount = 3, archerCount = 2 },
        new WaveDefinition { meleeCount = 3, archerCount = 2 },
    };

    private EnemySpawner enemySpawner;

    // Wave-timing multipliers from the chosen difficulty (see GameSettings / the menu selector).
    private DifficultyScale scale = new DifficultyScale(1f, 1f, 1f);

    // Sets up the singleton and makes sure the game starts unpaused (Time.timeScale can be
    // left at 0 from a previous Game Over/Pause if this scene was reloaded).
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject); // enforce a single GameManager - destroy any duplicate

        Time.timeScale = 1f;
    }

    // Reads the chosen difficulty, applies it to the spawner, and kicks off the wave loop.
    void Start()
    {
        enemySpawner = FindObjectOfType<EnemySpawner>();

        scale = GameSettings.SpawnScale; // difficulty multipliers picked on the main menu
        Debug.Log($"Difficulty: {GameSettings.Difficulty}  " +
                  $"(wave gap x{scale.waveGap}, spawn gap x{scale.spawnGap}, count x{scale.enemyCount})");
        if (enemySpawner != null)
            enemySpawner.SetSpawnGapMultiplier(scale.spawnGap);

        StartCoroutine(RunWaves()); // begin spawning wave 1
    }

    /// <summary>
    /// The entire wave loop as one readable coroutine: spawn a wave, wait for the player to
    /// clear it, take a breather, repeat - forever, until the player dies. Uses Coroutines
    /// with WaitForSeconds / WaitUntil instead of scattered Invoke / InvokeRepeating timers,
    /// so the whole flow reads top-to-bottom in one place.
    /// </summary>
    private IEnumerator RunWaves()
    {
        yield return IntroWarmupAndFade(); // hide the first-frame shader-compile stutter behind a brief fade

        while (!isGameOver)
        {
            currentWave++;
            WaveDefinition wave = GetWaveDefinition(currentWave);
            Debug.Log($"=== Wave {currentWave} begins: {wave.meleeCount} melee, {wave.archerCount} archers ===");

            if (UIManager.Instance != null)
                UIManager.Instance.UpdateWaveText(currentWave);

            // 1. place every enemy in the wave, one at a time
            if (enemySpawner != null)
                yield return enemySpawner.SpawnWaveRoutine(wave.meleeCount, wave.archerCount);

            // 2. wait until the player has killed them all
            yield return new WaitUntil(() =>
                isGameOver || enemySpawner == null || enemySpawner.LiveEnemyCount == 0);

            if (isGameOver) yield break;

            Debug.Log($"=== Wave {currentWave} cleared ===");
            if (UIManager.Instance != null)
                UIManager.Instance.ShowWaveComplete(currentWave);

            // 3. breather before the next wave (longer on Easy, shorter on Hard)
            yield return new WaitForSeconds(timeBetweenWaves * scale.waveGap);
        }
    }

    /// <summary>
    /// Covers the very start of the level with a runtime-built black overlay while enemy
    /// shaders get force-compiled behind it (see EnemySpawner.WarmupShadersRoutine). Without
    /// this, the first-ever render of the terrain's materials and the first enemy the player
    /// meets each cause a visible hitch, since Unity compiles a shader variant the first time
    /// it's actually used rather than ahead of time. Built entirely in code so it doesn't
    /// depend on any UI already existing in the scene.
    /// </summary>
    private IEnumerator IntroWarmupAndFade()
    {
        GameObject canvasObj = new GameObject("IntroFadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // draw on top of the HUD and everything else

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        Image fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = Color.black;
        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        yield return null; // let the opaque black frame render once before doing anything expensive

        if (enemySpawner != null)
            yield return enemySpawner.WarmupShadersRoutine();

        yield return new WaitForSeconds(0.5f); // give slower machines time to finish compiling

        float fadeDuration = 1f;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeImage.color = new Color(0f, 0f, 0f, 1f - (t / fadeDuration));
            yield return null;
        }

        Destroy(canvasObj);
    }

    /// <summary>
    /// Returns the hand-tuned composition for the first few waves, then keeps escalating
    /// forever past that - waves never run out, only the player dying ends a run.
    /// The counts are then scaled by the chosen difficulty (fewer on Easy, more on Hard).
    /// </summary>
    private WaveDefinition GetWaveDefinition(int wave)
    {
        WaveDefinition baseWave;
        if (wave <= waves.Length)
        {
            baseWave = waves[wave - 1];
        }
        else
        {
            int extraWaves = wave - waves.Length;
            baseWave = new WaveDefinition
            {
                meleeCount = waves[waves.Length - 1].meleeCount + extraWaves,
                archerCount = waves[waves.Length - 1].archerCount + extraWaves / 2
            };
        }

        return new WaveDefinition
        {
            meleeCount = Mathf.Max(1, Mathf.RoundToInt(baseWave.meleeCount * scale.enemyCount)),
            archerCount = Mathf.RoundToInt(baseWave.archerCount * scale.enemyCount)
        };
    }

    /// <summary>Called once every wave in the list has been cleared.</summary>
    public void Victory()
    {
        if (isGameOver) return; // don't run this twice

        isGameOver = true; // this also stops RunWaves()'s while loop from spawning any more waves
        Debug.Log("VICTORY! All waves cleared!");

        ClearPowerUps();

        if (UIManager.Instance != null)
            UIManager.Instance.ShowVictory(score);
    }

    // Called whenever the player earns points (e.g. killing an enemy) - keeps the HUD in sync.
    public void AddScore(int points)
    {
        score += points;
        Debug.Log("Score: " + score);

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateScoreText(score);
    }

    // Called by PlayerHealth when the player's health reaches zero.
    public void GameOver()
    {
        if (isGameOver) return; // don't run this twice

        isGameOver = true;
        Debug.Log("GAME OVER!");

        StopAllCoroutines(); // stop the wave loop
        ClearPowerUps();
        Time.timeScale = 0f; // freeze the game behind the Game Over screen

        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameOver(score);
    }

    /// <summary>Removes any uncollected power-ups lying in the level.</summary>
    private void ClearPowerUps()
    {
        foreach (PowerUp powerUp in FindObjectsOfType<PowerUp>())
            Destroy(powerUp.gameObject);
    }

    /// <summary>
    /// Wired to the Game Over screen's Restart button. Reloads the current scene through
    /// SceneManagement - one call rebuilds the whole level (player, enemies, UI, waves,
    /// power-ups) back to its starting state, so there is nothing to reset by hand.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f; // reloading the scene does not reset the time scale on its own
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}