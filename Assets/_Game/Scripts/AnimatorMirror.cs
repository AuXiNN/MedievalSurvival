using UnityEngine;

/// <summary>
/// Copies every parameter from a source Animator onto this GameObject's Animator, every frame.
///
/// The player has two separate Animator/skeleton setups layered on top of each other (a leftover
/// from combining the default StarterAssets rig with the Solus Knight model): the root
/// PlayerArmature's Animator is the one ThirdPersonController/PlayerCombat/PlayerBlock/PlayerHealth
/// actually drive, but it has no visible mesh attached anymore. The nested Solus_The_Knight
/// object's Animator is the one with the visible mesh, but nothing drives its parameters. This
/// mirrors the former onto the latter so the mesh you see actually animates.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimatorMirror : MonoBehaviour
{
    [SerializeField] private Animator source; // the "real" Animator that gameplay scripts drive

    private Animator target; // this object's own Animator (the one attached to the visible mesh)

    void Start()
    {
        target = GetComponent<Animator>();

        if (source == null)
            Debug.LogError("AnimatorMirror: no source Animator assigned!");
    }

    // Runs after Update, once per frame, so it copies the source's final values for this frame.
    void LateUpdate()
    {
        if (source == null || target == null) return; // nothing to mirror without both Animators

        // Triggers are NOT mirrored here: a trigger resets itself the instant it's consumed by
        // a transition on the source, which happens right after Update() and before this
        // LateUpdate ever runs - by the time we'd poll it, it already reads false. Scripts that
        // fire triggers on the source (PlayerCombat, PlayerBlock) forward them immediately via
        // SetTrigger()/ResetTrigger() below instead of relying on polling here.
        foreach (AnimatorControllerParameter p in source.parameters)
        {
            switch (p.type)
            {
                case AnimatorControllerParameterType.Float:
                    target.SetFloat(p.nameHash, source.GetFloat(p.nameHash));
                    break;
                case AnimatorControllerParameterType.Int:
                    target.SetInteger(p.nameHash, source.GetInteger(p.nameHash));
                    break;
                case AnimatorControllerParameterType.Bool:
                    target.SetBool(p.nameHash, source.GetBool(p.nameHash));
                    break;
            }
        }
    }

    /// <summary>Forwards a trigger to the mirrored (visible) Animator. Call this alongside any
    /// source.SetTrigger(hash) so the visible mesh's Animator gets it in the same frame.</summary>
    public void SetTrigger(int hash)
    {
        if (target == null) target = GetComponent<Animator>();
        target.SetTrigger(hash);
    }

    /// <summary>Forwards a trigger reset to the mirrored (visible) Animator.</summary>
    public void ResetTrigger(int hash)
    {
        if (target == null) target = GetComponent<Animator>();
        target.ResetTrigger(hash);
    }
}
