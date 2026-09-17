using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 9:16 tasarım alanının (21.6 x 38.4 birim) ekrana tamamen sığmasını sağlar (contain).
/// Farklı oranlı ekranlarda kalan boşluk kamera arka plan rengiyle (BK kırmızısı) dolar.
/// Canvas'lar da aynı kurala göre eşlenir (genişse yüksekliğe, darsa genişliğe) ki UI ile dünya hizası bozulmasın.
/// </summary>
[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class BKCameraFitter : MonoBehaviour
{
    public float designWidth = 21.6f;
    public float designHeight = 38.4f;

    [Tooltip("Sahnedeki CanvasScaler'ların matchWidthOrHeight değerini aynı sığdırma kuralına göre ayarla")]
    public bool syncCanvasScalers = true;

    private Camera cam;
    private int lastW, lastH;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        Fit();
    }

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH) Fit();
    }

    void Fit()
    {
        if (!cam) cam = GetComponent<Camera>();
        lastW = Screen.width;
        lastH = Screen.height;
        if (lastH <= 0 || lastW <= 0) return;

        float aspect = (float)lastW / lastH;
        float designAspect = designWidth / designHeight;
        float sizeByHeight = designHeight * 0.5f;
        float sizeByWidth = (designWidth * 0.5f) / aspect;

        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);

        if (!syncCanvasScalers) return;

        // Ekran tasarımdan genişse yükseklik tam sığar (match=1), darsa genişlik tam sığar (match=0)
        float match = aspect >= designAspect ? 1f : 0f;
        CanvasScaler[] scalers = FindObjectsOfType<CanvasScaler>(true);
        for (int i = 0; i < scalers.Length; i++)
        {
            if (scalers[i].uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                scalers[i].matchWidthOrHeight = match;
        }
    }
}
