using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class GameManager2D : MonoBehaviour
{
    public static GameManager2D Instance;
    
    [Header("UI Elemanları - Player 1 (Sol)")]
    public TextMeshProUGUI scoreText_P0;
    public TextMeshProUGUI comboText_P0;

    [Header("UI Elemanları - Player 2 (Sağ)")]
    public TextMeshProUGUI scoreText_P1;
    public TextMeshProUGUI comboText_P1;

    [Header("UI Elemanları - Paylaşılan")]
    public TextMeshProUGUI timerText;

    [Header("Bitiş Paneli")]
    public GameObject endGamePanel;
    public TextMeshProUGUI endGameScore_P0;
    public TextMeshProUGUI endGameScore_P1;

    [Header("Bitiş Paneli Ayarları")]
    [Tooltip("Panel kaç saniye sonra otomatik kapansın")]
    public float endGamePanelDuration = 5f;
    private float endGamePanelTimer = 0f;
    private bool endGamePanelActive = false;

    [Header("Oyun Ayarları")]
    public float gameDuration = 60f;
    public int basePoints = 10;

    [Header("Debug Ayarları")]
    [Tooltip("False yaparsanız tek oyuncuyla da oyunu başlatabilirsiniz (debug için)")]
    public bool requireTwoPlayers = true;
    
    [Header("Combo Sistemi")]
    public float comboResetTime = 2f;
    private int[] comboMultiplier = new int[2] { 1, 1 };
    private float[] lastCatchTime = new float[2];
    private int[] currentCombo = new int[2];

    [Header("Referanslar")]
    private ObjectSpawner2D[] spawners;

    private float timeRemaining;
    private int[] scores = new int[2];
    private bool gameStarted = false;
    private bool waitingToStart = true;
    
    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        // Tüm spawner'ları bul
        spawners = FindObjectsOfType<ObjectSpawner2D>();

        // Spawner'lar başlangıçta kapalı
        foreach (var spawner in spawners)
        {
            spawner.enabled = false;
        }

        // Bitiş panelini gizle
        if (endGamePanel)
        {
            endGamePanel.SetActive(false);
        }

        // Başlangıç UI'ını güncelle
        UpdateUI();

        if (requireTwoPlayers)
        {
            Debug.Log("2 Oyunculu Mod: Space tuşuna basarak oyunu başlatın! (Her iki oyuncu gerekli)");
        }
        else
        {
            Debug.Log("DEBUG MOD: Space tuşuna basarak oyunu başlatın! (Tek oyuncuyla test edebilirsiniz)");
        }
    }
    
    // 2 oyunculu mod için oyun başlatma (menüsüz)
    void CheckAndStartGame()
    {
        if (KinectManager.Instance)
        {
            long userId0 = KinectManager.Instance.GetUserIdByIndex(0);
            long userId1 = KinectManager.Instance.GetUserIdByIndex(1);

            if (requireTwoPlayers)
            {
                // İki oyuncu modu - Her iki oyuncunun da algılanması gerekli
                if (userId0 != 0 && userId1 != 0)
                {
                    Debug.Log("Her iki oyuncu da algılandı! Oyun başlıyor...");
                    StartGame();
                }
                else
                {
                    Debug.Log("Bekleniyor... Player 1: " + (userId0 != 0 ? "Algılandı" : "Bekleniyor") +
                             ", Player 2: " + (userId1 != 0 ? "Algılandı" : "Bekleniyor"));
                }
            }
            else
            {
                // Debug modu - En az bir oyuncu yeterli
                if (userId0 != 0 || userId1 != 0)
                {
                    Debug.Log("DEBUG MOD: En az bir oyuncu algılandı, oyun başlıyor...");
                    Debug.Log("Player 1: " + (userId0 != 0 ? "Algılandı" : "Yok") +
                             ", Player 2: " + (userId1 != 0 ? "Algılandı" : "Yok"));
                    StartGame();
                }
                else
                {
                    Debug.Log("DEBUG MOD: Hiçbir oyuncu algılanmadı, Kinect önünde durun...");
                }
            }
        }
        else
        {
            Debug.LogWarning("KinectManager bulunamadı!");
        }
    }
    
    void StartGame()
    {
        // Tüm basket controller'ları bul ve kalibre et
        BasketController2D[] baskets = FindObjectsOfType<BasketController2D>();
        Debug.Log($"Kalibrasyon başlıyor... {baskets.Length} basket bulundu.");

        foreach (var basket in baskets)
        {
            basket.CalibratePlayer();
        }

        // Kısa bir bekleme (kalibrasyon için)
        System.Threading.Thread.Sleep(100);

        // Tüm spawner'ları aktif et
        foreach (var spawner in spawners)
        {
            spawner.enabled = true;
        }

        gameStarted = true;
        waitingToStart = false;
        timeRemaining = gameDuration;

        // Her iki oyuncunun da skorunu sıfırla
        scores[0] = 0;
        scores[1] = 0;
        currentCombo[0] = 0;
        currentCombo[1] = 0;
        comboMultiplier[0] = 1;
        comboMultiplier[1] = 1;
        lastCatchTime[0] = 0;
        lastCatchTime[1] = 0;

        // Bitiş panelini gizle
        if (endGamePanel)
        {
            endGamePanel.SetActive(false);
        }

        UpdateUI();

        Debug.Log("Oyun başladı! Süre: " + gameDuration + " saniye");
    }
    
    void Update()
    {
        // Bitiş paneli timer kontrolü (otomatik kapanma)
        if (endGamePanelActive && endGamePanelTimer > 0)
        {
            endGamePanelTimer -= Time.deltaTime;

            if (endGamePanelTimer <= 0)
            {
                // Süre doldu, otomatik kapat
                CloseEndGamePanel();
            }
        }

        // Space tuşu ile oyunu başlat veya paneli kapat
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (endGamePanelActive)
            {
                // Panel açıkken Space'e basıldı - hemen kapat
                CloseEndGamePanel();
            }
            else if (waitingToStart)
            {
                // Oyun başlamadı, başlatmaya çalış
                CheckAndStartGame();
            }
        }

        if(gameStarted && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateUI();

            // Her iki oyuncu için combo reset kontrolü
            for (int i = 0; i < 2; i++)
            {
                if(Time.time - lastCatchTime[i] > comboResetTime && lastCatchTime[i] > 0)
                {
                    currentCombo[i] = 0;
                    comboMultiplier[i] = 1;
                }
            }

            if(timeRemaining <= 0)
            {
                EndGame();
            }
        }
    }
    
    // Player-specific scoring
    public void AddScore(int playerIndex, int points)
    {
        if (playerIndex < 0 || playerIndex >= 2)
        {
            Debug.LogError($"Geçersiz playerIndex: {playerIndex}");
            return;
        }

        if (!gameStarted)
        {
            Debug.LogWarning("Oyun başlamadan puan eklenemez!");
            return;
        }

        // Combo sistemi (player-specific)
        currentCombo[playerIndex]++;
        comboMultiplier[playerIndex] = Mathf.Min(currentCombo[playerIndex], 5); // Max 5x

        int totalPoints = points * comboMultiplier[playerIndex];
        scores[playerIndex] += totalPoints;
        lastCatchTime[playerIndex] = Time.time;

        Debug.Log($"Player {playerIndex}: +{totalPoints} puan (Combo x{comboMultiplier[playerIndex]})");

        UpdateUI();
    }
    
    void UpdateUI()
    {
        // Player 1 (Sol) UI
        if (scoreText_P0)
        {
            scoreText_P0.text = $"P1: {scores[0]}";
        }

        if (comboText_P0)
        {
            if(currentCombo[0] > 1)
            {
                comboText_P0.text = $"x{comboMultiplier[0]}!";
                comboText_P0.gameObject.SetActive(true);
            }
            else
            {
                comboText_P0.gameObject.SetActive(false);
            }
        }

        // Player 2 (Sağ) UI
        if (scoreText_P1)
        {
            scoreText_P1.text = $"P2: {scores[1]}";
        }

        if (comboText_P1)
        {
            if(currentCombo[1] > 1)
            {
                comboText_P1.text = $"x{comboMultiplier[1]}!";
                comboText_P1.gameObject.SetActive(true);
            }
            else
            {
                comboText_P1.gameObject.SetActive(false);
            }
        }

        // Timer (Paylaşılan)
        if (timerText)
        {
            timerText.text = $"Süre: {Mathf.Ceil(timeRemaining)}";
        }
    }
    
    void EndGame()
    {
        gameStarted = false;

        // Tüm spawner'ları durdur
        foreach (var spawner in spawners)
        {
            spawner.enabled = false;
        }

        Debug.Log($"Oyun Bitti! Player 1: {scores[0]}, Player 2: {scores[1]}");

        // Bitiş panelini göster ve timer'ı başlat
        if (endGamePanel)
        {
            endGamePanel.SetActive(true);
            endGamePanelActive = true;
            endGamePanelTimer = endGamePanelDuration;

            if (endGameScore_P0)
            {
                endGameScore_P0.text = $"Player 1\n{scores[0]}";
            }

            if (endGameScore_P1)
            {
                endGameScore_P1.text = $"Player 2\n{scores[1]}";
            }

            Debug.Log($"Bitiş paneli {endGamePanelDuration} saniye gösterilecek (veya Space'e basın)");
        }
    }

    /// <summary>
    /// Bitiş panelini kapat ve sahneyi yeniden yükle
    /// </summary>
    void CloseEndGamePanel()
    {
        Debug.Log("Bitiş paneli kapatıldı. Sahne yenileniyor...");

        // Sahneyi yeniden yükle (tüm değişkenler sıfırlanır, oyuncular yeniden kalibre edilir)
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    // Oyunu sıfırlama metodu (sahneyi yeniden yükler)
    public void ResetGame()
    {
        Debug.Log("Oyun sıfırlanıyor - Sahne yenileniyor...");

        // Sahneyi yeniden yükle (tüm değişkenler otomatik sıfırlanır)
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}