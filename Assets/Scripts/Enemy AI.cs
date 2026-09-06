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

    // References
    private Transform player;
    private Rigidbody rb;
    private bool canAttack = true;
    private bool playerDetected = false;
    private Animator animator;
    private bool isMoving = false;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        animator = GetComponent<Animator>();

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
    }

    void Update()
    {
        if (player == null) return;

        isMoving = false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
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

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetBool("isRunning", isMoving);
    }

    private void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        Vector3 movement = direction * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }

    private void LookAtPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

private void AttackPlayer()
{
    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
    if (!canAttack) return;

    animator.SetTrigger("Attack");
    
    // Delay damage to match animation hit frame
    Invoke(nameof(DealDamage), 0.5f);
    
    canAttack = false;
    Invoke(nameof(ResetAttack), attackCooldown);
}

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

    private void ResetAttack()
    {
        canAttack = true;
    }

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