using UnityEngine;

public class BasketController2D : MonoBehaviour
{
    [Header("Kinect Referansları")]
    private KinectManager kinectManager;
    private PhysicalBasketDetector basketDetector;
    
    [Header("Sepet Ayarları")]
    public Transform basket2D;
    public float moveSpeed = 8f;
    public float horizontalRange = 8f;
    public SpriteRenderer basketSprite;
    
    [Header("Kalibrasyon")]
    public bool isCalibrated = false;
    private float centerOffset = 0f;
    
    [Header("Hareket Ayarları")]
    [Tooltip("Kinect koordinat scale faktörü")]
    public float coordinateScale = 5f;
    [Tooltip("Pozisyon smoothing hızı")]
    public float smoothingSpeed = 12f;
    
    [Header("Görsel Feedback")]
    public Color normalColor = Color.white;
    public Color holdingColor = Color.green;
    public Color notDetectedColor = Color.red;
    public Color bothHandsDetectedColor = Color.blue;
    
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
            basketSprite.color = notDetectedColor;
            return;
        }
        
        // Sepet tutma durumunu al
        isHoldingBasket = basketDetector.IsHoldingBasket;
        
        // Pozisyon hesapla ve uygula
        float targetX = CalculateTargetPosition();
        MoveBasket(targetX);
        
        // Görsel feedback
        UpdateVisualFeedback();
    }
    
    
    float CalculateTargetPosition()
    {
        // PhysicalBasketDetector'dan smoothed pozisyonu al
        Vector3 basketPosition = basketDetector.BasketCenterPosition;
        
        // Kinect koordinatlarını oyun koordinatlarına çevir
        float targetX = (basketPosition.x - centerOffset) * coordinateScale;
        
        return targetX;
    }
    
    void MoveBasket(float targetX)
    {
        Vector2 currentPos = basket2D.position;
        
        // Sepet tutuluyorsa daha responsive, tutulmuyorsa daha smooth hareket
        float lerpSpeed = isHoldingBasket ? smoothingSpeed : smoothingSpeed * 0.6f;
        float newX = Mathf.Lerp(currentPos.x, targetX, lerpSpeed * Time.deltaTime);
        
        // Sınırlar içinde tut
        newX = Mathf.Clamp(newX, -horizontalRange, horizontalRange);
        
        basket2D.position = new Vector2(newX, currentPos.y);
    }
    
    void UpdateVisualFeedback()
    {
        if(isHoldingBasket)
        {
            basketSprite.color = holdingColor; // Yeşil - sepet tutuluyorken
        }
        else
        {
            // 2 el görünüyor mu kontrol et
            long userId = kinectManager.GetPrimaryUserID();
            bool leftTracked = kinectManager.IsJointTracked(userId, (int)KinectInterop.JointType.HandLeft);
            bool rightTracked = kinectManager.IsJointTracked(userId, (int)KinectInterop.JointType.HandRight);
            
            if (leftTracked && rightTracked)
            {
                basketSprite.color = bothHandsDetectedColor; // Mavi - 2 el görünüyor ama yeşil state değil
            }
            else
            {
                basketSprite.color = normalColor; // Beyaz - normal durum
            }
        }
    }
    
    public void Calibrate()
    {
        if(!kinectManager || !kinectManager.IsUserDetected() || !basketDetector)
            return;
        
        // PhysicalBasketDetector'dan center pozisyonu al
        centerOffset = basketDetector.GetCenterXOffset();
        isCalibrated = true;
        Debug.Log("2D Kalibrasyon OK! Offset: " + centerOffset);
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