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

    private float speed;                              // current travel speed, set by Init() or the default above
    private int damage;                                // damage dealt to the player on a hit
    private Vector3 direction = Vector3.forward;       // normalized flight direction
    private bool initialized;                          // true once Init() has set a real aimed direction

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
        Destroy(gameObject, lifeTime); // auto-cleanup if the arrow never hits anything
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

    // Moves the arrow forward every frame, sweeping a ray across this frame's travel distance
    // first. A plain `transform.position +=` with only OnTriggerEnter to catch hits lets a fast
    // arrow skip clean over a thin or slightly-misaligned environment collider in a single frame,
    // since the overlap used to only ever get checked at the arrow's end-of-frame position, never
    // along the path it travelled to get there.
    void Update()
    {
        Vector3 moveDir = initialized ? direction : transform.forward; // fall back to forward if never aimed
        float distance = speed * Time.deltaTime;

        if (Physics.Raycast(transform.position, moveDir, out RaycastHit hit, distance) &&
            TryHandleHit(hit.collider, hit.point))
            return; // arrow was destroyed by the hit, nothing left to move

        transform.position += moveDir * distance;
    }

    // Fallback for the rare case something overlaps the arrow without the sweep ray above
    // catching it (e.g. it spawns already inside a collider).
    private void OnTriggerEnter(Collider other)
    {
        TryHandleHit(other, transform.position);
    }

    // Shared hit logic for both the sweep ray and the trigger fallback.
    // Returns true if the arrow was destroyed as a result.
    private bool TryHandleHit(Collider other, Vector3 hitPoint)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage(damage); // apply the hit

            ImpactAndDestroy(hitPoint);
            return true;
        }

        // Ignore other triggers (pickups, other projectiles) and enemies (no friendly fire),
        // but stop on solid environment geometry.
        if (!other.isTrigger && !other.CompareTag("Enemy"))
        {
            ImpactAndDestroy(hitPoint);
            return true;
        }

        return false;
    }

    // Shared "the arrow just hit something" cleanup: play feedback, then remove the arrow.
    private void ImpactAndDestroy(Vector3 hitPoint)
    {
        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, hitPoint, GameSettings.SfxVolume);

        if (hitEffect != null)
            Instantiate(hitEffect, hitPoint, Quaternion.identity);

        Destroy(gameObject);
    }
}
