using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Oyun sonu özeti (bitiş ekranı ve liderlik tablosu için).</summary>
public struct BKGameResult
{
    public int itemScore;          // ürünlerden toplanan puan (bomba cezaları düşülmüş)
    public int menuCount;          // tamamlanan menü sayısı
    public int menuBonusPoints;    // menü başına bonus
    public int menuBonusTotal;     // menuCount * menuBonusPoints
    public int finalScore;         // itemScore + menuBonusTotal
    public int itemsCaught;
    public int bombsCaught;
    public int previousHighScore;  // oyun öncesi liderlik tablosundaki en yüksek puan
    public bool isTop10;           // liderlik tablosuna girer mi
    public bool isNewHighScore;    // yeni rekor mu
}

/// <summary>
/// Burger King "King's Catch" tek oyunculu oyun akışı:
/// Başlangıç → Oyuncu bekleme → Geri sayım → Oyun → Bitiş ekranı → Liderlik tablosu → Başlangıç.
/// Puan, menü sayacı, bomba cezası ve zorluk ilerlemesi burada yönetilir.
/// </summary>
public class BKGameManager : MonoBehaviour
{
    public static BKGameManager Instance { get; private set; }

    public enum State { Start, WaitingForPlayer, Countdown, Playing, Ended, Leaderboard }

    [Header("Referanslar")]
    public BKItemSpawner spawner;
    public BasketController2D basketController;
    public Transform bag;
    public BKBagFeedback bagFeedback;
    public BKHudUI hud;
    public BKStartScreenUI startScreen;
    public BKEndScreenUI endScreen;
    public BKLeaderboardUI leaderboardScreen;
    public JsonLeaderboardManager leaderboard;

    [Header("Oyun Ayarları")]
    public float gameDuration = 60f;

    [Tooltip("Oyun başlamadan önceki geri sayım (0 = yok)")]
    public int countdownSeconds = 3;

    [Tooltip("Yakalanan her bomba bu kadar puan düşürür (puan 0'ın altına inmez)")]
    public int bombPenalty = 9;

    [Tooltip("Tamamlanan her menü oyun sonunda bu kadar bonus puan verir")]
    public int menuBonusPoints = 50;

    [Tooltip("Düşen 'tamamlanan menü' ürünü yakalanınca menü sayacını da artırsın mı")]
    public bool fullMenuItemCountsAsMenu = true;

    [Tooltip("Menü tamamlamak için her birinden 1 tane toplanması gereken ürünler")]
    public List<BKItemType> requiredMenuItems = new List<BKItemType>
    {
        BKItemType.Ekmek, BKItemType.Kofte, BKItemType.Marul, BKItemType.Domates,
        BKItemType.Sogan, BKItemType.Tursu, BKItemType.Patates, BKItemType.Bardak
    };

    [Tooltip("Kinect bağlı değilse (test) oyunun klavye ile başlamasına izin ver")]
    public bool allowStartWithoutKinect = true;

    [Tooltip("Son N saniyede süre uyarısı (kırmızı)")]
    public float timeWarningSeconds = 10f;

    [Tooltip("Kinect kullanıcısı görüldükten sonra kalibrasyon için beklenen kararlı kare sayısı (dirsek pozisyonları otursun)")]
    public int calibrationStableFrames = 3;

    [Tooltip("Takip bu süreden kısa kesilip geri gelirse (yeni kullanıcı id'si alsa bile) aynı oyuncu sayılır ve sepet yeniden ortalanmaz")]
    public float recalibrateAfterLostSeconds = 0.75f;

    [Tooltip("Yeni id'nin aynı oyuncu sayılması için dirsek merkezinin son bilinen konuma en fazla uzaklığı (dünya birimi)")]
    public float sameUserMaxCenterDelta = 2f;

    [Tooltip("BAŞLA anındaki yeniden ortalama isteği bu sürede yapılamazsa (dirsekler görülmezse) mevcut kalibrasyonla devam edilir")]
    public float recalibrateRequestTimeout = 1f;

    [Header("Kombo (isteğe bağlı)")]
    [Tooltip("Açıksa art arda yakalamalar puanı çarpar (tasarım dokümanında yok, varsayılan kapalı)")]
    public bool comboEnabled = false;
    public int comboMax = 5;
    public float comboResetTime = 2f;

