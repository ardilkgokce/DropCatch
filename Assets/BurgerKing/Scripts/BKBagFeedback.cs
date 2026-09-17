using System.Collections;
using UnityEngine;

/// <summary>
/// Sepet (BK paketi) görsel geri bildirimi: yakalamada ölçek "punch", bombada kırmızı flaş + sarsıntı.
/// X ekseni BasketController2D tarafından yönetildiği için sarsıntı yalnızca Y ekseninde yapılır.
/// </summary>
public class BKBagFeedback : MonoBehaviour
{
    public SpriteRenderer bagRenderer;

    [Header("Yakalama")]
    public float punchScale = 1.12f;
    public float punchDuration = 0.2f;

    [Header("Bomba")]
    public Color bombTint = new Color(1f, 0.45f, 0.4f, 1f);
    public float bombShakeAmplitude = 0.25f;
    public float bombDuration = 0.45f;

    private Vector3 baseScale;
    private Color baseColor = Color.white;
    private Coroutine punchRoutine;
    private Coroutine bombRoutine;
    private float restY;          // sarsıntı başlamadan önceki gerçek Y
    private bool hasRestY;

    void Awake()
    {
        baseScale = transform.localScale;
        if (!bagRenderer) bagRenderer = GetComponent<SpriteRenderer>();
        if (bagRenderer) baseColor = bagRenderer.color;
    }

    public void PlayCatch(float strength = 1f)
    {
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(PunchRoutine(strength));
    }

    public void PlayBombHit()
    {
        if (bombRoutine != null)
        {
            StopCoroutine(bombRoutine);
            RestoreRest(); // üst üste bombalarda sarsıntı ofseti birikmesin
        }
        bombRoutine = StartCoroutine(BombRoutine());
        PlayCatch(0.6f);
    }

    void RestoreRest()
    {
        if (hasRestY)
        {
            Vector3 p = transform.position;
            p.y = restY;
            transform.position = p;
            hasRestY = false;
        }
        if (bagRenderer) bagRenderer.color = baseColor;
    }

    IEnumerator PunchRoutine(float strength)
    {
        // strength > 1 daha güçlü punch (menü tamamlama), Lerp'in 0-1 sınırına takılmasın
        float peak = Mathf.LerpUnclamped(1f, punchScale, Mathf.Clamp(strength, 0.1f, 3f));
        float t = 0f;
        while (t < punchDuration)
        {
            t += Time.deltaTime;
            float k = t / punchDuration;
            float s = k < 0.4f
                ? Mathf.Lerp(1f, peak, BKEase.OutQuad(k / 0.4f))
                : Mathf.Lerp(peak, 1f, BKEase.OutQuad((k - 0.4f) / 0.6f));
            transform.localScale = baseScale * s;
            yield return null;
        }
        transform.localScale = baseScale;
        punchRoutine = null;
    }

    IEnumerator BombRoutine()
    {
        if (!hasRestY)
        {
            restY = transform.position.y;
            hasRestY = true;
        }

        float t = 0f;
        while (t < bombDuration)
        {
            t += Time.deltaTime;
            float k = t / bombDuration;
            float amp = bombShakeAmplitude * (1f - k);
            Vector3 p = transform.position;
            p.y = restY + Mathf.Sin(t * 60f) * amp;
            transform.position = p;
            if (bagRenderer) bagRenderer.color = Color.Lerp(bombTint, baseColor, k);
            yield return null;
        }

        RestoreRest();
        bombRoutine = null;
    }
}
