using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Oyun içi arayüz: skor, süre, menü sayacı, menü takip ikonları, geri sayım, bekleme uyarısı,
/// "+1 MENÜ" animasyonu, bomba flaşı ve uçan puan yazıları.
/// </summary>
public class BKHudUI : MonoBehaviour
{
    [Header("Kök")]
    public GameObject root;

    [Header("Üst Bilgi")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI menuCountText;
    public TextMeshProUGUI comboText;
    public Color timeNormalColor = new Color(0.1f, 0.09f, 0.08f, 1f);
    public Color timeWarningColor = new Color(0.82f, 0.15f, 0.12f, 1f);

    [Header("Menü Takip (her slot bir ürün)")]
    public Image[] trackerSlots;
    public Image[] trackerIcons;
    public BKItemType[] trackerTypes;
    public Color slotDimColor = new Color(0.91f, 0.86f, 0.78f, 1f);
    public Color slotLitColor = new Color(1f, 0.82f, 0.25f, 1f);
    public Color iconDimColor = new Color(1f, 1f, 1f, 0.28f);
    public Color iconLitColor = Color.white;

    [Header("Menü Tamamlandı Animasyonu")]
    public RectTransform menuPopup;
    public CanvasGroup menuPopupGroup;
    public TextMeshProUGUI menuPopupText;
    public float menuPopupDuration = 1.6f;

    [Header("Geri Sayım / Bekleme")]
    public GameObject countdownRoot;
    public TextMeshProUGUI countdownText;
    public GameObject waitingRoot;
    public TextMeshProUGUI waitingText;

    [Header("Bomba Flaşı")]
    public CanvasGroup bombFlash;
    public float bombFlashAlpha = 0.45f;
    public float bombFlashDuration = 0.35f;

    [Header("Uçan Yazı")]
    public BKFloatingText floatingTextPrefab;
    public Transform floatingTextParent;
    public Color gainColor = new Color(0.1f, 0.09f, 0.08f, 1f);
    public Color penaltyColor = new Color(0.82f, 0.15f, 0.12f, 1f);

    private Coroutine popupRoutine;
    private Coroutine countdownRoutine;
    private Coroutine flashRoutine;
    private Vector3 countdownBaseScale = Vector3.one;

    void Awake()
    {
        if (countdownText) countdownBaseScale = countdownText.rectTransform.localScale;
    }

    public void Show()
    {
        if (root) root.SetActive(true);
        HideCountdown();
        ShowWaiting(false);
        if (menuPopupGroup) menuPopupGroup.alpha = 0f;
        if (bombFlash) bombFlash.alpha = 0f;
        if (comboText) comboText.gameObject.SetActive(false);
    }

    public void Hide()
    {
        if (root) root.SetActive(false);
    }

    public void SetScore(int score)
    {
        if (scoreText) scoreText.text = "SKOR  " + score;
    }

    public void SetTime(float seconds, bool warning)
    {
        if (!timeText) return;
        int s = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        timeText.text = "SÜRE  " + s;
        timeText.color = warning ? timeWarningColor : timeNormalColor;
    }

    public void SetMenuCount(int count)
    {
        if (menuCountText) menuCountText.text = "×" + count;
    }

    public void SetCombo(int multiplier)
    {
        if (!comboText) return;
        comboText.gameObject.SetActive(multiplier > 1);
        comboText.text = "x" + multiplier + " KOMBO";
    }

    /// <summary>Menü için toplanan ürünleri ikonlarda aydınlatır; gerekli listede olmayan slotları gizler.</summary>
    public void SetTracker(ICollection<BKItemType> collected, IList<BKItemType> required)
    {
        if (trackerIcons == null || trackerTypes == null) return;

        int n = Mathf.Min(trackerIcons.Length, trackerTypes.Length);
        for (int i = 0; i < n; i++)
        {
            BKItemType type = trackerTypes[i];
            bool relevant = required != null && required.Contains(type);
            bool lit = relevant && collected != null && collected.Contains(type);

            Image slot = trackerSlots != null && i < trackerSlots.Length ? trackerSlots[i] : null;
            if (slot)
            {
                slot.gameObject.SetActive(relevant);
                slot.color = lit ? slotLitColor : slotDimColor;
            }
            else if (trackerIcons[i])
            {
                trackerIcons[i].gameObject.SetActive(relevant);
            }

            if (trackerIcons[i]) trackerIcons[i].color = lit ? iconLitColor : iconDimColor;
        }
    }

    public void ShowFloatingText(Vector3 worldPos, string text, Color color)
    {
        if (!floatingTextPrefab) return;
        BKFloatingText ft = Instantiate(floatingTextPrefab, worldPos, Quaternion.identity, floatingTextParent);
        ft.Play(text, color);
    }

    public void PlayMenuComplete(int menuCount)
    {
        if (!menuPopup || !menuPopupGroup) return;
        if (menuPopupText) menuPopupText.text = "+1 MENÜ!";
        if (popupRoutine != null) StopCoroutine(popupRoutine);
        popupRoutine = StartCoroutine(MenuPopupRoutine());
    }

    IEnumerator MenuPopupRoutine()
    {
        float t = 0f;
        float inDur = 0.35f;
        float outDur = 0.4f;
        float hold = Mathf.Max(0f, menuPopupDuration - inDur - outDur);

        menuPopupGroup.alpha = 1f;
        while (t < inDur)
        {
            t += Time.deltaTime;
            float k = BKEase.OutBack(t / inDur);
            menuPopup.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, k);
            yield return null;
        }
        menuPopup.localScale = Vector3.one;

        yield return new WaitForSeconds(hold);

        t = 0f;
        while (t < outDur)
        {
            t += Time.deltaTime;
            float k = t / outDur;
            menuPopupGroup.alpha = 1f - k;
            menuPopup.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, k);
            yield return null;
        }
        menuPopupGroup.alpha = 0f;
        popupRoutine = null;
    }

    public void ShowCountdown(string text)
    {
        if (!countdownRoot) return;
        countdownRoot.SetActive(true);
        if (countdownText) countdownText.text = text;
        if (countdownRoutine != null) StopCoroutine(countdownRoutine);
        countdownRoutine = StartCoroutine(CountdownPunch());
    }

    IEnumerator CountdownPunch()
    {
        if (!countdownText) yield break;
        float t = 0f;
        const float dur = 0.4f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = BKEase.OutBack(t / dur);
            countdownText.rectTransform.localScale = countdownBaseScale * Mathf.Lerp(0.5f, 1f, k);
            yield return null;
        }
        countdownText.rectTransform.localScale = countdownBaseScale;
        countdownRoutine = null;
    }

    public void HideCountdown()
    {
        if (countdownRoot) countdownRoot.SetActive(false);
    }

    public void ShowWaiting(bool on)
    {
        if (waitingRoot && waitingRoot.activeSelf != on) waitingRoot.SetActive(on);
    }

    public void FlashBomb()
    {
        if (!bombFlash) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        float t = 0f;
        while (t < bombFlashDuration)
        {
            t += Time.deltaTime;
            bombFlash.alpha = Mathf.Lerp(bombFlashAlpha, 0f, t / bombFlashDuration);
            yield return null;
        }
        bombFlash.alpha = 0f;
        flashRoutine = null;
    }
}