    [Header("Bitiş Ekranı")]
    [Tooltip("İsim girilmezse bitiş ekranı bu kadar saniye sonra kapanır")]
    public float endScreenDuration = 20f;

    [Tooltip("İsim yazılırken zaman aşımı işlemez; tuşa basılmadan bu kadar saniye geçerse tekrar işlemeye başlar")]
    public float typingIdleTimeout = 10f;

    [Tooltip("Liderlik tablosu bu kadar saniye gösterilir")]
    public float leaderboardScreenDuration = 8f;

    public bool showLeaderboardAfterGame = true;

    [Tooltip("Tabloya giren ama isim yazmayan oyuncuyu misafir adıyla kaydet")]
    public bool saveAnonymousScores = true;
    public string anonymousName = "MİSAFİR";

    [Header("Ses")]
    public AudioSource sfxSource;
    public AudioClip catchSound;
    public AudioClip bombSound;
    public AudioClip menuCompleteSound;
    public AudioClip countdownTickSound;
    public AudioClip countdownGoSound;
    public AudioClip gameOverSound;
    [Tooltip("Yeni rekor / liderlik tablosuna giriş kutlaması")]
    public AudioClip highScoreSound;
    [Tooltip("Son saniyelerde (timeWarningSeconds) her saniye çalar")]
    public AudioClip timeWarningSound;
    [Tooltip("BAŞLA ve KAYDET butonları")]
    public AudioClip buttonSound;

    [Header("Efektler")]
    public GameObject catchEffectPrefab;
    public float catchEffectScale = 1f;
    public GameObject bombEffectPrefab;
    public float bombEffectScale = 1f;
    public int effectSortingOrder = 9;

    [Tooltip("Yakalanan ürünün sepetin içine kaydığı nokta (sepet konumuna göre, dünya birimi)")]
    public Vector3 catchTargetOffset = new Vector3(0f, -0.4f, 0f);

    [Tooltip("Sepetin sprite sorting order'ı; yakalanan ürün bunun bir altına alınır")]
    public int bagSortingOrder = 5;

    // ---- Durum ----
    public State CurrentState { get; private set; } = State.Start;
    public int Score { get; private set; }
    public int MenuCount { get; private set; }
    public float TimeRemaining { get; private set; }
    public int ComboMultiplier { get; private set; } = 1;
    public BKGameResult LastResult => lastResult;

    private readonly HashSet<BKItemType> collected = new HashSet<BKItemType>();
    private int currentCombo;
    private float lastCatchTime;
    private int itemsCaught;
    private int bombsCaught;
    private Vector3 bagStartPosition;
    private Coroutine flowRoutine;
    private BKGameResult lastResult;
    private bool nameSubmitted;
    private string savedName;
    private PhysicalBasketDetector basketDetector;
    private long calibratedUserId;        // sepetin kalibre edildiği Kinect kullanıcı id'si (0 = yok / yeniden istendi)
    private long candidateUserId;         // kalibrasyon bekleyen kullanıcı
    private int candidateFrames;          // adayın dirseklerinin kesintisiz takip edildiği kare sayısı
    private float candidateGap;           // aday görülmeden önce kullanıcı ne kadar süre görülmemişti
    private float lastSeenTime = -1f;     // en son herhangi bir kullanıcının görüldüğü an
    private Vector3 lastKnownCenter;      // kalibre kullanıcının son geçerli dirsek merkezi
    private bool hasLastKnownCenter;
    private float recalibrateDeadline = -1f;

    void Awake()
    {
        Instance = this;
        if (bag) bagStartPosition = bag.position;
        if (basketController) basketDetector = basketController.GetComponent<PhysicalBasketDetector>();
    }

    void Start()
    {
        ResetToStart();
    }

