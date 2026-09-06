using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("World-Space Health Bar")]
    [SerializeField] private Slider worldHealthSlider;
    [SerializeField] private GameObject worldHealthBarRoot;

    [Header("Effects")]
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;

    [Header("Power-Up Drops")]
    [SerializeField] private GameObject[] powerUpPrefabs;
    [SerializeField] [Range(0f, 1f)] private float powerUpDropChance = 0.25f;

    // Reference to spawner so it knows when we die
    private EnemySpawner spawner;
    private AudioSource audioSource;

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (worldHealthSlider != null)
            worldHealthSlider.value = 1f;

        Debug.Log(gameObject.name + " spawned with " + currentHealth + " health.");
    }

    /// <summary>
    /// Called by EnemySpawner to register itself
    /// </summary>
    public void SetSpawner(EnemySpawner s)
    {
        spawner = s;
    }

    /// <summary>
    /// Called when enemy takes damage
    /// </summary>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log(gameObject.name + " took " + damage + " damage! Health: " + currentHealth);

        if (worldHealthSlider != null)
            worldHealthSlider.value = (float)currentHealth / maxHealth;

        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound, GameSettings.SfxVolume);

        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Visual feedback when hit
    /// </summary>
    private System.Collections.IEnumerator DamageFlash()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            foreach (Renderer r in renderers)
                r.material.color = Color.red;

            yield return new WaitForSeconds(0.1f);

            foreach (Renderer r in renderers)
                r.material.color = Color.white;
        }
    }

    /// <summary>
    /// Called when enemy health reaches zero
    /// </summary>
    private void Die()
    {
        Debug.Log(gameObject.name + " has been defeated!");

        // Play death animation FIRST, and via Play() rather than SetTrigger(). SetTrigger just
        // requests a transition - if the Combat layer is mid Load/Hold/Release (archers) or any
        // other state, it has to wait for a valid outgoing transition before it actually cuts
        // over, which reads as a delay. Play() jumps straight to the Death state on every layer
        // instantly, no transition/exit-time wait, regardless of what was playing.
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Play("Death", 0, 0f);
            if (anim.layerCount > 1)
                anim.Play("Death", 1, 0f);
        }

        // Play death sound
        if (deathSound != null)
            AudioSource.PlayClipAtPoint(deathSound, transform.position, GameSettings.SfxVolume);

        // Stop enemy from floating
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Disable collider
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Hide the health bar over the corpse
        if (worldHealthBarRoot != null)
            worldHealthBarRoot.SetActive(false);

        // Disable AI so enemy stops moving (whichever behaviour this enemy uses)
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
            ai.enabled = false;

        ArcherAI archerAi = GetComponent<ArcherAI>();
        if (archerAi != null)
            archerAi.enabled = false;

        // Delay destroy so animation plays
        StartCoroutine(DeathDelay());
    }

    private System.Collections.IEnumerator DeathDelay()
    {
        yield return new WaitForSeconds(2.5f);

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(10);

        if (spawner != null)
            spawner.OnEnemyDeath();

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        TryDropPowerUp();

        Destroy(gameObject);
    }

    /// <summary>
    /// Rolls the power-up drop chance and spawns a random one from the assigned list.
    /// </summary>
    private void TryDropPowerUp()
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Length == 0) return;
        if (Random.value > powerUpDropChance) return;

        GameObject prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
        if (prefab != null)
            Instantiate(prefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }
}