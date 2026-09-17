using UnityEngine;

/// <summary>
/// Küçük easing yardımcıları (ek bağımlılık olmadan basit UI animasyonları için).
/// </summary>
public static class BKEase
{
    public static float OutQuad(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t); }
    public static float InQuad(float t) { t = Mathf.Clamp01(t); return t * t; }

    public static float OutBack(float t, float s = 1.70158f)
    {
        t = Mathf.Clamp01(t) - 1f;
        return t * t * ((s + 1f) * t + s) + 1f;
    }
}
