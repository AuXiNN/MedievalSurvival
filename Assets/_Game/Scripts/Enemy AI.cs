using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f; // Changed from 3 to 5!
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 10;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 15f;

    [Header("Physics")]
    [Tooltip("Downward acceleration - matches the player's ThirdPersonController.Gravity so " +
             "enemies drop with the same weight instead of floating down.")]
    [SerializeField] private float gravity = -20f;

    // References
    private Transform player;
    private Rigidbody rb;
    private bool canAttack = true;         // false while the attack cooldown is running
    private bool playerDetected = false;   // mirrors hasSpottedPlayer, kept for readability/possible external checks
    private bool hasSpottedPlayer = false; // once true, chases forever regardless of distance
    private Animator animator;
    private EnemySound sound;
    private bool isMoving = false;         // drives the "isRunning" animator parameter

    // Finds the player and caches component references; configures the Rigidbody for
    // script-driven gravity instead of Unity's default.
    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        animator = GetComponent<Animator>();
        sound = GetComponent<EnemySound>();

        if (playerObject != null)
        {
            player = playerObject.transform;
            Debug.Log("Enemy: Player found!");
        }
        else
        {
            Debug.LogError("Enemy: No GameObject with 'Player' tag found!");
        }

        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Enemy: No Rigidbody found!");
        }
        else
        {
            // Apply gravity ourselves at the player's rate - the built-in Rigidbody gravity
            // (~-9.81) plus any drag made enemies drift down slowly and look weightless.
            rb.useGravity = false;
            rb.linearDamping = 0f;
        }
    }

    // Physics update loop - applies our own custom gravity every physics tick.
    void FixedUpdate()
    {
        if (rb == null || rb.isKinematic) return; // isKinematic == dead (see EnemyHealth.Die)
        rb.linearVelocity += Vector3.up * (gravity * Time.fixedDeltaTime);
    }

    // Main AI loop: detect the player, then chase and melee-attack once spotted.
    void Update()
    {
        if (player == null) return;

        isMoving = false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Once this enemy has seen the player it commits - it keeps chasing no matter how far
        // the player runs, instead of giving up at the edge of detectionRange.
        if (distanceToPlayer <= detectionRange && !hasSpottedPlayer)
        {
            hasSpottedPlayer = true;
            if (sound != null) sound.PlayBattleCry(); // "there he is!" the moment it engages
        }

        if (hasSpottedPlayer)
        {
            playerDetected = true;

            if (distanceToPlayer <= attackRange)
            {
                AttackPlayer();
            }
            else if (distanceToPlayer > stoppingDistance)
            {
                MoveTowardsPlayer();
                isMoving = true;
            }

            LookAtPlayer();
        }
        else
        {
            playerDetected = false;
        }

        UpdateAnimator();
    }

    // Syncs the run animation to whether the enemy is currently moving.
    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetBool("isRunning", isMoving);
    }

    // Sets velocity to walk straight toward the player (horizontal only, via the Rigidbody).
    private void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // ignore height difference so the enemy doesn't try to fly/dive

        Vector3 movement = direction * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }

    // Smoothly rotates the enemy to face the player.
    private void LookAtPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero) // avoid an invalid rotation if standing exactly on top of the player
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

// Stops the enemy and plays its attack animation, if the cooldown allows it.
private void AttackPlayer()
{
    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // stand still while attacking
    if (!canAttack) return;

    animator.SetTrigger("Attack");
    if (sound != null) sound.PlayAttackGrunt();

    // Delay damage to match animation hit frame
    Invoke(nameof(DealDamage), 0.5f);

    canAttack = false;
    Invoke(nameof(ResetAttack), attackCooldown);
}

// Called partway through the attack animation (via Invoke, timed to the swing) to actually
// apply damage - re-checks range in case the player ran away mid-swing.
private void DealDamage()
{
    if (player == null) return;
    float dist = Vector3.Distance(transform.position, player.position);
    if (dist <= attackRange) // Only damage if still in range
    {
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null) playerHealth.TakeDamage(attackDamage);
    }
}

    // Called after attackCooldown seconds have passed - allows the enemy to attack again.
    private void ResetAttack()
    {
        canAttack = true;
    }

    // Draws detection/attack/stopping ranges as wireframe spheres in the Scene view when selected.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
}