using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

/// <summary>
/// Shield block: hold the block button to reduce incoming damage and raise the
/// shield-block animation. PlayerHealth checks IsBlocking to reduce damage,
/// PlayerCombat checks it to stop you swinging while your shield is up, and
/// ThirdPersonController movement is frozen for as long as the shield is up.
/// </summary>
public class PlayerBlock : MonoBehaviour
{
    [Header("Block Settings")]
    [Tooltip("0 = no reduction, 1 = fully blocks all damage.")]
    [Range(0f, 1f)] [SerializeField] private float damageReduction = 0.8f;

    private Animator animator;
    private PlayerCombat playerCombat;
    private ThirdPersonController thirdPersonController;
    private static readonly int BlockingHash = Animator.StringToHash("Blocking");
    private static readonly int BlockHitHash = Animator.StringToHash("BlockHit");

    public bool IsBlocking { get; private set; }

    void Start()
    {
        animator = GetComponent<Animator>();
        playerCombat = GetComponent<PlayerCombat>();
        thirdPersonController = GetComponent<ThirdPersonController>();
    }

    void Update()
    {
        // Shield stays down while mid-swing until the attack is far enough along to be
        // cancelled (PlayerCombat.CanRaiseShield). Once it can come up during an attack,
        // raising it also cuts the swing short so you can act again immediately.
        // Can only block with both feet on the ground - no shield mid-jump/fall.
        bool rmbHeld = Mouse.current != null && Mouse.current.rightButton.isPressed;
        bool grounded = thirdPersonController == null || thirdPersonController.Grounded;
        bool wantsToBlock = rmbHeld && grounded && (playerCombat == null || playerCombat.CanRaiseShield);

        if (wantsToBlock != IsBlocking)
        {
            if (wantsToBlock && playerCombat != null && playerCombat.IsAttacking)
                playerCombat.CancelAttack();

            IsBlocking = wantsToBlock;
            if (animator != null)
                animator.SetBool(BlockingHash, IsBlocking);
        }

        // Freeze walking / running / jumping for as long as the shield is up.
        if (thirdPersonController != null)
            thirdPersonController.MovementLocked = IsBlocking;
    }

    void OnDisable()
    {
        // Never leave the player frozen if this component is switched off mid-block.
        IsBlocking = false;
        if (thirdPersonController != null)
            thirdPersonController.MovementLocked = false;
    }

    /// <summary>
    /// Instantly drops the shield and its animation. Called by PlayerCombat the moment you
    /// attack, so the block pose doesn't linger while you swing.
    /// </summary>
    public void CancelBlock()
    {
        if (!IsBlocking) return;

        IsBlocking = false;
        if (animator != null)
            animator.SetBool(BlockingHash, false);

        if (thirdPersonController != null)
            thirdPersonController.MovementLocked = false;
    }

    /// <summary>
    /// Applies the block reduction to an incoming damage amount. Called by PlayerHealth.
    /// Also plays the shield-hit reaction animation if currently blocking.
    /// </summary>
    public int ModifyIncomingDamage(int damage)
    {
        if (!IsBlocking) return damage;

        if (animator != null)
            animator.SetTrigger(BlockHitHash);

        return Mathf.RoundToInt(damage * (1f - damageReduction));
    }
}
