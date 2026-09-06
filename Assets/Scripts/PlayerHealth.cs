using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Effects")]
    [SerializeField] private GameObject damageEffect;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private float damageFlashDuration = 0.1f;

    private AudioSource audioSource;
    private Renderer[] renderers;
    private PlayerBlock playerBlock;

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        renderers = GetComponentsInChildren<Renderer>();
        playerBlock = GetComponent<PlayerBlock>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Initialize health bar to full
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);

        Debug.Log("Player health initialized: " + currentHealth);
    }

    /// <summary>
    /// Called when player takes damage
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        if (playerBlock != null)
            damage = playerBlock.ModifyIncomingDamage(damage);

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log("Player took " + damage + " damage! Health: " + currentHealth);

        // Update health bar UI
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);

        // Play hurt sound - same 1x scale as every other combat sound (enemy hit, enemy
        // death, arrow impact). This used to be scaled to 0.3x, which made getting hit
        // sound much quieter than landing a hit for no real reason.
        if (hurtSound != null && audioSource != null)
            audioSource.PlayOneShot(hurtSound, GameSettings.SfxVolume);

        // Flash the player red, same as enemies do when hit
        StartCoroutine(DamageFlash());

        // Show damage effect
        if (damageEffect != null)
            StartCoroutine(ShowDamageEffect());

        // Check if dead
        if (currentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Visual feedback when hurt: briefly tints the player's renderers red
    /// </summary>
    private System.Collections.IEnumerator DamageFlash()
    {
        if (renderers == null || renderers.Length == 0) yield break;

        foreach (Renderer r in renderers)
            r.material.color = Color.red;

        yield return new WaitForSeconds(damageFlashDuration);

        foreach (Renderer r in renderers)
            r.material.color = Color.white;
    }

    /// <summary>
    /// Visual feedback when hurt
    /// </summary>
    private System.Collections.IEnumerator ShowDamageEffect()
    {
        damageEffect.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        damageEffect.SetActive(false);
    }

    /// <summary>
    /// Called when player dies
    /// </summary>
    private void Die()
    {
        Debug.Log("Player has died! Game Over!");

        // Tell GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }
    
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
    }

    /// <summary>
    /// Restores health, e.g. from a health power-up or potion. Clamped to maxHealth.
    /// </summary>
    public void Heal(int amount)
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        Debug.Log("Player healed " + amount + "! Health: " + currentHealth);

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
    }

    /// <summary>
    /// Restores health to 100% instantly - used by the Health power-up.
    /// </summary>
    public void FullHeal()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        currentHealth = maxHealth;
        Debug.Log("Player fully healed!");

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
    }

    public int GetCurrentHealth() { return currentHealth; }
    public int GetMaxHealth() { return maxHealth; }
}