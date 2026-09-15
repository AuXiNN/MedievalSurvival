using UnityEngine;

/// <summary>
/// Every incidental sound an enemy knight makes, in one place: a battle cry when it first
/// engages, a grunt on each swing, armoured footsteps while it moves, and (archers only)
/// the bow draw / release. The AI scripts just call the Play* methods - clip assignment
/// and the 3-D audio settings live here.
///
/// Assign the clips on the enemy prefab. Any left empty are simply skipped.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class EnemySound : MonoBehaviour
{
    [Header("Voice")]
    [Tooltip("One-off shout the first time this knight engages the player.")]
    [SerializeField] private AudioClip battleCry;
    [Tooltip("Short exertion grunt on each attack swing / shot.")]
    [SerializeField] private AudioClip attackGrunt;

    [Header("Movement")]
    [Tooltip("Armoured footstep(s) - one is picked at random and played on a timer while moving.")]
    [SerializeField] private AudioClip[] footsteps;
    [Tooltip("Seconds between footsteps while running. Lower = faster cadence.")]
    [SerializeField] private float stepInterval = 0.26f;
    [Tooltip("Footsteps go quiet for this long after an attack starts, so the swing/shot reads clean.")]
    [SerializeField] private float attackFootstepSilence = 0.9f;

    [Header("Archer")]
    [Tooltip("Bow being drawn - played when the archer starts a shot. If your clip is a long " +
             "draw-and-release recording, set Bow Draw Seconds to just play the front of it.")]
    [SerializeField] private AudioClip bowDraw;
    [Tooltip("Only play this many seconds of the draw clip. 0 = play the whole thing.")]
    [SerializeField] private float bowDrawSeconds = 1f;
    [Tooltip("Bow-string release - played the instant the arrow flies.")]
    [SerializeField] private AudioClip bowRelease;

    [Header("Levels")]
    [Range(0f, 1f)] [SerializeField] private float voiceVolume = 0.9f;
    [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.25f;

    private AudioSource source;
    private Animator animator;
    private float stepTimer;
    private float footstepsSilencedUntil;
    private bool hasCried;

    private static readonly int RunningHash = Animator.StringToHash("isRunning");

    void Awake()
    {
        source = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();

        // Positional audio so the player can hear which direction enemies are coming from.
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 3f;
        source.maxDistance = 35f;
    }

    void Update()
    {
        // Footsteps: play one on a timer while the run animation is active - but not while
        // an attack is going, so the swing / shot isn't muddied by stomping.
        bool moving = animator != null && animator.GetBool(RunningHash);
        bool attacking = Time.time < footstepsSilencedUntil;
        if (!moving || attacking || footsteps == null || footsteps.Length == 0)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            stepTimer = stepInterval;
            AudioClip step = footsteps[Random.Range(0, footsteps.Length)];
            if (step != null)
                source.PlayOneShot(step, footstepVolume * GameSettings.SfxVolume);
        }
    }

    /// <summary>Battle cry - only ever plays once, the first time the knight engages.</summary>
    public void PlayBattleCry()
    {
        if (hasCried) return;
        hasCried = true;
        Play(battleCry, voiceVolume);
    }

    // Plays the exertion grunt on a melee swing and briefly silences footsteps for clarity.
    public void PlayAttackGrunt()
    {
        SilenceFootsteps();
        Play(attackGrunt, voiceVolume);
    }

    // Plays the bow-string release sound the instant an arrow is fired.
    public void PlayBowRelease() => Play(bowRelease, 0.8f);

    /// <summary>Mutes footsteps briefly - called whenever an attack begins.</summary>
    private void SilenceFootsteps() => footstepsSilencedUntil = Time.time + attackFootstepSilence;

    /// <summary>
    /// Plays the front of the draw clip and fades it out after bowDrawSeconds, on a throwaway
    /// AudioSource so it can be cut short without disturbing footsteps / grunts on the main one.
    /// </summary>
    public void PlayBowDraw()
    {
        SilenceFootsteps();
        if (bowDraw == null) return;

        float dur = bowDrawSeconds > 0f ? Mathf.Min(bowDrawSeconds, bowDraw.length) : bowDraw.length;

        GameObject go = new GameObject("BowDrawSFX");
        go.transform.position = transform.position;
        AudioSource s = go.AddComponent<AudioSource>();
        s.clip = bowDraw;
        s.volume = 0.8f * GameSettings.SfxVolume;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = source.minDistance;
        s.maxDistance = source.maxDistance;
        s.Play();

        Destroy(go, dur + 1f);            // hard cleanup even if the fade coroutine is interrupted
        StartCoroutine(FadeAndKill(s, dur));
    }

    private System.Collections.IEnumerator FadeAndKill(AudioSource s, float playFor)
    {
        const float fade = 0.12f;
        yield return new WaitForSeconds(Mathf.Max(0f, playFor - fade));

        float startVol = s.volume;
        for (float t = 0f; t < fade && s != null; t += Time.deltaTime)
        {
            s.volume = Mathf.Lerp(startVol, 0f, t / fade);
            yield return null;
        }
        if (s != null) Destroy(s.gameObject);
    }

    // Shared helper: plays a one-shot clip at the given base volume, scaled by the player's
    // SFX volume setting. Does nothing if the clip wasn't assigned.
    private void Play(AudioClip clip, float volume)
    {
        if (clip != null && source != null)
            source.PlayOneShot(clip, volume * GameSettings.SfxVolume);
    }
}
