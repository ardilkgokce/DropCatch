using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    [Header("Panel Referansları")]
    public GameObject menuPanel;
    public GameObject leaderboardPanel;
    public GameObject registrationPanel;
    
    [Header("Kayıt Form Elemanları")]
    public TMP_InputField nameInput;
    public TMP_InputField phoneInput;
    public Button startButton;
    public TextMeshProUGUI errorText;
    
    [Header("Liderlik Tablosu")]
    public Transform leaderboardContent;
    public GameObject leaderboardEntryPrefab;
    
    [Header("Oyun Bitiş Efektleri")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public ParticleSystem confettiEffect;
    public AudioSource gameOverAudio;
    public AudioClip successSound;
    
    private LeaderboardManager leaderboardManager;
    private GameManager2D gameManager;
    private string currentPlayerName;
    private string currentPlayerPhone;
    
    void Start()
    {
        leaderboardManager = GetComponent<LeaderboardManager>();
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
        if (phoneInput) phoneInput.onValueChanged.AddListener(OnInputChanged);
        
        // Liderlik tablosunu güncelle
        UpdateLeaderboardUI();
        
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
        if (phoneInput) phoneInput.text = "5551234567";
        // Butona tıkla
        OnStartButtonClicked();
    }
    
    // Public metod - Unity Button OnClick event için
    public void OnStartButtonClick()
    {
        OnStartButtonClicked();
    }
    
    void OnStartButtonClicked()
    {
        Debug.Log("MenuManager: Start button clicked!");
        
        // Oyuncu bilgilerini kaydet (boş olsa bile)
        currentPlayerName = string.IsNullOrEmpty(nameInput.text.Trim()) ? "Misafir" : nameInput.text.Trim();
        currentPlayerPhone = string.IsNullOrEmpty(phoneInput.text.Trim()) ? "0000000000" : phoneInput.text.Trim();
        Debug.Log($"MenuManager: Starting game for player: {currentPlayerName}");
        
        // Menüyü kapat ve oyunu başlat
        StartCoroutine(StartGameSequence());
    }
    
    bool ValidateForm()
    {
        string name = nameInput.text.Trim();
        string phone = phoneInput.text.Trim();
        
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
        
        if (string.IsNullOrEmpty(phone))
        {
            ShowError("Lütfen telefon numaranızı girin!");
            return false;
        }
        
        // Basit telefon validasyonu (10-11 haneli)
        string cleanPhone = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");
        if (cleanPhone.Length < 10 || cleanPhone.Length > 11)
        {
            ShowError("Geçerli bir telefon numarası girin!");
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
        PlayerData playerData = new PlayerData(currentPlayerName, currentPlayerPhone, finalScore);
        leaderboardManager.AddScore(playerData);
        
        // Oyun bitiş efektlerini göster
        StartCoroutine(ShowGameOverSequence(finalScore));
    }
    
    IEnumerator ShowGameOverSequence(int score)
    {
        // Kısa bir bekleme
        yield return new WaitForSeconds(1f);
        
        // Game over panelini göster
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
            finalScoreText.text = $"Skorunuz: {score}";
            
            // Konfeti efekti
            if (confettiEffect) confettiEffect.Play();
            
            // Ses efekti
            if (gameOverAudio && successSound)
            {
                gameOverAudio.PlayOneShot(successSound);
            }
            
            // 3 saniye bekle
            yield return new WaitForSeconds(3f);
            
            gameOverPanel.SetActive(false);
        }
        
        // Liderlik tablosunu güncelle
        UpdateLeaderboardUI();
        
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
        nameInput.text = "";
        phoneInput.text = "";
        
        // Butonun aktif olduğundan emin ol
        startButton.interactable = true;
    }
    
    void UpdateLeaderboardUI()
    {
        // Eski girişleri temizle
        foreach (Transform child in leaderboardContent)
        {
            Destroy(child.gameObject);
        }
        
        // Liderlik tablosunu al ve göster
        List<PlayerData> topScores = leaderboardManager.GetTopScores();
        
        for (int i = 0; i < topScores.Count; i++)
        {
            PlayerData data = topScores[i];
            GameObject entry = CreateLeaderboardEntry(i + 1, data);
            entry.transform.SetParent(leaderboardContent, false);
        }
        
        // Boş yerleri doldur (10'a kadar)
        for (int i = topScores.Count; i < 10; i++)
        {
            GameObject entry = CreateEmptyLeaderboardEntry(i + 1);
            entry.transform.SetParent(leaderboardContent, false);
        }
    }
    
    GameObject CreateLeaderboardEntry(int rank, PlayerData data)
    {
        GameObject entry = new GameObject($"Entry_{rank}");
        RectTransform rect = entry.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 40);
        
        // Horizontal Layout Group ekle
        HorizontalLayoutGroup layout = entry.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 20;
        layout.padding = new RectOffset(10, 10, 5, 5);
        
        // Sıra numarası
        GameObject rankObj = new GameObject("Rank");
        TextMeshProUGUI rankText = rankObj.AddComponent<TextMeshProUGUI>();
        rankText.text = $"{rank}.";
        rankText.fontSize = 18;
        rankText.fontStyle = FontStyles.Bold;
        rankObj.transform.SetParent(entry.transform, false);
        
        // İsim
        GameObject nameObj = new GameObject("Name");
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = data.name;
        nameText.fontSize = 18;
        nameObj.transform.SetParent(entry.transform, false);
        
        // Layout element ekle (genişlik kontrolü için)
        LayoutElement nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.preferredWidth = 200;
        nameLayout.flexibleWidth = 1;
        
        // Skor
        GameObject scoreObj = new GameObject("Score");
        TextMeshProUGUI scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = data.score.ToString();
        scoreText.fontSize = 18;
        scoreText.fontStyle = FontStyles.Bold;
        scoreText.color = new Color(0.2f, 0.8f, 0.2f);
        scoreObj.transform.SetParent(entry.transform, false);
        
        return entry;
    }
    
    GameObject CreateEmptyLeaderboardEntry(int rank)
    {
        GameObject entry = new GameObject($"EmptyEntry_{rank}");
        RectTransform rect = entry.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 40);
        
        HorizontalLayoutGroup layout = entry.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 20;
        layout.padding = new RectOffset(10, 10, 5, 5);
        
        // Sıra numarası
        GameObject rankObj = new GameObject("Rank");
        TextMeshProUGUI rankText = rankObj.AddComponent<TextMeshProUGUI>();
        rankText.text = $"{rank}.";
        rankText.fontSize = 18;
        rankText.color = new Color(0.5f, 0.5f, 0.5f);
        rankObj.transform.SetParent(entry.transform, false);
        
        // Boş metin
        GameObject nameObj = new GameObject("Name");
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "---";
        nameText.fontSize = 18;
        nameText.color = new Color(0.5f, 0.5f, 0.5f);
        nameObj.transform.SetParent(entry.transform, false);
        
        return entry;
    }
}