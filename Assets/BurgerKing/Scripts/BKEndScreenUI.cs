using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Bitiş ekranı: puan sayaç animasyonu, menü bonusu, yüksek skor, yeni rekor / tabloya giriş kutlaması
/// ve isim girişi.
/// </summary>
public class BKEndScreenUI : MonoBehaviour
{
    [Header("Kök")]
    public GameObject root;

    [Header("Yazılar")]
    public TextMeshProUGUI finalScoreText;

    [Tooltip("Yalnızca yüksek skor değeri (\"YÜKSEK SKOR\" başlığı sahnede sabit yazı)")]
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI breakdownText;
    public TextMeshProUGUI newRecordText;

    [Header("Görsel")]
    [Tooltip("Taç + burger tutan eller görseli; ekran açılırken belirir, isim girişi kartı açılınca solar")]
    public Image heroImage;
    [Range(0f, 1f)] public float heroDimAlpha = 0f;
    public float heroFadeDuration = 0.2f;

    [Header("İsim Girişi")]
    public GameObject nameEntryRoot;
    public TMP_InputField nameInput;
    public Button saveButton;
    public TextMeshProUGUI nameEntryHint;

    [Header("Kutlama")]
    public GameObject[] confettiPrefabs;
    public Transform confettiSpawnPoint;
    public int confettiSortingOrder = 60;
    public float confettiLifetime = 8f;

    [Tooltip("Konfeti prefab'larının ölçek çarpanı (dünya 21.6 x 38.4 birim olduğu için büyütmek gerekir)")]
    public float confettiScale = 5f;

    [Header("Ses")]
    [Tooltip("Puan sayarken döngüde çalan ses için kaynak")]
    public AudioSource countSource;
    public AudioClip countSound;

    [Header("Animasyon")]
    public float countUpDuration = 1.2f;
    public float bonusCountUpDuration = 0.8f;

    /// <summary>İsim kaydedildi veya ekran atlandı.</summary>
    public bool Finished { get; private set; }

    /// <summary>Oyuncu isim yazıyor (Space atlama tuşu devre dışı kalsın).</summary>
    public bool IsTyping => nameEntryRoot && nameEntryRoot.activeInHierarchy && nameInput && nameInput.isFocused;

    private Coroutine revealRoutine;
    private Coroutine heroRoutine;
    private Coroutine heroFadeRoutine;
    private Coroutine entryRoutine;
    private BKGameResult result;
    private readonly List<GameObject> spawnedFx = new List<GameObject>();

    void Awake()
    {
        if (saveButton) saveButton.onClick.AddListener(OnSaveClicked);
        if (nameInput) nameInput.onSubmit.AddListener(_ => OnSaveClicked());
    }

    public void Show(BKGameResult r)
    {
        result = r;
        Finished = false;
        if (root) root.SetActive(true);
        if (nameEntryRoot) nameEntryRoot.SetActive(false);
        if (newRecordText) newRecordText.gameObject.SetActive(false);
        if (breakdownText) breakdownText.gameObject.SetActive(false);

        // Önceki ekran punch animasyonu ortasında kapatıldıysa ölçekler bozuk kalmasın
        ResetScale(finalScoreText);
        ResetScale(breakdownText);
        ResetScale(newRecordText);
        if (nameEntryRoot) nameEntryRoot.transform.localScale = Vector3.one;
        SetHeroAlpha(1f);

        if (heroImage)
        {
            if (heroRoutine != null) StopCoroutine(heroRoutine);
            heroRoutine = StartCoroutine(Punch(heroImage.rectTransform, 0.45f, 0.85f));
        }

        if (revealRoutine != null) StopCoroutine(revealRoutine);
        revealRoutine = StartCoroutine(Reveal(r));
    }

