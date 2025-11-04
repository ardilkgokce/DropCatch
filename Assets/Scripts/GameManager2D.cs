using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class GameManager2D : MonoBehaviour
{
    public static GameManager2D Instance;
    
    [Header("UI Elemanları")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI comboText;
    
    [Header("Oyun Ayarları")]
    public float gameDuration = 60f;
    public int basePoints = 10;
    
    [Header("Combo Sistemi")]
    public float comboResetTime = 2f;
    public int comboMultiplier = 1;
    private float lastCatchTime;
    private int currentCombo = 0;
    
    [Header("Referanslar")]
    private BasketController2D basketController;
    private ObjectSpawner2D spawner;
    private MenuManager menuManager;
    
    private float timeRemaining;
    private int score = 0;
    private bool gameStarted = false;
    private string currentPlayerName;
    
    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        basketController = FindObjectOfType<BasketController2D>();
        spawner = FindObjectOfType<ObjectSpawner2D>();
        menuManager = FindObjectOfType<MenuManager>();
        
        // Spawner başlangıçta kapalı
        if (spawner) spawner.enabled = false;
        
        // MenuManager oyunu başlatacak
    }
    
    public void StartGameWithPlayer(string playerName)
    {
        currentPlayerName = playerName;
        StartCoroutine(WaitForUserAndStart());
    }
    
    IEnumerator WaitForUserAndStart()
    {
        // Kullanıcı algılanana kadar bekle
        while(!KinectManager.Instance || !KinectManager.Instance.IsUserDetected())
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Kullanıcı algılandı, oyunu başlat
        yield return new WaitForSeconds(0.5f);
        StartGame();
    }
    
    void StartGame()
    {
        spawner.enabled = true;
        gameStarted = true;
        timeRemaining = gameDuration;
        score = 0;
        currentCombo = 0;
        comboMultiplier = 1;
        UpdateUI();
    }
    
    void Update()
    {
        if(gameStarted && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateUI();
            
            // Combo reset kontrolü
            if(Time.time - lastCatchTime > comboResetTime)
            {
                currentCombo = 0;
                comboMultiplier = 1;
            }
            
            if(timeRemaining <= 0)
            {
                EndGame();
            }
        }
    }
    
    public void AddScore(int points)
    {
        // Combo sistemi
        currentCombo++;
        comboMultiplier = Mathf.Min(currentCombo, 5); // Max 5x
        
        int totalPoints = points * comboMultiplier;
        score += totalPoints;
        lastCatchTime = Time.time;
        
        UpdateUI();
    }
    
    void UpdateUI()
    {
        scoreText.text = $"Skor: {score}";
        timerText.text = $"Süre: {Mathf.Ceil(timeRemaining)}";
        
        if(currentCombo > 1)
        {
            comboText.text = $"Combo x{comboMultiplier}!";
            comboText.gameObject.SetActive(true);
        }
        else
        {
            comboText.gameObject.SetActive(false);
        }
    }
    
    void EndGame()
    {
        gameStarted = false;
        spawner.enabled = false;
        
        Debug.Log($"Oyun Bitti! {currentPlayerName} - Skor: {score}");
        
        // MenuManager'a oyunun bittiğini bildir
        if (menuManager)
        {
            menuManager.OnGameEnded(score);
        }
    }
    
    // Oyunu sıfırlama metodu (sahne yenileme yerine)
    public void ResetGame()
    {
        score = 0;
        currentCombo = 0;
        comboMultiplier = 1;
        timeRemaining = gameDuration;
        gameStarted = false;
        
        // Spawner'ı temizle
        if (spawner)
        {
            spawner.enabled = false;
            // Sahnedeki tüm düşen nesneleri temizle
            FallingObject2D[] fallingObjects = FindObjectsOfType<FallingObject2D>();
            foreach (var obj in fallingObjects)
            {
                Destroy(obj.gameObject);
            }
        }
        
        UpdateUI();
    }
}