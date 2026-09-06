using UnityEngine;
using UnityEngine.SceneManagement;

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

    private Vector3 playerStartPosition;
    private Quaternion playerStartRotation;
    private GameObject playerObject;

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

    void Awake()
    {   
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        
        Time.timeScale = 1f; 
    }

    void Start()
    {
        enemySpawner = FindObjectOfType<EnemySpawner>();
    
        // Hardcode the exact spawn position
        playerStartPosition = new Vector3(744.8f, 5.45f, 636.6f);
        playerStartRotation = Quaternion.Euler(0, 93.698f, 0); // ← use the original Y rotation
    
        playerObject = GameObject.FindGameObjectWithTag("Player");
    
        StartNextWave();
    }
    
    public void StartNextWave()
    {
        if (isGameOver) return;

        currentWave++;

        WaveDefinition wave = GetWaveDefinition(currentWave);
        Debug.Log($"Wave {currentWave} started! Melee: {wave.meleeCount}, Archers: {wave.archerCount}");

        if (enemySpawner != null)
            enemySpawner.StartWave(wave.meleeCount, wave.archerCount);

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateWaveText(currentWave);
    }

    /// <summary>
    /// Returns the hand-tuned composition for the first few waves, then keeps escalating
    /// forever past that - waves never run out, only the player dying ends a run.
    /// </summary>
    private WaveDefinition GetWaveDefinition(int wave)
    {
        if (wave <= waves.Length)
            return waves[wave - 1];

        int extraWaves = wave - waves.Length;
        return new WaveDefinition
        {
            meleeCount = waves[waves.Length - 1].meleeCount + extraWaves,
            archerCount = waves[waves.Length - 1].archerCount + extraWaves / 2
        };
    }

    public void OnWaveComplete()
    {
        if (isGameOver) return;

        Debug.Log("Wave " + currentWave + " complete!");
        Invoke(nameof(StartNextWave), timeBetweenWaves);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowWaveComplete(currentWave);
    }

    /// <summary>Called once every wave in the list has been cleared.</summary>
    public void Victory()
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("VICTORY! All waves cleared!");

        ClearPowerUps();

        if (UIManager.Instance != null)
            UIManager.Instance.ShowVictory(score);
    }

    public void AddScore(int points)
    {
        score += points;
        Debug.Log("Score: " + score);

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateScoreText(score);
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("GAME OVER!");

        ClearPowerUps();
        Time.timeScale = 0f;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameOver(score);
    }

    /// <summary>Removes any uncollected power-ups lying in the level.</summary>
    private void ClearPowerUps()
    {
        foreach (PowerUp powerUp in FindObjectsOfType<PowerUp>())
            Destroy(powerUp.gameObject);
    }

  public void RestartGame()
{
    Time.timeScale = 1f;
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
    
    isGameOver = false;
    currentWave = 0;
    score = 0;

    // Reset player position by name
   // Reset player position
    GameObject playerParent = GameObject.Find("PlayerArmature");
    if (playerParent != null)
    {
        CharacterController cc = playerParent.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = false;
    
        playerParent.transform.position = playerStartPosition;
        playerParent.transform.rotation = playerStartRotation; // ← this resets rotation too
    
        if (cc != null) cc.enabled = true;
    }

    // Reset player health
    PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
    if (playerHealth != null)
        playerHealth.ResetHealth();

    // Reset animator
    Animator playerAnimator = FindObjectOfType<PlayerHealth>()?.GetComponent<Animator>();
    if (playerAnimator != null)
    {
        playerAnimator.Rebind();
        playerAnimator.Update(0f);
    }

    // Destroy all enemies
    foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        Destroy(enemy);

    // Clear any power-ups left on the ground from the previous run
    ClearPowerUps();

    // Restart waves
    enemySpawner = FindObjectOfType<EnemySpawner>();
    StartNextWave();

    // Update UI
    if (UIManager.Instance != null)
    {
        UIManager.Instance.UpdateScoreText(0);
        UIManager.Instance.UpdateWaveText(0);
        UIManager.Instance.HideGameOver();
    }
}
}