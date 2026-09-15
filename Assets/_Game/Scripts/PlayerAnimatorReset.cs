using UnityEngine;

// Forces the Animator back to its default state (e.g. Idle) as soon as the scene starts,
// so the player never spawns mid-animation or holding a leftover pose from a previous run.
public class PlayerAnimatorReset : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Rebind();      // reset all parameters and go back to each layer's default state
            animator.Update(0f);    // immediately apply that reset pose instead of waiting a frame
        }
    }
}