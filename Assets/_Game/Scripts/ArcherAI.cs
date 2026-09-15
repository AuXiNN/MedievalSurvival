using UnityEngine;

/// <summary>
/// Ranged enemy behaviour: fires at the player from anywhere within attackRange, closing
/// the distance only if the player is further than that - it never retreats, even at close
/// range, and never melee-attacks. Pairs with EnemyHealth exactly like EnemyAI does.
/// </summary>
public class ArcherAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Ranged Combat Settings")]
    [Tooltip("Maximum distance the archer will shoot from. Beyond this it moves closer; within it, it just shoots (never retreats).")]
    [SerializeField] private float attackRange = 36f; // doubled - lets the archer engage from much further back
    [SerializeField] private float fireCooldown = 2f;
    [SerializeField] private int projectileDamage = 12;
    [SerializeField] private float projectileSpeed = 32f; // fast - a real threat at range, not a lob

    [Header("References")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Physics")]
    [Tooltip("Downward acceleration - matches the player's ThirdPersonController.Gravity so " +
             "archers drop with the same weight instead of floating down.")]
    [SerializeField] private float gravity = -20f;

    private Transform player;          // cached reference to the player's transform
    private Rigidbody rb;               // used to move the archer via velocity
    private Animator animator;
    private EnemySound sound;
    private bool canFire = true;        // false while the fire cooldown is running
    private bool isMoving = false;      // drives the "isRunning" animator parameter
    private bool hasEngaged = false;    // true once the archer has spotted/engaged the player (plays battle cry once)

    // Finds the player and caches component references; configures the Rigidbody for
    // script-driven gravity instead of Unity's default.
    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        sound = GetComponent<EnemySound>();

        if (playerObject != null)
            player = playerObject.transform;
        else
            Debug.LogError("ArcherAI: No GameObject with 'Player' tag found!");

        if (rb == null)
        {
            Debug.LogError("ArcherAI: No Rigidbody found!");
        }
        else
        {
            // Apply gravity ourselves at the player's rate - the built-in Rigidbody gravity
            // (~-9.81) plus any drag made archers drift down slowly and look weightless.
            rb.useGravity = false;
            rb.linearDamping = 0f;
        }

        if (arrowPrefab == null)
            Debug.LogWarning("ArcherAI: No arrow prefab assigned - this archer can't fire.");
    }

    // Physics update loop - applies our own custom gravity every physics tick.
    void FixedUpdate()
    {
        if (rb == null || rb.isKinematic) return; // isKinematic == dead (see EnemyHealth.Die)
        rb.linearVelocity += Vector3.up * (gravity * Time.fixedDeltaTime);
    }

    // Main AI loop: face the player, close the distance if too far, otherwise hold position
    // and shoot.
    void Update()
    {
        if (player == null) return;

        isMoving = false;
        float distance = Vector3.Distance(transform.position, player.position);

        LookAtPlayer();

        if (distance > attackRange)
        {
            MoveTowardsPlayer();
            isMoving = true;
        }
        else if (rb != null)
        {
            // Within range - hold position and shoot. Never backs away, even at close range.
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        if (distance <= attackRange)
        {
            if (!hasEngaged)
            {
                hasEngaged = true;
                if (sound != null) sound.PlayBattleCry(); // shout as it draws on the player
            }
            if (canFire) Fire();
        }

        UpdateAnimator();
    }

    // Syncs the run animation to whether the archer is currently moving.
    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetBool("isRunning", isMoving);
    }

    // Walks straight toward the player's current position (horizontal only).
    private void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f; // ignore height difference so the archer doesn't try to fly/dive
        Move(direction);
    }

    // Sets horizontal velocity in the given direction while leaving vertical velocity
    // (gravity/falling) untouched.
    private void Move(Vector3 direction)
    {
        if (rb == null) return;
        Vector3 movement = direction * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }

    // Smoothly rotates the archer to face the player.
    private void LookAtPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero) // avoid an invalid rotation if standing exactly on top of the player
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    // Starts the shoot animation and begins the fire cooldown. The arrow itself spawns later,
    // via ReleaseArrow() below.
    private void Fire()
    {
        canFire = false;

        if (animator != null)
            animator.SetTrigger("Attack");
        if (sound != null) sound.PlayBowDraw();

        Invoke(nameof(ResetFire), fireCooldown);

        // The arrow itself is NOT spawned here - see ReleaseArrow() below. It's called by an
        // Animation Event placed on the shoot animation's exact release frame (set up by
        // Tools > Medieval Survival > Build Archer Enemy), so the arrow always appears in sync
        // with the hand letting go of the string, however long the animation actually takes.
    }

    /// <summary>
    /// Called via an Animation Event at the exact moment the bow hand releases the arrow.
    /// Do not call this on a timer - it needs to stay locked to the animation.
    /// </summary>
    public void ReleaseArrow()
    {
        if (arrowPrefab == null || player == null) return;

        // Use the dedicated fire point if one is assigned, otherwise spawn roughly at hand height.
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + transform.forward + Vector3.up * 1.4f;
        Vector3 targetPos = player.position + Vector3.up * 1f; // aim roughly at chest height
        Vector3 direction = (targetPos - spawnPos).normalized;

        GameObject arrowGO = Instantiate(arrowPrefab, spawnPos, Quaternion.LookRotation(direction));
        Projectile projectile = arrowGO.GetComponent<Projectile>();
        if (projectile != null)
            projectile.Init(direction, projectileSpeed, projectileDamage);

        if (sound != null) sound.PlayBowRelease();
    }

    // Called after fireCooldown seconds have passed - allows the archer to shoot again.
    private void ResetFire()
    {
        canFire = true;
    }

    // Draws the attack range as a wireframe sphere in the Scene view when this enemy is selected.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
