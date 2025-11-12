using UnityEngine;

public class BasketController2D : MonoBehaviour
{
    [Header("Oyuncu Ayarları")]
    [Tooltip("0 = Sol oyuncu (Player 1), 1 = Sağ oyuncu (Player 2)")]
    public int playerIndex = 0;

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

    // Kalibrasyon ve pozisyon tracking
    private Vector3 startPosition;           // Basket'in başlangıç pozisyonu (scene'deki transform)
    private Vector3 calibratedKinectPosition; // Kalibrasyon anındaki Kinect pozisyonu
    private bool isCalibrated = false;       // Kalibrasyon yapıldı mı?

    private bool isHoldingBasket = false;
    
    void Start()
    {
        kinectManager = KinectManager.Instance;

        // Aynı GameObject üzerindeki PhysicalBasketDetector'ı al
        basketDetector = GetComponent<PhysicalBasketDetector>();

        if (!basketDetector)
        {
            Debug.LogError($"Player {playerIndex}: PhysicalBasketDetector bulunamadı! Bu GameObject'e PhysicalBasketDetector componenti ekleyin.");
        }
        else
        {
            // PhysicalBasketDetector'ın playerIndex'ini otomatik olarak ayarla
            basketDetector.playerIndex = playerIndex;
            Debug.Log($"Player {playerIndex} BasketController başlatıldı.");
        }
    }

    /// <summary>
    /// Oyuncuyu kalibre eder - oyun başında GameManager tarafından çağrılır
    /// </summary>
    public void CalibratePlayer()
    {
        if (!basketDetector || !kinectManager)
        {
            Debug.LogError($"Player {playerIndex}: Kalibrasyon yapılamadı - BasketDetector veya KinectManager bulunamadı!");
            return;
        }

        // Bu oyuncunun Kinect tarafından algılandığını kontrol et
        long userId = kinectManager.GetUserIdByIndex(playerIndex);
        if (userId == 0)
        {
            Debug.LogWarning($"Player {playerIndex}: Kalibrasyon yapılamadı - Oyuncu henüz algılanmadı!");
            return;
        }

        // Basket'in başlangıç pozisyonunu kaydet
        startPosition = basket2D.position;

        // Şu anki Kinect pozisyonunu kalibrasyon noktası olarak kaydet
        calibratedKinectPosition = basketDetector.BasketCenterPosition;

        // Kalibrasyon tamamlandı
        isCalibrated = true;

        Debug.Log($"Player {playerIndex} KALİBRE EDİLDİ:\n" +
                  $"  Basket Start Position: {startPosition}\n" +
                  $"  Kinect Calibrated Position: {calibratedKinectPosition}\n" +
                  $"  Movement Range: [{startPosition.x - horizontalRange:F2}, {startPosition.x + horizontalRange:F2}]");
    }

    void Update()
    {
        if(!kinectManager || !basketDetector)
        {
            return;
        }

        // Kalibrasyon yapılmadan hareket etme
        if (!isCalibrated)
        {
            return;
        }

        // Bu player'ın takip edilip edilmediğini kontrol et
        long userId = kinectManager.GetUserIdByIndex(playerIndex);
        if (userId == 0) // 0 = takip edilen kullanıcı yok
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
        // Şu anki Kinect pozisyonunu al
        Vector3 currentKinectPosition = basketDetector.BasketCenterPosition;

        // Kalibrasyon noktasından ne kadar hareket edildiğini hesapla (offset)
        Vector3 kinectOffset = currentKinectPosition - calibratedKinectPosition;

        // Offset'i scale ve sensitivity ile çarp
        float scaledOffset = kinectOffset.x * coordinateScale * movementSensitivity;

        // Basket'in başlangıç pozisyonuna offset'i ekle
        float targetX = startPosition.x + scaledOffset;

        return targetX;
    }
    
    void MoveBasket(float targetX)
    {
        Vector2 currentPos = basket2D.position;

        // Sınırları startPosition'a göre hesapla
        float minX = startPosition.x - horizontalRange;
        float maxX = startPosition.x + horizontalRange;

        // Hedef pozisyonu sınırlar içinde tut
        targetX = Mathf.Clamp(targetX, minX, maxX);

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

    // Scene view'da hareket alanını göster
    void OnDrawGizmosSelected()
    {
        // Basket pozisyonu (basket2D varsa onu kullan, yoksa transform.position)
        Vector3 center = basket2D ? basket2D.position : transform.position;

        // Play mode'da ise startPosition'ı kullan (kalibrasyon sonrası)
        if (Application.isPlaying && isCalibrated)
        {
            center = startPosition;
        }

        // Sol ve sağ bölge limitleri
        float leftZoneStart = center.x - horizontalRange;  // Sol bölge başlangıcı
        float leftZoneEnd = center.x;                       // Sol bölge sonu (merkez)
        float rightZoneStart = center.x;                    // Sağ bölge başlangıcı (merkez)
        float rightZoneEnd = center.x + horizontalRange;   // Sağ bölge sonu

        // SOL BÖLGE (MAVİ)
        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.8f); // Açık mavi
        Vector3 leftStart = new Vector3(leftZoneStart, center.y, center.z);
        Vector3 leftEnd = new Vector3(leftZoneEnd, center.y, center.z);
        Gizmos.DrawLine(leftStart, leftEnd);

        // Sol bölge dikey çizgi
        Gizmos.DrawLine(leftStart + Vector3.up * 0.5f, leftStart + Vector3.down * 0.5f);

        // SAĞ BÖLGE (TURUNCU)
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f); // Turuncu
        Vector3 rightStart = new Vector3(rightZoneStart, center.y, center.z);
        Vector3 rightEnd = new Vector3(rightZoneEnd, center.y, center.z);
        Gizmos.DrawLine(rightStart, rightEnd);

        // Sağ bölge dikey çizgi
        Gizmos.DrawLine(rightEnd + Vector3.up * 0.5f, rightEnd + Vector3.down * 0.5f);

        // MERKEZ NOKTA (SARI)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 0.3f);

        // Text label (Unity Editor'da)
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;

        // Üst label - Genel bilgiler
        string rangeInfo = $"Player {playerIndex} | Range: ±{horizontalRange}";
        UnityEditor.Handles.Label(center + Vector3.up * 0.7f, rangeInfo);

        // Play mode'da ekstra bilgiler
        if (Application.isPlaying)
        {
            string calibrationStatus = isCalibrated ? "Calibrated ✓" : "Not Calibrated ✗";
            UnityEditor.Handles.color = isCalibrated ? Color.green : Color.red;
            UnityEditor.Handles.Label(center + Vector3.up * 1.2f, calibrationStatus);

            if (isCalibrated)
            {
                // Şu anki pozisyon
                if (basket2D)
                {
                    UnityEditor.Handles.color = Color.cyan;
                    UnityEditor.Handles.Label(center + Vector3.down * 0.3f,
                        $"Current: {basket2D.position.x:F2}");
                }
            }
        }
        else
        {
            // Edit mode'da bölge bilgileri
            UnityEditor.Handles.color = new Color(0.7f, 0.7f, 0.7f);
            UnityEditor.Handles.Label(center + Vector3.down * 0.5f,
                $"Area: [{leftZoneStart:F1}, {rightZoneEnd:F1}]");
        }
        #endif
    }
}