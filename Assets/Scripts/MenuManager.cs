using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class MenuManager : MonoBehaviour
{
    [Header("Panel Referansları")]
    public GameObject menuPanel;
    public GameObject gameOverPanel;
    [FormerlySerializedAs("KVKKPanel")] public GameObject kvkkPanel;
    
    [Header("Kayıt Form Elemanları")]
    public TMP_InputField nameInput;
    public TMP_InputField emailInput;
    public Button startButton;
    public TextMeshProUGUI errorText;
    public TextMeshProUGUI kvvkCheckBox;
    
    [Header("Oyun Bitiş")]
    public TextMeshProUGUI[] leaderboardTexts; // 10 adet text (1. den 10. ya kadar)
    public float gameOverTimeout = 10f; // Otomatik dönüş süresi
    
    [Header("Menü Liderlik Tablosu")]
    public TextMeshProUGUI[] menuLeaderboardTexts; // MenuPanel'deki 10 adet text
    
    private JsonLeaderboardManager leaderboardManager;
    private GameManager2D gameManager;
    private string currentPlayerName;
    private string currentPlayerEmail;
    private Coroutine gameOverCoroutine;
    
    void Start()
    {
        // Önce eski LeaderboardManager varsa deaktif et
        LeaderboardManager oldManager = GetComponent<LeaderboardManager>();
        if (oldManager) oldManager.enabled = false;
        
        // JsonLeaderboardManager'ı ekle veya al
        leaderboardManager = GetComponent<JsonLeaderboardManager>();
        if (!leaderboardManager)
        {
            leaderboardManager = gameObject.AddComponent<JsonLeaderboardManager>();
        }
        
        gameManager = GameManager2D.Instance;
        
        // UI başlangıç durumu
        menuPanel.SetActive(true);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        
        // CanvasGroup kontrolü ve düzenleme
        CanvasGroup menuCanvasGroup = menuPanel.GetComponent<CanvasGroup>();
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
            menuCanvasGroup.interactable = true;
            menuCanvasGroup.blocksRaycasts = true;
            Debug.Log("MenuManager: CanvasGroup ayarlandı");
        }
        
        // Buton kontrolü
        if (startButton == null)
        {
            Debug.LogError("MenuManager: Start button referansı atanmamış!");
            return;
        }
        
        // Buton listener'ı
        startButton.onClick.RemoveAllListeners(); // Önce eski listener'ları temizle
        startButton.onClick.AddListener(OnStartButtonClicked);
        Debug.Log("MenuManager: Start button listener eklendi");
        
        // Input field listener'ları
        if (nameInput) nameInput.onValueChanged.AddListener(OnInputChanged);
        if (emailInput) emailInput.onValueChanged.AddListener(OnInputChanged);
        
        // Menü liderlik tablosunu güncelle
        UpdateMenuLeaderboard();
        
        // Error text'i temizle
        if (errorText) errorText.text = "";
        
        // Butonun interactable olduğundan emin ol
        startButton.interactable = true;
    }
    
    void OnInputChanged(string value)
    {
        // Input değiştiğinde error'u temizle
        if (errorText) errorText.text = "";
    }
    
    // Test metodu - Unity Inspector'dan çağrılabilir
    [ContextMenu("Test Start Button")]
    public void TestStartButton()
    {
        Debug.Log("Test Start Button çağrıldı!");
        // Test için form alanlarını doldur
        if (nameInput) nameInput.text = "Test Oyuncu";
        if (emailInput) emailInput.text = "test@email.com";
        // Butona tıkla
        OnStartButtonClicked();
    }
    
    // Public metod - Unity Button OnClick event için
    public void OnStartButtonClick()
    {
        OnStartButtonClicked();
    }

    public void KvkkPanelOpen()
    {
        kvkkPanel.SetActive(true);
    }
    public void KvkkPanelClose()
    {
        kvkkPanel.SetActive(false);
        kvvkCheckBox.gameObject.SetActive(true);
    }
    
    void OnStartButtonClicked()
    {
        Debug.Log("MenuManager: Start button clicked!");
        
        // Oyuncu bilgilerini kaydet (boş olsa bile)
        currentPlayerName = string.IsNullOrEmpty(nameInput.text.Trim()) ? "Misafir" : nameInput.text.Trim();
        currentPlayerEmail = string.IsNullOrEmpty(emailInput.text.Trim()) ? "misafir@email.com" : emailInput.text.Trim();
        Debug.Log($"MenuManager: Starting game for player: {currentPlayerName}");
        
        // Menüyü kapat ve oyunu başlat
        StartCoroutine(StartGameSequence());
    }
    
    bool ValidateForm()
    {
        string name = nameInput.text.Trim();
        string email = emailInput.text.Trim();
        
        if (string.IsNullOrEmpty(name))
        {
            ShowError("Lütfen isminizi girin!");
            return false;
        }
        
        if (name.Length < 2)
        {
            ShowError("İsim en az 2 karakter olmalıdır!");
            return false;
        }
        
        if (string.IsNullOrEmpty(email))
        {
            ShowError("Lütfen email adresinizi girin!");
            return false;
        }
        
        // Basit email validasyonu
        if (!email.Contains("@") || !email.Contains("."))
        {
            ShowError("Geçerli bir email adresi girin!");
            return false;
        }
        
        return true;
    }
    
    void ShowError(string message)
    {
        if (errorText)
        {
            errorText.text = message;
            errorText.color = Color.red;
        }
    }
    
    IEnumerator StartGameSequence()
    {
        Debug.Log("MenuManager: StartGameSequence başladı");
        
        // Menü panelini fade out yap
        CanvasGroup canvasGroup = menuPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            float fadeTime = 0.5f;
            float elapsedTime = 0;
            
            while (elapsedTime < fadeTime)
            {
                canvasGroup.alpha = Mathf.Lerp(1, 0, elapsedTime / fadeTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            Debug.Log("MenuManager: CanvasGroup bulunamadı, doğrudan kapatılıyor");
        }
        
        menuPanel.SetActive(false);
        
        // GameManager'a oyunu başlatmasını söyle
        if (gameManager)
        {
            Debug.Log("MenuManager: GameManager'a oyunu başlatması söyleniyor");
            gameManager.StartGameWithPlayer(currentPlayerName);
        }
        else
        {
            Debug.LogError("MenuManager: GameManager bulunamadı!");
        }
    }
    
    public void OnGameEnded(int finalScore)
    {
        // Oyuncu verisini oluştur ve kaydet
        PlayerData playerData = new PlayerData(currentPlayerName, currentPlayerEmail, finalScore);
        leaderboardManager.AddScore(playerData);
        
        // Oyun bitiş ekranını göster
        ShowGameOverScreen();
    }
    
    void ShowGameOverScreen()
    {
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
            UpdateGameOverLeaderboard();
            
            // Timeout coroutine'ini başlat
            if (gameOverCoroutine != null)
            {
                StopCoroutine(gameOverCoroutine);
            }
            gameOverCoroutine = StartCoroutine(GameOverTimeout());
        }
    }
    
    void UpdateGameOverLeaderboard()
    {
        List<PlayerData> topScores = leaderboardManager.GetTopScores();
        
        // 10 text'i güncelle
        for (int i = 0; i < leaderboardTexts.Length; i++)
        {
            if (i < topScores.Count)
            {
                // "1. ARDIL GÖKÇE 960 PUAN" formatında
                leaderboardTexts[i].text = $"{i + 1}. {topScores[i].name.ToUpper()} {topScores[i].score} PUAN";
            }
            else
            {
                // Boş yerler
                leaderboardTexts[i].text = $"{i + 1}. ---------- --- PUAN";
            }
        }
    }
    
    IEnumerator GameOverTimeout()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < gameOverTimeout)
        {
            // Herhangi bir tuşa basıldı mı kontrol et
            if (Input.anyKeyDown)
            {
                Debug.Log("Tuşa basıldı, menüye dönülüyor");
                break;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Game over ekranını kapat ve menüye dön
        ReturnToMenu();
    }
    
    void ReturnToMenu()
    {
        // Game over panelini kapat
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Coroutine'i temizle
        if (gameOverCoroutine != null)
        {
            StopCoroutine(gameOverCoroutine);
            gameOverCoroutine = null;
        }
        
        // Menü liderlik tablosunu güncelle
        UpdateMenuLeaderboard();
        
        // Menüyü tekrar göster
        menuPanel.SetActive(true);
        CanvasGroup canvasGroup = menuPanel.GetComponent<CanvasGroup>();
        if (canvasGroup)
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        // Form alanlarını temizle
        if (nameInput) nameInput.text = "";
        if (emailInput) emailInput.text = "";
        
        // Butonun aktif olduğundan emin ol
        if (startButton) startButton.interactable = true;
    }
    
    void UpdateMenuLeaderboard()
    {
        if (menuLeaderboardTexts == null || menuLeaderboardTexts.Length == 0)
            return;
            
        List<PlayerData> topScores = leaderboardManager.GetTopScores();
        
        // 10 text'i güncelle
        for (int i = 0; i < menuLeaderboardTexts.Length && i < 10; i++)
        {
            if (menuLeaderboardTexts[i] != null)
            {
                if (i < topScores.Count)
                {
                    // "1. ARDIL GÖKÇE 960 PUAN" formatında
                    menuLeaderboardTexts[i].text = $"{i + 1}. {topScores[i].name.ToUpper()} {topScores[i].score} PUAN";
                }
                else
                {
                    // Boş yerler
                    menuLeaderboardTexts[i].text = $"{i + 1}. ---------- --- PUAN";
                }
            }
        }
    }
}