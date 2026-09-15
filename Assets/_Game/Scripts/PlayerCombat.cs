using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class PlayerCombat : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip attackSound; // whoosh sound
    private AudioSource audioSource;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 0.8f;
    [SerializeField] private float attackRadius = 1.5f;
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private Transform attackPoint;

    [Header("Animation Settings")]
    [SerializeField] private float attackAnimationSpeed = 1.5f;

    [Tooltip("How far through the Attack animation (0-1) you're allowed to cancel the swing " +
             "by raising the shield. 0.464 ≈ frame 64 of the clip.")]
    [Range(0f, 1f)] [SerializeField] private float blockCancelPoint = 0.464f;

    // References
    private Animator animator;
    private ThirdPersonController thirdPersonController;
    private PlayerBlock playerBlock;
    private AnimatorMirror[] mirrors;
    private bool canAttack = true;
    private bool hitEnemy = false;
    private bool canPlayWhoosh = true;

    // Power-ups / inventory weapons feed into these instead of touching attackDamage directly.
    private float damageMultiplier = 1f;
    private int damageBonus = 0;
    private Coroutine damageBuffRoutine;

    public bool IsStrongAttackActive => damageMultiplier > 1f;
    public bool IsAttacking => !canAttack;

    private const int CombatLayer = 1;

    /// <summary>
    /// True when the player is free to raise the shield: either not swinging, or far enough
    /// through the Attack animation (blockCancelPoint) that the swing can be cancelled.
    /// PlayerBlock checks this before letting the shield come up.
    /// </summary>
    public bool CanRaiseShield
    {
        get
        {
            if (canAttack) return true;
            if (animator == null) return true;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(CombatLayer);
            return state.IsName("Attack") && state.normalizedTime >= blockCancelPoint;
        }
    }

    /// <summary>
    /// Ends the current swing early so the player can act again immediately. Called by
    /// PlayerBlock when a shield-raise cancels the attack past blockCancelPoint.
    /// </summary>
    public void CancelAttack()
    {
        if (canAttack) return;

        CancelInvoke(nameof(DealDamage));
        CancelInvoke(nameof(PlayWhoosh));
        CancelInvoke(nameof(ResetAttack));

        canAttack = true;
        hitEnemy = false;
    }

    // The Combat Layer now has an Any State -> Attack transition (0s duration, no exit time)
    // driven by the "Attack" trigger, so a swing cuts straight into the Attack state from
    // anything - Block, BlockHit, mid-transition or idle - on the very next frame. No more
    // waiting for the block animation to relay through Empty first.
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    // Animation hashes
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");

    // Caches references to everything this script needs from sibling components.
    void Start()
    {
        animator = GetComponent<Animator>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        playerBlock = GetComponent<PlayerBlock>();
        audioSource = GetComponent<AudioSource>();
        mirrors = GetComponentsInChildren<AnimatorMirror>(true);

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (animator == null)
            Debug.LogError("PlayerCombat: No Animator found!");

        Debug.Log("PlayerCombat initialized - Press LEFT CLICK or E to attack!");
    }

    // Listens for the attack input (left click or E) every frame.
    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryAttack();

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            TryAttack();
    }

    /// <summary>
    /// Checks if the player is on the ground
    /// </summary>
    private bool IsGrounded()
    {
        if (thirdPersonController != null)
            return thirdPersonController.Grounded;

        if (animator != null)
            return animator.GetBool(GroundedHash);

        return true;
    }

    /// <summary>
    /// Attempts to perform an attack if conditions are met
    /// </summary>
    private void TryAttack()
    {
        if (!canAttack) return;

        if (!IsGrounded())
        {
            Debug.Log("Can't attack while jumping!");
            return;
        }

        // Attacking always wins over blocking - drop the shield instantly instead of
        // refusing the swing, so there's no waiting around for the block animation.
        if (playerBlock != null)
            playerBlock.CancelBlock();

        canAttack = false;
        hitEnemy = false;

        // Speed up animation
        animator.SetFloat("AttackSpeed", attackAnimationSpeed);

        // Fire the attack trigger - the Any State -> Attack transition on the Combat Layer
        // cuts over instantly regardless of whether we were just blocking, mid-swing, or idle.
        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(AttackHash);

        // Triggers self-reset before AnimatorMirror's LateUpdate can poll them, so forward
        // this one directly to the visible mesh's Animator right now instead.
        foreach (AnimatorMirror mirror in mirrors)
        {
            mirror.ResetTrigger(AttackHash);
            mirror.SetTrigger(AttackHash);
        }

        Debug.Log("Attack!");

        // Cancel any previous invokes first!
        CancelInvoke(nameof(DealDamage));
        CancelInvoke(nameof(PlayWhoosh));
        CancelInvoke(nameof(ResetAttack));

        // Deal damage first then check whoosh
        Invoke(nameof(DealDamage), 0.33f); // check enemy hit at frame 15
        Invoke(nameof(PlayWhoosh), 0.36f); // slightly after to check hitEnemy
        Invoke(nameof(ResetAttack), attackCooldown);
    }

    /// <summary>
    /// Plays whoosh ONLY if no enemy was hit and cooldown is ready
    /// </summary>
    private void PlayWhoosh()
    {
        if (!hitEnemy && attackSound != null && audioSource != null && canPlayWhoosh)
        {
            audioSource.PlayOneShot(attackSound, 0.7f * GameSettings.SfxVolume);
            canPlayWhoosh = false;
            Invoke(nameof(ResetWhoosh), 1.2f); // 2 second cooldown
        }
    }

    /// <summary>
    /// Resets whoosh cooldown
    /// </summary>
    private void ResetWhoosh()
    {
        canPlayWhoosh = true;
    }

    /// <summary>
    /// Resets the ability to attack
    /// </summary>
    private void ResetAttack()
    {
        canAttack = true;
        hitEnemy = false;
    }

    /// <summary>
    /// Deals damage to every enemy inside the swing radius - the mace sweeps, so two enemies
    /// stacked on the player both get hit. Each enemy is only damaged once per swing even if
    /// it has more than one collider in range.
    /// </summary>
    public void DealDamage()
    {
        Vector3 origin = attackPoint != null
            ? attackPoint.position
            : transform.position + transform.forward + Vector3.up;

        Collider[] hits = Physics.OverlapSphere(origin, attackRadius);

        HashSet<EnemyHealth> alreadyDamaged = new HashSet<EnemyHealth>();
        int finalDamage = Mathf.RoundToInt((attackDamage + damageBonus) * damageMultiplier);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null || !alreadyDamaged.Add(enemyHealth)) continue;

            hitEnemy = true;

            // No hit sound here - EnemyHealth.TakeDamage() below plays its own hitSound.
            enemyHealth.TakeDamage(finalDamage);
        }
    }

    /// <summary>
    /// Temporarily multiplies attack damage - used by the Strong Attack power-up.
    /// Stacking pickups refreshes the duration rather than stacking the multiplier.
    /// </summary>
    public void ApplyDamageBuff(float multiplier, float duration)
    {
        if (damageBuffRoutine != null) StopCoroutine(damageBuffRoutine);
        damageBuffRoutine = StartCoroutine(DamageBuffRoutine(multiplier, duration));
    }

    // Runs for the buff's duration: keeps the UI timer updated every frame, then reverts
    // the damage multiplier back to normal once time runs out.
    private System.Collections.IEnumerator DamageBuffRoutine(float multiplier, float duration)
    {
        damageMultiplier = multiplier;
        Debug.Log($"Strong Attack active! Damage x{multiplier} for {duration}s");

        if (UIManager.Instance != null)
            UIManager.Instance.ShowStrongAttackTimer(duration);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (UIManager.Instance != null)
                UIManager.Instance.UpdateStrongAttackTimer(1f - elapsed / duration);
            yield return null;
        }

        damageMultiplier = 1f;
        damageBuffRoutine = null;

        if (UIManager.Instance != null)
            UIManager.Instance.HideStrongAttackTimer();

        Debug.Log("Strong Attack wore off.");
    }

    /// <summary>
    /// Cancels the Strong Attack buff right now and hides its UI timer. Called on
    /// death / restart so a buff picked up in one run never carries into the next.
    /// </summary>
    public void ClearDamageBuff()
    {
        if (damageBuffRoutine != null)
        {
            StopCoroutine(damageBuffRoutine);
            damageBuffRoutine = null;
        }
        damageMultiplier = 1f;

        if (UIManager.Instance != null)
            UIManager.Instance.HideStrongAttackTimer();
    }

    /// <summary>
    /// Permanent flat damage bonus - used by inventory weapon pickups.
    /// </summary>
    public void AddDamageBonus(int amount)
    {
        damageBonus += amount;
    }

    /// <summary>
    /// Shows attack range in Scene view
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = attackPoint != null
            ? attackPoint.position
            : transform.position + transform.forward + Vector3.up;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, attackRadius);
    }
}
