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

    private Transform player;
    private Rigidbody rb;
    private Animator animator;
    private bool canFire = true;
    private bool isMoving = false;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        if (playerObject != null)
            player = playerObject.transform;
        else
            Debug.LogError("ArcherAI: No GameObject with 'Player' tag found!");

        if (rb == null)
            Debug.LogError("ArcherAI: No Rigidbody found!");

        if (arrowPrefab == null)
            Debug.LogWarning("ArcherAI: No arrow prefab assigned - this archer can't fire.");
    }

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

        if (distance <= attackRange && canFire)
            Fire();

        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetBool("isRunning", isMoving);
    }

    private void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;
        Move(direction);
    }

    private void Move(Vector3 direction)
    {
        if (rb == null) return;
        Vector3 movement = direction * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }

    private void LookAtPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void Fire()
    {
        canFire = false;

        if (animator != null)
            animator.SetTrigger("Attack");

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

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + transform.forward + Vector3.up * 1.4f;
        Vector3 targetPos = player.position + Vector3.up * 1f; // aim roughly at chest height
        Vector3 direction = (targetPos - spawnPos).normalized;

        GameObject arrowGO = Instantiate(arrowPrefab, spawnPos, Quaternion.LookRotation(direction));
        Projectile projectile = arrowGO.GetComponent<Projectile>();
        if (projectile != null)
            projectile.Init(direction, projectileSpeed, projectileDamage);
    }

    private void ResetFire()
    {
        canFire = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
