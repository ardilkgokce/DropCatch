using UnityEngine;

public class BasketController2D : MonoBehaviour
{
    [Header("Kinect Referansları")]
    private KinectManager kinectManager;
    private PhysicalBasketDetector basketDetector;
    
    [Header("Sepet Ayarları")]
    public Transform basket2D;
    public float horizontalRange = 8f;
    
    
    [Header("Hareket Ayarları")]
    [Tooltip("Kinect koordinat scale faktörü")]
    public float coordinateScale = 5f;
    
    [Tooltip("Hareket hassasiyet çarpanı (0.1 = çok yavaş, 1 = normal, 2 = hızlı)")]
    [Range(0.1f, 2f)]
    public float movementSensitivity = 1f;
    
    [Tooltip("Smoothing hızı (1 = smoothing yok, 20 = çok smooth)")]
    [Range(1f, 20f)]
    public float smoothingSpeed = 10f;
    
    
    private bool isHoldingBasket = false;
    
    void Start()
    {
        kinectManager = KinectManager.Instance;
        basketDetector = FindObjectOfType<PhysicalBasketDetector>();
        
        if (!basketDetector)
        {
            Debug.LogError("PhysicalBasketDetector bulunamadı! GameObject'e PhysicalBasketDetector componenti ekleyin.");
        }
    }
    
    void Update()
    {
        if(!kinectManager || !kinectManager.IsUserDetected() || !basketDetector)
        {
            return;
        }
        
        // Sepet tutma durumunu al
        isHoldingBasket = basketDetector.IsHoldingBasket;
        
        // Pozisyon hesapla ve uygula
        float targetX = CalculateTargetPosition();
        MoveBasket(targetX);
        
    }
    
    float CalculateTargetPosition()
    {
        // PhysicalBasketDetector'dan pozisyonu al (zaten world space'de)
        Vector3 basketPosition = basketDetector.BasketCenterPosition;
        
        // Kinect koordinatlarını oyun koordinatlarına çevir ve hassasiyet çarpanını uygula
        float targetX = basketPosition.x * coordinateScale * movementSensitivity;
        
        return targetX;
    }
    
    void MoveBasket(float targetX)
    {
        Vector2 currentPos = basket2D.position;
        
        // Hedef pozisyonu sınırlar içinde tut
        targetX = Mathf.Clamp(targetX, -horizontalRange, horizontalRange);
        
        // Basit smoothing uygula
        float newX = Mathf.Lerp(currentPos.x, targetX, smoothingSpeed * Time.deltaTime);
        
        // Pozisyonu güncelle
        basket2D.position = new Vector2(newX, currentPos.y);
    }
    
    
    
    // Debug ve monitoring için public methodlar
    public bool IsBasketBeingHeld()
    {
        return basketDetector ? basketDetector.IsHoldingBasket : false;
    }
    
    public float GetHandDistance()
    {
        return basketDetector ? basketDetector.HandDistance : 0f;
    }
    
    public bool AreBothHandsClosed()
    {
        return basketDetector ? basketDetector.AreBothHandsClosed() : false;
    }
}