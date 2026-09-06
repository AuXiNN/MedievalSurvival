using UnityEngine;
using TMPro;

/// <summary>
/// Gives a TMP title some life: a metallic vertical gradient, a bright "glint" that sweeps
/// across the letters every few seconds, a slow breathing brightness, a gentle float, a soft
/// drop shadow, and a one-time letter-by-letter reveal when it appears.
///
/// Drop it on the title's TextMeshPro object (or use
/// Tools > Medieval Survival > Add Title Effect). Everything is tunable in the Inspector;
/// zero out an amplitude/strength to switch that layer off.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
[DisallowMultipleComponent]
public class TitleTextEffect : MonoBehaviour
{
    [Header("Metallic gradient")]
    [SerializeField] private Color topColor = new Color(1f, 0.93f, 0.62f);
    [SerializeField] private Color bottomColor = new Color(0.79f, 0.47f, 0.06f);

    [Header("Glint sweep")]
    [SerializeField] private Color glintColor = Color.white;
    [Range(0.02f, 0.6f)] [SerializeField] private float glintWidth = 0.16f;   // fraction of the title's width
    [SerializeField] private float glintSpeed = 0.9f;                          // widths per second
    [SerializeField] private float glintInterval = 2.6f;                       // pause between sweeps (s)
    [Range(0f, 1f)] [SerializeField] private float glintStrength = 0.95f;

    [Header("Breathing brightness")]
    [Range(0f, 0.5f)] [SerializeField] private float breathAmount = 0.12f;
    [SerializeField] private float breathSpeed = 1.7f;

    [Header("Float")]
    [SerializeField] private float bobAmplitude = 5f;
    [SerializeField] private float bobSpeed = 1.1f;

    [Header("Reveal on show")]
    [SerializeField] private bool playReveal = true;
    [SerializeField] private float revealPerChar = 0.045f;
    [SerializeField] private float revealCharFade = 0.22f;

    [Header("Drop shadow")]
    [SerializeField] private bool dropShadow = true;
    [Range(0f, 1f)] [SerializeField] private float shadowStrength = 0.7f;

    private TMP_Text text;
    private RectTransform rt;
    private Vector2 basePos;
    private bool hasBasePos;
    private float time;
    private float revealStartTime;
    private Material matInstance;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        rt = (RectTransform)transform;
    }

    void OnEnable()
    {
        if (!hasBasePos)
        {
            basePos = rt.anchoredPosition;
            hasBasePos = true;
        }
        time = 0f;
        revealStartTime = Time.unscaledTime;

        if (dropShadow)
            ApplyDropShadow();
    }

    void OnDisable()
    {
        if (hasBasePos)
            rt.anchoredPosition = basePos;
    }

    private void ApplyDropShadow()
    {
        matInstance = text.fontMaterial; // TMP hands back a per-instance material
        if (matInstance == null || !matInstance.HasProperty(ShaderUtilities.ID_UnderlayColor))
            return;

        matInstance.EnableKeyword("UNDERLAY_ON");
        matInstance.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, shadowStrength));
        matInstance.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.75f);
        matInstance.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.75f);
        matInstance.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.4f);
    }

    void LateUpdate()
    {
        time += Time.unscaledDeltaTime;

        if (bobAmplitude > 0f && hasBasePos)
            rt.anchoredPosition = basePos + new Vector2(0f, Mathf.Sin(time * bobSpeed) * bobAmplitude);

        text.ForceMeshUpdate();
        TMP_TextInfo info = text.textInfo;
        if (info == null || info.characterCount == 0)
            return;

        // Title extent, for normalised horizontal position.
        float minX = float.MaxValue, maxX = float.MinValue;
        for (int i = 0; i < info.characterCount; i++)
        {
            if (!info.characterInfo[i].isVisible) continue;
            minX = Mathf.Min(minX, info.characterInfo[i].bottomLeft.x);
            maxX = Mathf.Max(maxX, info.characterInfo[i].topRight.x);
        }
        float width = Mathf.Max(0.0001f, maxX - minX);

        // Reveal timing.
        float revealTotal = playReveal ? info.characterCount * revealPerChar + revealCharFade : 0f;
        bool revealing = playReveal && (Time.unscaledTime - revealStartTime) < revealTotal;

        // Glint head sweeps -glintWidth .. 1+glintWidth, then rests off-screen for glintInterval.
        float sweepDuration = (1f + 2f * glintWidth) / Mathf.Max(0.01f, glintSpeed);
        float cycle = sweepDuration + glintInterval;
        float localT = Mathf.Repeat(time, cycle);
        float glintHead = localT < sweepDuration
            ? -glintWidth + localT * glintSpeed
            : 999f; // resting

        // Breathing multiplier on the whole title.
        float breath = 1f + Mathf.Sin(time * breathSpeed) * breathAmount;
        Color top = topColor * breath;
        Color bottom = bottomColor * breath;
        top.a = bottom.a = 1f;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo ci = info.characterInfo[i];
            if (!ci.isVisible) continue;

            Color32[] cols = info.meshInfo[ci.materialReferenceIndex].colors32;
            int v = ci.vertexIndex;

            float charLeft = (ci.bottomLeft.x - minX) / width;
            float charRight = (ci.topRight.x - minX) / width;

            float alpha = 1f;
            if (revealing)
            {
                float e = (Time.unscaledTime - revealStartTime - i * revealPerChar) / Mathf.Max(0.0001f, revealCharFade);
                alpha = Mathf.Clamp01(e);
            }

            // vertex order: 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right
            cols[v + 0] = Tint(bottom, charLeft, glintHead, alpha);
            cols[v + 1] = Tint(top, charLeft, glintHead, alpha);
            cols[v + 2] = Tint(top, charRight, glintHead, alpha);
            cols[v + 3] = Tint(bottom, charRight, glintHead, alpha);
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private Color32 Tint(Color baseColor, float xNorm, float glintHead, float alpha)
    {
        float d = Mathf.Abs(xNorm - glintHead);
        float g = d < glintWidth ? 1f - d / glintWidth : 0f;
        g = g * g * glintStrength; // sharpen the highlight
        Color c = Color.Lerp(baseColor, glintColor, g);
        c.a = alpha;
        return c;
    }
}
