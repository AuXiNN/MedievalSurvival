using UnityEngine;

/// <summary>
/// Receives the 'OnFootstep' and 'OnLand' Animation Events fired by the Walk_N/Run_N/
/// Run_N_Land animation clips and plays a sound for each. Without this, Unity logs an error
/// (and can trigger Console's "Error Pause") every time one of those events fires because it
/// has no receiver.
///
/// Attach this to the same GameObject that has the Animator (e.g. Solus_The_Knight).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour
{
    [SerializeField] private AudioClip[] footstepClips;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] private float landVolume = 0.8f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    // Called via SendMessage by the 'OnFootstep' Animation Event on the Walk_N/Run_N clips.
    public void OnFootstep(AnimationEvent animationEvent)
    {
        PlayRandomClip(volume);
    }

    // Called via SendMessage by the 'OnLand' Animation Event on the Run_N_Land clip.
    public void OnLand(AnimationEvent animationEvent)
    {
        PlayRandomClip(landVolume);
    }

    private void PlayRandomClip(float atVolume)
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.PlayOneShot(clip, atVolume);
    }
}