    void Update()
    {
        // İsim yazılırken klavye kısayolları (R, Space) devre dışı
        bool typing = endScreen && endScreen.IsTyping;

        if (!typing && Input.GetKeyDown(KeyCode.R))
        {
            ResetToStart();
            return;
        }

        if (!typing && CurrentState == State.Start && Input.GetKeyDown(KeyCode.Space))
        {
            OnStartPressed();
            return;
        }

        if (CurrentState == State.Countdown || CurrentState == State.Playing)
        {
            EnsureBasketCalibrated();
        }

        if (CurrentState != State.Playing) return;

        float timeBefore = TimeRemaining;
        TimeRemaining -= Time.deltaTime;

        // Son saniyeler: ekrandaki sayı her değiştiğinde tik sesi (0'da süre bitti sesi çalar)
        int secondBefore = Mathf.CeilToInt(timeBefore);
        int secondNow = Mathf.CeilToInt(TimeRemaining);
        if (secondNow < secondBefore && secondNow > 0 && secondNow <= timeWarningSeconds)
        {
            PlaySfx(timeWarningSound);
        }

        float progress = gameDuration > 0f ? 1f - TimeRemaining / gameDuration : 1f;
        if (spawner) spawner.SetProgress(progress);
        if (hud) hud.SetTime(TimeRemaining, TimeRemaining <= timeWarningSeconds);

        if (comboEnabled && currentCombo > 0 && Time.time - lastCatchTime > comboResetTime)
        {
            ResetCombo();
        }

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            EndGame();
        }
    }

    // ------------------------------------------------------------------
    // Akış
    // ------------------------------------------------------------------

    /// <summary>Her şeyi sıfırlar ve başlangıç ekranına döner.</summary>
    public void ResetToStart()
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }

        CurrentState = State.Start;
        if (spawner)
        {
            spawner.Stop();
            spawner.ClearAll();
        }

        Score = 0;
        MenuCount = 0;
        collected.Clear();
        ComboMultiplier = 1;
        currentCombo = 0;
        itemsCaught = 0;
        bombsCaught = 0;
        TimeRemaining = gameDuration;
        nameSubmitted = false;
        savedName = null;
        calibratedUserId = 0;
        candidateUserId = 0;
        candidateFrames = 0;
        candidateGap = 0f;
        lastSeenTime = -1f;
        hasLastKnownCenter = false;
        recalibrateDeadline = -1f;

        ResetBagPosition();

        if (hud) hud.Hide();
        if (endScreen) endScreen.Hide();
        if (leaderboardScreen) leaderboardScreen.Hide();
        if (startScreen) startScreen.Show();

        if (MusicManager.Instance) MusicManager.Instance.SetMusicVolume(false);
    }

    /// <summary>BAŞLA butonu / Space: oyuncu bekleme + geri sayım akışını başlatır.</summary>
    public void OnStartPressed()
    {
        if (CurrentState != State.Start) return;
        PlaySfx(buttonSound);
        flowRoutine = StartCoroutine(FlowRoutine());
    }

    IEnumerator FlowRoutine()
    {
        // 1) Başlangıç ekranını kapat, HUD'ı aç
        if (startScreen) startScreen.Hide();
        if (hud)
        {
            hud.Show();
            hud.SetScore(0);
            hud.SetMenuCount(0);
            hud.SetCombo(1);
            hud.SetTracker(collected, requiredMenuItems);
            hud.SetTime(gameDuration, false);
        }
        ResetBagPosition();

        // 2) Oyuncu bekle (Space ile zorla geçilebilir)
        CurrentState = State.WaitingForPlayer;
        yield return null; // oyunu başlatan Space basışı aynı karede beklemeyi de atlamasın
        while (!IsPlayerReady())
        {
            if (hud) hud.ShowWaiting(true);
            if (Input.GetKeyDown(KeyCode.Space)) break;
            yield return null;
        }
        if (hud) hud.ShowWaiting(false);

        // 3) Geri sayım
        CurrentState = State.Countdown;
        for (int i = countdownSeconds; i >= 1; i--)
        {
            if (hud) hud.ShowCountdown(i.ToString());
            PlaySfx(countdownTickSound);
            yield return new WaitForSeconds(1f);
        }

        if (hud) hud.ShowCountdown("BAŞLA!");
        PlaySfx(countdownGoSound);
        RequestRecalibration(); // oyuncunun BAŞLA anındaki duruşu merkez olsun
        yield return new WaitForSeconds(0.8f);
        if (hud) hud.HideCountdown();

        // 4) Oyun
        BeginPlay();
        while (CurrentState == State.Playing) yield return null;

        // 5) Bitiş ekranı: isim girilene ya da süre dolana kadar (isim yazılırken süre işlemez)
        float t = 0f;
        float lastKeyTime = Time.time;
        while (endScreen && !endScreen.Finished && t < endScreenDuration)
        {
            bool typingName = endScreen.IsTyping;
            if (Input.anyKeyDown) lastKeyTime = Time.time;
            if (!typingName || Time.time - lastKeyTime > typingIdleTimeout) t += Time.deltaTime;
            if (!typingName && Input.GetKeyDown(KeyCode.Space)) endScreen.Skip();
            yield return null;
        }
        FinalizeScore();

        // 6) Liderlik tablosu
        if (showLeaderboardAfterGame && leaderboardScreen && leaderboard)
        {
            CurrentState = State.Leaderboard;
            if (endScreen) endScreen.Hide();
            leaderboardScreen.Show(leaderboard.GetTopScores(), savedName, lastResult.finalScore);
            yield return null; // bitiş ekranını atlayan Space basışı tabloyu da atlamasın

            t = 0f;
            while (t < leaderboardScreenDuration)
            {
                t += Time.deltaTime;
                if (Input.GetKeyDown(KeyCode.Space)) break;
                yield return null;
            }
        }

        flowRoutine = null;
        ResetToStart();
    }

    bool IsPlayerReady()
    {
        KinectManager km = KinectManager.Instance;
        if (km == null || !km.IsInitialized()) return allowStartWithoutKinect;
        int index = basketController ? basketController.playerIndex : 0;
        return km.GetUserIdByIndex(index) != 0;
    }

    /// <summary>Bir sonraki kararlı karede sepet yeniden kalibre edilsin (ortalanır); süresi dolarsa mevcut kalibrasyon kalır.</summary>
    void RequestRecalibration()
    {
        calibratedUserId = 0;
        candidateUserId = 0;
        candidateFrames = 0;
        recalibrateDeadline = Time.time + recalibrateRequestTimeout;
    }

    /// <summary>
    /// Kinect kullanıcısı görünür görünmez (ve kullanıcı değişirse) sepeti kalibre eder.
    /// Tek seferlik kalibrasyon o anda kullanıcı yoksa kaçar ve sepet tüm oyun boyunca donardı; bu yüzden
    /// geri sayım ve oyun boyunca her karede kontrol edilir. Dirsekler birkaç kare boyunca geçerli takip edilince kalibre edilir.
    /// Kinect kısa bir takip kaybından sonra aynı kişiye yeni id verir; dirsek merkezi son bilinen yere yakınsa
    /// eski kalibrasyon korunur (sepet ortaya zıplamaz).
    /// </summary>
    void EnsureBasketCalibrated()
    {
        KinectManager km = KinectManager.Instance;
        if (!basketController || km == null || !km.IsInitialized()) return;

        long uid = km.GetUserIdByIndex(basketController.playerIndex);
        if (uid == 0)
        {
            candidateUserId = 0;
            candidateFrames = 0;
            return;
        }

        bool elbowsValid = km.IsJointTracked(uid, (int)KinectInterop.JointType.ElbowLeft)
                           && km.IsJointTracked(uid, (int)KinectInterop.JointType.ElbowRight)
                           && ElbowObjectsValid();

        if (basketController.IsCalibrated && uid == calibratedUserId)
        {
            if (elbowsValid && basketDetector)
            {
                lastKnownCenter = basketDetector.BasketCenterPosition;
                hasLastKnownCenter = true;
            }
            lastSeenTime = Time.time;
            return;
        }

        // Yeni aday: ilk görüldüğü karede bir önceki görülmeye göre boşluk süresini kaydet
        if (uid != candidateUserId)
        {
            candidateUserId = uid;
            candidateFrames = 0;
            candidateGap = lastSeenTime >= 0f ? Time.time - lastSeenTime : float.PositiveInfinity;
        }
        lastSeenTime = Time.time;

        // BAŞLA'daki yeniden ortalama isteği zaman aşımına uğradıysa (dirsekler görülmedi) mevcut kalibrasyonla devam et
        if (basketController.IsCalibrated && calibratedUserId == 0 && recalibrateDeadline > 0f && Time.time > recalibrateDeadline)
        {
            calibratedUserId = uid;
            recalibrateDeadline = -1f;
            return;
        }

        if (!elbowsValid)
        {
            candidateFrames = 0;
            return;
        }

        candidateFrames++;
        if (candidateFrames < Mathf.Max(1, calibrationStableFrames)) return;

        // Aynı oyuncu yeni id aldıysa (kısa kesinti + benzer dirsek merkezi) eski kalibrasyon geçerli
        if (basketController.IsCalibrated && calibratedUserId != 0 && hasLastKnownCenter && basketDetector
            && candidateGap < recalibrateAfterLostSeconds
            && Mathf.Abs(basketDetector.BasketCenterPosition.x - lastKnownCenter.x) < sameUserMaxCenterDelta)
        {
            calibratedUserId = uid;
            recalibrateDeadline = -1f;
            return;
        }

        // Hareket aralığı başlangıç pozisyonuna göre hesaplandığından önce ortala, sonra kalibre et
        ResetBagPosition();
        basketController.CalibratePlayer();
        if (basketController.IsCalibrated)
        {
            calibratedUserId = uid;
            recalibrateDeadline = -1f;
            if (basketDetector)
            {
                lastKnownCenter = basketDetector.BasketCenterPosition;
                hasLastKnownCenter = true;
            }
        }
    }

    /// <summary>
    /// JointOverlayer dirsek objelerini yalnızca geçerli bir pozisyon alınca taşır (z = sensöre uzaklık &gt; 0);
    /// kaybolunca z = -10, hiç yazılmadıysa z = 0. Bayat/boş pozisyonla kalibre olmayı önler.
    /// </summary>
    bool ElbowObjectsValid()
    {
        if (!basketDetector || !basketDetector.leftElbowObject || !basketDetector.rightElbowObject) return true;
        return basketDetector.leftElbowObject.position.z > 0f && basketDetector.rightElbowObject.position.z > 0f;
    }

    void ResetBagPosition()
    {
        if (bag) bag.position = bagStartPosition;
    }

    void BeginPlay()
    {
        Score = 0;
        MenuCount = 0;
        collected.Clear();
        ComboMultiplier = 1;
        currentCombo = 0;
        itemsCaught = 0;
        bombsCaught = 0;
        TimeRemaining = gameDuration;

        if (hud)
        {
            hud.SetScore(0);
            hud.SetMenuCount(0);
            hud.SetCombo(1);
            hud.SetTracker(collected, requiredMenuItems);
            hud.SetTime(gameDuration, false);
        }

        if (spawner) spawner.Begin();
        CurrentState = State.Playing;
    }

    void EndGame()
    {
        CurrentState = State.Ended;

        if (spawner)
        {
            spawner.Stop();
            spawner.ClearAll();
        }

        int menuBonus = MenuCount * menuBonusPoints;
        int finalScore = Score + menuBonus;

        int previousHigh = 0;
        bool isTop10 = false;
        if (leaderboard)
        {
            List<PlayerData> top = leaderboard.GetTopScores();
            if (top != null && top.Count > 0) previousHigh = top[0].score;
            isTop10 = finalScore > 0 && leaderboard.IsHighScore(finalScore);
        }

        lastResult = new BKGameResult
        {
            itemScore = Score,
            menuCount = MenuCount,
            menuBonusPoints = menuBonusPoints,
            menuBonusTotal = menuBonus,
            finalScore = finalScore,
            itemsCaught = itemsCaught,
            bombsCaught = bombsCaught,
            previousHighScore = previousHigh,
            isTop10 = isTop10,
            isNewHighScore = finalScore > previousHigh && finalScore > 0
        };

        if (hud) hud.Hide();
        if (MusicManager.Instance) MusicManager.Instance.SetMusicVolume(true);
        PlaySfx(gameOverSound); // kutlama sesi bitiş ekranında skor açıklanınca çalar

        if (endScreen) endScreen.Show(lastResult);
    }

    /// <summary>Bitiş ekranından isim gönderildiğinde çağrılır.</summary>
    public void SubmitName(string playerName)
    {
        if (nameSubmitted || !leaderboard) return;

        playerName = string.IsNullOrWhiteSpace(playerName) ? anonymousName : playerName.Trim();
        leaderboard.AddScore(new PlayerData(playerName, "", lastResult.finalScore));
        nameSubmitted = true;
        savedName = playerName;
    }

    void FinalizeScore()
    {
        if (!nameSubmitted && lastResult.isTop10 && saveAnonymousScores)
        {
            SubmitName(anonymousName);
        }
    }

    // ------------------------------------------------------------------
    // Yakalama
    // ------------------------------------------------------------------

    /// <summary>BKFallingItem sepete değdiğinde çağırır.</summary>
    public void OnItemCaught(BKFallingItem item, Transform basketTransform)
    {
        if (item == null) return;

        if (CurrentState != State.Playing)
        {
            item.Vanish();
            return;
        }

        Vector3 pos = item.transform.position;

        if (item.IsBomb)
        {
            bombsCaught++;
            int before = Score;
            Score = Mathf.Max(0, Score - bombPenalty);
            ResetCombo();

            if (bagFeedback) bagFeedback.PlayBombHit();
            SpawnEffect(bombEffectPrefab, pos, bombEffectScale);
            PlaySfx(bombSound);

            if (hud)
            {
                int lost = before - Score;
                hud.ShowFloatingText(pos, lost > 0 ? "-" + lost : "BOMBA!", hud.penaltyColor);
                hud.SetScore(Score);
                hud.FlashBomb();
            }

            item.Vanish();
            return;
        }

        itemsCaught++;

        if (comboEnabled)
        {
            currentCombo++;
            ComboMultiplier = Mathf.Clamp(currentCombo, 1, Mathf.Max(1, comboMax));
            lastCatchTime = Time.time;
            if (hud) hud.SetCombo(ComboMultiplier);
        }

        int gained = item.points * ComboMultiplier;
        Score += gained;

        if (hud)
        {
            hud.SetScore(Score);
            hud.ShowFloatingText(pos, "+" + gained, hud.gainColor);
        }

        if (bagFeedback) bagFeedback.PlayCatch();
        SpawnEffect(catchEffectPrefab, pos, catchEffectScale);
        PlaySfx(catchSound);

        if (item.IsFullMenu)
        {
            // Hazır menü bonus bir menü sayılır; oyuncunun yarım kalan menü ilerlemesine dokunmaz
            if (fullMenuItemCountsAsMenu) CompleteMenu(false);
        }
        else if (requiredMenuItems.Contains(item.itemType) && collected.Add(item.itemType))
        {
            if (hud) hud.SetTracker(collected, requiredMenuItems);
            if (IsMenuComplete()) CompleteMenu(true);
        }

        item.PlayCaughtAnimation(bag ? bag : basketTransform, catchTargetOffset, 0.25f, bagSortingOrder - 1);
    }

    /// <summary>Ürün yakalanmadan ekrandan çıktığında çağrılır.</summary>
    public void OnItemMissed(BKFallingItem item)
    {
        if (CurrentState != State.Playing) return;
        if (comboEnabled && !item.IsBomb) ResetCombo();
    }

    bool IsMenuComplete()
    {
        for (int i = 0; i < requiredMenuItems.Count; i++)
        {
            if (!collected.Contains(requiredMenuItems[i])) return false;
        }
        return requiredMenuItems.Count > 0;
    }

    void CompleteMenu(bool clearProgress)
    {
        MenuCount++;

        if (clearProgress)
        {
            collected.Clear();
            if (hud) hud.SetTracker(collected, requiredMenuItems);
        }

        if (hud)
        {
            hud.SetMenuCount(MenuCount);
            hud.PlayMenuComplete(MenuCount);
        }

        PlaySfx(menuCompleteSound);
        if (bagFeedback) bagFeedback.PlayCatch(1.6f);
    }

    void ResetCombo()
    {
        currentCombo = 0;
        ComboMultiplier = 1;
        if (hud) hud.SetCombo(1);
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip && sfxSource) sfxSource.PlayOneShot(clip);
    }

    public void PlayButtonSound()
    {
        PlaySfx(buttonSound);
    }

    public void PlayCelebrationSound()
    {
        PlaySfx(highScoreSound);
    }

    void SpawnEffect(GameObject prefab, Vector3 pos, float scale)
    {
        if (!prefab) return;

        GameObject fx = Instantiate(prefab, pos, prefab.transform.rotation);
        fx.transform.localScale *= Mathf.Max(0.01f, scale);

        Renderer[] renderers = fx.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = effectSortingOrder;
        }

        Destroy(fx, 4f);
    }
}
