using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns a wave's worth of enemies, one at a time, at randomly-chosen spawn points.
/// The actual wave timing (how long between waves, when the next one starts) lives in
/// GameManager's wave coroutine - this class just knows how to place one wave on request
/// and how many of that wave are still alive.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Types")]
    [SerializeField] private GameObject meleeEnemyPrefab;
    [SerializeField] private GameObject archerEnemyPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Seconds between each enemy appearing within a single wave.")]
    [SerializeField] private float timeBetweenSpawns = 1.5f;

    [Header("Debug (read-only)")]
    [SerializeField] private int liveEnemyCount = 0;

    // Multiplies timeBetweenSpawns - GameManager sets this from the chosen difficulty
    // (>1 = enemies trickle in slower on Easy, <1 = they pour in on Hard).
    private float spawnGapMultiplier = 1f;

    /// <summary>Enemies still alive from the wave in progress. GameManager's coroutine waits on this.</summary>
    public int LiveEnemyCount => liveEnemyCount;

    /// <summary>Called by GameManager at startup to apply the difficulty's spawn-rate scaling.</summary>
    public void SetSpawnGapMultiplier(float multiplier)
    {
        spawnGapMultiplier = Mathf.Max(0.1f, multiplier);
    }

    /// <summary>
    /// Spawns one wave, one enemy every <see cref="timeBetweenSpawns"/> seconds. It's a
    /// coroutine so GameManager can `yield return` it and continue only once the whole wave
    /// has been placed - no InvokeRepeating, no manual CancelInvoke.
    /// </summary>
    public IEnumerator SpawnWaveRoutine(int meleeCount, int archerCount)
    {
        // Build a shuffled spawn order so melee and archers aren't always grouped together.
        List<GameObject> queue = new List<GameObject>();
        for (int i = 0; i < meleeCount; i++) queue.Add(meleeEnemyPrefab);
        for (int i = 0; i < archerCount; i++) queue.Add(archerEnemyPrefab);
        Shuffle(queue);

        liveEnemyCount = 0;
        Debug.Log($"Spawner: placing wave - {meleeCount} melee + {archerCount} archers.");

        foreach (GameObject prefab in queue)
        {
            SpawnOne(prefab);
            yield return new WaitForSeconds(timeBetweenSpawns * spawnGapMultiplier);
        }

        Debug.Log("Spawner: whole wave placed.");
    }

    // Instantiates a single enemy at a spawn point and registers it with this spawner
    // so its death can be tracked toward the wave's live enemy count.
    private void SpawnOne(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("EnemySpawner: melee/archer prefab not assigned - skipping this spawn.");
            return;
        }
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("EnemySpawner: no spawn points assigned!");
            return;
        }

        // Prefer a spawn point with no enemy standing on it; fall back to a random one.
        Transform point = GetFreeSpawnPoint();
        if (point == null) point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject enemy = Instantiate(prefab, point.position, point.rotation);
        liveEnemyCount++;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null) health.SetSpawner(this);

        Debug.Log($"Enemy spawned at '{point.name}'. Alive this wave: {liveEnemyCount}");
    }

    /// <summary>
    /// Briefly instantiates one of each enemy type so their materials render (and their
    /// shaders compile) right now, rather than the first time a real enemy of that type
    /// spawns into view during a wave - which is what causes the stutter the moment the
    /// player first sees a melee/archer enemy. Callers are expected to hide this behind a
    /// loading/fade overlay since these instances are fully visible while they exist.
    /// </summary>
    public IEnumerator WarmupShadersRoutine()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) yield break;

        List<GameObject> warmupInstances = new List<GameObject>();
        foreach (GameObject prefab in new[] { meleeEnemyPrefab, archerEnemyPrefab })
        {
            if (prefab == null) continue;
            warmupInstances.Add(Instantiate(prefab, spawnPoints[0].position, spawnPoints[0].rotation));
        }

        // Give the render pipeline a couple of frames to actually draw (and thus compile) them.
        yield return null;
        yield return null;

        foreach (GameObject instance in warmupInstances)
            Destroy(instance);
    }

    /// <summary>Called by EnemyHealth once an enemy has finished dying.</summary>
    public void OnEnemyDeath()
    {
        liveEnemyCount = Mathf.Max(0, liveEnemyCount - 1);
        Debug.Log($"Enemy down. Alive this wave: {liveEnemyCount}");
    }

    // Searches the spawn points (starting from a random one, wrapping around) for one that
    // doesn't already have an enemy standing near it, so enemies don't spawn stacked on top
    // of each other. Returns null if every point is currently occupied.
    private Transform GetFreeSpawnPoint()
    {
        int randomStart = Random.Range(0, spawnPoints.Length);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[(randomStart + i) % spawnPoints.Length];
            if (point == null) continue;

            bool occupied = false;
            foreach (Collider col in Physics.OverlapSphere(point.position, 2f))
            {
                if (col.CompareTag("Enemy")) { occupied = true; break; } // something with the Enemy tag is already here
            }
            if (!occupied) return point;
        }
        return null;
    }

    // Randomizes the order of a list in place (Fisher-Yates shuffle), used so the wave's
    // enemy types spawn in a mixed order instead of all melee then all archers.
    private static void Shuffle(List<GameObject> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]); // swap
        }
    }

    // Draws each spawn point as a small sphere (and its occupancy-check radius) in the Scene view.
    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        Gizmos.color = Color.magenta;
        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;
            Gizmos.DrawSphere(point.position, 0.5f);
            Gizmos.DrawWireSphere(point.position, 2f);
        }
    }
}
