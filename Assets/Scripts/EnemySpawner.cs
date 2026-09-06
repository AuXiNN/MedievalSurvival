using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Types")]
    [SerializeField] private GameObject meleeEnemyPrefab;
    [SerializeField] private GameObject archerEnemyPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float timeBetweenSpawns = 1.5f;

    [Header("Debug")]
    [SerializeField] private int currentEnemyCount = 0;
    private readonly List<GameObject> spawnQueue = new List<GameObject>();
    private int enemiesToSpawnThisWave = 0;
    private int enemiesSpawnedThisWave = 0;

    /// <summary>
    /// Called by GameManager to start a new wave with an exact melee/archer composition.
    /// </summary>
    public void StartWave(int meleeCount, int archerCount)
    {
        spawnQueue.Clear();
        for (int i = 0; i < meleeCount; i++) spawnQueue.Add(meleeEnemyPrefab);
        for (int i = 0; i < archerCount; i++) spawnQueue.Add(archerEnemyPrefab);
        Shuffle(spawnQueue); // so archers/melee don't always spawn in the same block order

        enemiesToSpawnThisWave = spawnQueue.Count;
        enemiesSpawnedThisWave = 0;
        currentEnemyCount = 0;

        Debug.Log($"Spawner: Starting wave with {meleeCount} melee + {archerCount} archer enemies!");

        // Spawn enemies one by one with delay
        InvokeRepeating(nameof(SpawnEnemy), 0.5f, timeBetweenSpawns);
    }

    private void SpawnEnemy()
    {
        // Stop spawning if we've spawned enough for this wave
        if (enemiesSpawnedThisWave >= enemiesToSpawnThisWave)
        {
            CancelInvoke(nameof(SpawnEnemy));
            Debug.Log("Spawner: All enemies for this wave spawned!");
            return;
        }

        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return;
        }

        GameObject enemyPrefab = spawnQueue[enemiesSpawnedThisWave];
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: a slot in this wave has no prefab assigned (melee/archer prefab missing).");
            enemiesSpawnedThisWave++; // skip it so the wave doesn't stall forever
            return;
        }

        Transform spawnPoint = GetFreeSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.Log("All spawn points occupied, retrying...");
            return;
        }

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        currentEnemyCount++;
        enemiesSpawnedThisWave++;

        // Register spawner with enemy
        EnemyHealth enemyHealth = newEnemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
            enemyHealth.SetSpawner(this);

        Debug.Log("Enemy spawned! " + enemiesSpawnedThisWave + "/" + enemiesToSpawnThisWave);
    }

    /// <summary>
    /// Called by EnemyHealth when enemy dies
    /// </summary>
    public void OnEnemyDeath()
    {
        currentEnemyCount--;
        Debug.Log("Enemy died! Remaining: " + currentEnemyCount);

        // Check if all enemies are dead to trigger next wave
        if (currentEnemyCount <= 0 && enemiesSpawnedThisWave >= enemiesToSpawnThisWave)
        {
            Debug.Log("All enemies defeated!");
            if (GameManager.Instance != null)
                GameManager.Instance.OnWaveComplete();
        }
    }

    private Transform GetFreeSpawnPoint()
    {
        int randomStart = Random.Range(0, spawnPoints.Length);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            int index = (randomStart + i) % spawnPoints.Length;
            Transform point = spawnPoints[index];

            Collider[] nearby = Physics.OverlapSphere(point.position, 2f);
            bool isFree = true;
            foreach (Collider col in nearby)
            {
                if (col.CompareTag("Enemy"))
                {
                    isFree = false;
                    break;
                }
            }
            if (isFree) return point;
        }
        return null;
    }

    private static void Shuffle(List<GameObject> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        Gizmos.color = Color.magenta;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawSphere(point.position, 0.5f);
                Gizmos.DrawWireSphere(point.position, 2f);
            }
        }
    }
}
