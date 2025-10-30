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
    public GameObject calibrationPanel;
    public TextMeshProUGUI calibrationText;
    
    [Header("Oyun Ayarları")]
    public float gameDuration = 60f;
    public int basePoints = 10;
    
    [Header("Combo Sistemi")]
    public float comboResetTime = 2f;
    public int comboMultiplier = 1;
    private float lastCatchTime;
    private int currentCombo = 0;
    
    private BasketController2D basketController;
    private ObjectSpawner2D spawner;
    private float timeRemaining;
    private int score = 0;
    private bool gameStarted = false;
    
    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        basketController = FindObjectOfType<BasketController2D>();
        spawner = FindObjectOfType<ObjectSpawner2D>();
        spawner.enabled = false;
        
        StartCoroutine(CalibrationRoutine());
    }
    
    IEnumerator CalibrationRoutine()
    {
        calibrationPanel.SetActive(true);
        calibrationText.text = "Kinect'in önünde ortada durun";
        
        // Kullanıcı algılanana kadar bekle
        while(!KinectManager.Instance || !KinectManager.Instance.IsUserDetected())
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        calibrationText.text = "Kullanıcı algılandı!\nFiziksel sepeti iki elinizle tutun ve SPACE'e basın";
        
        // Kalibrasyon için bekle
        while(!basketController.isCalibrated)
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                basketController.Calibrate();
                yield return new WaitForSeconds(1f);
                StartGame();
            }
            yield return null;
        }
    }
    
    void StartGame()
    {
        calibrationPanel.SetActive(false);
        spawner.enabled = true;
        gameStarted = true;
        timeRemaining = gameDuration;
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
        
        // Oyun sonu ekranı
        calibrationPanel.SetActive(true);
        calibrationText.text = $"Oyun Bitti!\nSkorunuz: {score}\nTekrar oynamak için SPACE";
        
        if(Input.GetKeyDown(KeyCode.Space))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}