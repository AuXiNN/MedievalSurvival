using UnityEngine;

public enum PowerUpType
{
    Health,
    StrongAttack
}

/// <summary>
/// A pickup that applies its effect instantly when the player walks into it
/// (as opposed to inventory items, which are collected and used later).
/// Drops from enemies (see EnemyHealth) or can be placed in the level directly.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PowerUp : MonoBehaviour
{
    [Header("Type")]
    [SerializeField] private PowerUpType type = PowerUpType.Health;

    [Header("Health Power-Up")]
    [SerializeField] private string healthPickupMessage = "Full Health!";

    [Header("Strong Attack Power-Up")]
    [SerializeField] private float damageMultiplier = 2f;
    [SerializeField] private float buffDuration = 10f;
    [SerializeField] private string strongAttackPickupMessage = "Strong Attack Power-Up!";

    [Header("Effects")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private GameObject pickupEffect;

    [Header("Idle Animation")]
    [SerializeField] private float bobHeight = 0.25f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float spinSpeed = 90f;

    [Header("Despawn")]
    [Tooltip("Disappears on its own if left unpicked for this long.")]
    [SerializeField] private float despawnTime = 12f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Destroy(gameObject, despawnTime);
    }

    void Update()
    {
        // Simple floating + spinning idle animation so pickups read as interactive.
        float y = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, y, startPos.z);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        switch (type)
        {
            case PowerUpType.Health:
                PlayerHealth health = other.GetComponent<PlayerHealth>();
                if (health != null) health.FullHeal();
                if (UIManager.Instance != null) UIManager.Instance.ShowPickupMessage(healthPickupMessage);
                break;

            case PowerUpType.StrongAttack:
                PlayerCombat combat = other.GetComponent<PlayerCombat>();
                if (combat != null) combat.ApplyDamageBuff(damageMultiplier, buffDuration);
                if (UIManager.Instance != null) UIManager.Instance.ShowPickupMessage(strongAttackPickupMessage);
                break;
        }

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, GameSettings.SfxVolume);

        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
