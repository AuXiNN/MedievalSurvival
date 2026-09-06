using UnityEngine;

/// <summary>
/// A simple straight-line projectile (arrow/bolt) fired by ArcherAI.
/// Flies in the direction it was initialized with and damages the player on hit.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float defaultSpeed = 18f;
    [SerializeField] private int defaultDamage = 12;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private GameObject hitEffect;

    private float speed;
    private int damage;
    private Vector3 direction = Vector3.forward;
    private bool initialized;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // The player uses a CharacterController, not a Rigidbody. Unity only sends trigger
        // callbacks between two colliders if at least one of them has a Rigidbody (a
        // CharacterController only satisfies that when IT'S the one moving into a trigger,
        // which is the reverse of this case) - without this, the arrow's trigger vs. the
        // player's CharacterController is never even checked for overlap. Kinematic since we
        // move this via script, not physics; ContinuousSpeculative so a fast-moving arrow can't
        // tunnel straight through in a single frame.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        speed = defaultSpeed;
        damage = defaultDamage;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    /// <summary>Called by ArcherAI right after Instantiate to aim and configure this shot.</summary>
    public void Init(Vector3 travelDirection, float projectileSpeed, int projectileDamage)
    {
        direction = travelDirection.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
        initialized = true;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    void Update()
    {
        Vector3 moveDir = initialized ? direction : transform.forward;
        transform.position += moveDir * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage(damage);

            ImpactAndDestroy();
            return;
        }

        // Ignore other triggers (pickups, other projectiles) but stop on solid geometry/enemies.
        if (!other.isTrigger && !other.CompareTag("Enemy"))
        {
            ImpactAndDestroy();
        }
    }

    private void ImpactAndDestroy()
    {
        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, transform.position, GameSettings.SfxVolume);

        if (hitEffect != null)
            Instantiate(hitEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