    public void Hide()
    {
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }
        if (heroRoutine != null)
        {
            StopCoroutine(heroRoutine);
            heroRoutine = null;
        }
        if (entryRoutine != null)
        {
            StopCoroutine(entryRoutine);
            entryRoutine = null;
        }
        if (heroImage) heroImage.rectTransform.localScale = Vector3.one;
        SetHeroAlpha(1f);
        StopCountSound();
        if (root) root.SetActive(false);
        ClearFx();
    }

    /// <summary>Space ile atlama: isim girişi açıksa kapatır, ekranı bitirir.</summary>
    public void Skip()
    {
        CloseNameEntry();
        Finished = true;
    }

    IEnumerator Reveal(BKGameResult r)
    {
        if (finalScoreText) finalScoreText.text = "0 PUAN";
        if (highScoreText) highScoreText.text = Mathf.Max(0, r.previousHighScore).ToString();

        yield return CountUp(0, r.itemScore, countUpDuration);

        if (r.menuCount > 0)
        {
            if (breakdownText)
            {
                breakdownText.text = "+ " + r.menuCount + " MENÜ × " + r.menuBonusPoints + " = +" + r.menuBonusTotal + " PUAN";
                breakdownText.gameObject.SetActive(true);
                yield return Punch(breakdownText.rectTransform, 0.35f, 0.5f);
            }
            yield return new WaitForSeconds(0.35f);
            yield return CountUp(r.itemScore, r.finalScore, bonusCountUpDuration);
        }
        else if (breakdownText)
        {
            breakdownText.text = "Menü tamamlanmadı";
            breakdownText.gameObject.SetActive(true);
        }

        if (finalScoreText)
        {
            finalScoreText.text = r.finalScore + " PUAN";
            yield return Punch(finalScoreText.rectTransform, 0.3f, 0.5f);
        }

        if (r.isNewHighScore)
        {
            if (highScoreText) highScoreText.text = r.finalScore.ToString();
            if (newRecordText)
            {
                newRecordText.gameObject.SetActive(true);
                yield return Punch(newRecordText.rectTransform, 0.4f, 0.5f);
            }
        }

        if (r.isTop10)
        {
            PlayCelebration();
            if (BKGameManager.Instance) BKGameManager.Instance.PlayCelebrationSound();
            OpenNameEntry();
        }

        revealRoutine = null;
    }

    IEnumerator CountUp(int from, int to, float duration)
    {
        if (!finalScoreText || duration <= 0f || to == from)
        {
            if (finalScoreText) finalScoreText.text = to + " PUAN";
            yield break;
        }

        StartCountSound();
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            int v = Mathf.RoundToInt(Mathf.Lerp(from, to, BKEase.OutQuad(t / duration)));
            finalScoreText.text = v + " PUAN";
            yield return null;
        }
        finalScoreText.text = to + " PUAN";
        StopCountSound();
    }

    void StartCountSound()
    {
        if (!countSource || !countSound) return;

        // Efekt sesi ayarını takip et
        BKGameManager gm = BKGameManager.Instance;
        if (gm && gm.sfxSource) countSource.volume = gm.sfxSource.volume;

        countSource.clip = countSound;
        countSource.loop = true;
        countSource.Play();
    }

    void StopCountSound()
    {
        if (countSource && countSource.isPlaying) countSource.Stop();
    }

    static void ResetScale(TextMeshProUGUI t)
    {
        if (t) t.rectTransform.localScale = Vector3.one;
    }

    IEnumerator Punch(RectTransform rt, float duration, float fromScale)
    {
        if (!rt) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = BKEase.OutBack(t / duration);
            rt.localScale = Vector3.one * Mathf.LerpUnclamped(fromScale, 1f, k);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    void OpenNameEntry()
    {
        if (!nameEntryRoot) return;
        nameEntryRoot.SetActive(true);
        FadeHero(heroDimAlpha);

        if (entryRoutine != null) StopCoroutine(entryRoutine);
        entryRoutine = StartCoroutine(Punch((RectTransform)nameEntryRoot.transform, 0.35f, 0.6f));

        if (nameEntryHint) nameEntryHint.text = result.isNewHighScore ? "YENİ REKOR! İsmini yaz:" : "Liderlik tablosuna girdin! İsmini yaz:";
        if (nameInput)
        {
            nameInput.text = "";
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(nameInput.gameObject);
            nameInput.ActivateInputField();
        }
    }

    void CloseNameEntry()
    {
        if (nameEntryRoot) nameEntryRoot.SetActive(false);
        FadeHero(1f);
    }

    void OnSaveClicked()
    {
        if (Finished || !nameEntryRoot || !nameEntryRoot.activeInHierarchy) return;

        string name = nameInput ? nameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name))
        {
            if (nameEntryHint) nameEntryHint.text = "Lütfen bir isim yaz";
            if (nameInput) nameInput.ActivateInputField();
            return;
        }

        if (BKGameManager.Instance)
        {
            BKGameManager.Instance.PlayButtonSound();
            BKGameManager.Instance.SubmitName(name);
        }
        CloseNameEntry();
        Finished = true;
    }

    /// <summary>Anında ayarlar ve süren solma animasyonunu durdurur.</summary>
    void SetHeroAlpha(float a)
    {
        if (heroFadeRoutine != null)
        {
            StopCoroutine(heroFadeRoutine);
            heroFadeRoutine = null;
        }
        if (!heroImage) return;
        Color c = heroImage.color;
        c.a = a;
        heroImage.color = c;
    }

    void FadeHero(float target)
    {
        if (!heroImage) return;
        if (heroFadeRoutine != null) StopCoroutine(heroFadeRoutine);
        heroFadeRoutine = StartCoroutine(FadeHeroRoutine(target));
    }

    IEnumerator FadeHeroRoutine(float target)
    {
        float start = heroImage.color.a;
        float t = 0f;
        while (t < heroFadeDuration)
        {
            t += Time.deltaTime;
            Color c = heroImage.color;
            c.a = Mathf.Lerp(start, target, t / Mathf.Max(0.01f, heroFadeDuration));
            heroImage.color = c;
            yield return null;
        }
        Color end = heroImage.color;
        end.a = target;
        heroImage.color = end;
        heroFadeRoutine = null;
    }

    void PlayCelebration()
    {
        if (confettiPrefabs == null) return;
        Vector3 pos = confettiSpawnPoint ? confettiSpawnPoint.position : Vector3.zero;

        for (int i = 0; i < confettiPrefabs.Length; i++)
        {
            if (!confettiPrefabs[i]) continue;
            // Prefab'ın kendi rotasyonu korunmalı (konfeti emitter'ları -90° X ile yukarı bakar)
            GameObject fx = Instantiate(confettiPrefabs[i], pos, confettiPrefabs[i].transform.rotation);
            fx.transform.localScale *= Mathf.Max(0.01f, confettiScale);
            Renderer[] renderers = fx.GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++) renderers[j].sortingOrder = confettiSortingOrder;
            spawnedFx.Add(fx);
            Destroy(fx, confettiLifetime);
        }
    }

    void ClearFx()
    {
        for (int i = 0; i < spawnedFx.Count; i++)
        {
            if (spawnedFx[i]) Destroy(spawnedFx[i]);
        }
        spawnedFx.Clear();
    }
}
