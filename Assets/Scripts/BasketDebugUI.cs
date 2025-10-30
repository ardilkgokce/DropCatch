using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BasketDebugUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI debugText;
    public Toggle debugToggle;
    public GameObject debugPanel;
    
    [Header("References")]
    private PhysicalBasketDetector basketDetector;
    private BasketController2D basketController;
    private KinectManager kinectManager;
    
    [Header("Settings")]
    public bool showDebugInfo = true;
    public float updateInterval = 0.1f;
    
    private float lastUpdateTime;
    
    void Start()
    {
        basketDetector = FindObjectOfType<PhysicalBasketDetector>();
        basketController = FindObjectOfType<BasketController2D>();
        kinectManager = KinectManager.Instance;
        
        if (debugToggle != null)
        {
            debugToggle.isOn = showDebugInfo;
            debugToggle.onValueChanged.AddListener(OnDebugToggleChanged);
        }
        
        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebugInfo);
        }
    }
    
    void Update()
    {
        if (!showDebugInfo || debugText == null)
            return;
        
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateDebugInfo();
            lastUpdateTime = Time.time;
        }
    }
    
    void UpdateDebugInfo()
    {
        if (!basketDetector || !basketController || !kinectManager)
        {
            debugText.text = "Component eksik!\nPhysicalBasketDetector, BasketController2D veya KinectManager bulunamadı.";
            return;
        }
        
        string debugInfo = "=== SEPET KONTROL SİSTEMİ ===\n\n";
        
        // Kinect durumu
        debugInfo += $"🔗 Kinect Bağlantısı: {(kinectManager.IsInitialized() ? "✅ Aktif" : "❌ Pasif")}\n";
        debugInfo += $"👤 Kullanıcı Algılandı: {(kinectManager.IsUserDetected() ? "✅ Evet" : "❌ Hayır")}\n\n";
        
        if (kinectManager.IsUserDetected())
        {
            // Sepet tutma durumu ve detaylı şart kontrolü
            debugInfo += "=== SEPET TUTMA DURUMU ===\n";
            debugInfo += $"🧺 Sepet Tutuluyor: {(basketDetector.IsHoldingBasket ? "✅ EVET" : "❌ Hayır")}\n";
            
            // Renk durumu açıklaması
            bool leftTracked = kinectManager.IsJointTracked(kinectManager.GetPrimaryUserID(), (int)KinectInterop.JointType.HandLeft);
            bool rightTracked = kinectManager.IsJointTracked(kinectManager.GetPrimaryUserID(), (int)KinectInterop.JointType.HandRight);
            
            string colorStatus = "";
            if (basketDetector.IsHoldingBasket)
            {
                colorStatus = "🟢 YEŞİL - Sepet tutuluyorken";
            }
            else if (leftTracked && rightTracked)
            {
                int detectionFrames = basketDetector.ConsecutiveDetectionFrames;
                int threshold = basketDetector.detectionFrameThreshold;
                colorStatus = $"🔵 MAVİ - 2 el görünüyor ama yeşil değil ({detectionFrames}/{threshold} frame)";
            }
            else
            {
                colorStatus = "⚪ BEYAZ - Normal durum";
            }
            
            debugInfo += $"🎨 Sepet Rengi: {colorStatus}\n\n";
            
            debugInfo += "=== TUTMA ŞARTLARI ===\n";
            debugInfo += $"🎮 Algılama Modu: {(basketDetector.easyDetectionMode ? "🟢 KOLAY (2 El = Yeşil)" : "🔵 GELİŞMİŞ")}\n";
            debugInfo += GetDetailedConditionsStatus();
            debugInfo += "\n";
            
            // Pozisyon bilgisi
            debugInfo += "=== POZİSYON BİLGİSİ ===\n";
            Vector3 basketPos = basketDetector.BasketCenterPosition;
            debugInfo += $"📍 Sepet Merkezi: ({basketPos.x:F2}, {basketPos.y:F2}, {basketPos.z:F2})\n";
            debugInfo += $"🎯 Oyun Pozisyonu: {basketController.transform.position.x:F2}\n";
            debugInfo += $"⚙️ Kalibrasyon: {(basketController.isCalibrated ? "✅ Tamam" : "❌ Gerekli")}\n\n";
            
            // El durumları
            long userId = kinectManager.GetPrimaryUserID();
            var leftHandState = kinectManager.GetLeftHandState(userId);
            var rightHandState = kinectManager.GetRightHandState(userId);
            
            debugInfo += "=== EL DURUMLARI ===\n";
            debugInfo += $"👈 Sol El: {GetHandStateText(leftHandState)}\n";
            debugInfo += $"👉 Sağ El: {GetHandStateText(rightHandState)}\n\n";
            
            // Performans bilgisi
            debugInfo += "=== PERFORMANS ===\n";
            debugInfo += $"🖥️ FPS: {(1f / Time.deltaTime):F0}\n";
            debugInfo += $"⏱️ Frame Time: {(Time.deltaTime * 1000):F1}ms";
        }
        else
        {
            debugInfo += "❗ Kinect'in önünde durun ve kameraya bakın\n";
            debugInfo += "• Mesafe: 1.5-3 metre arası\n";
            debugInfo += "• Işık: Çok parlak veya karanlık olmasın\n";
            debugInfo += "• Hareket: Yavaş ve sabit hareket edin";
        }
        
        debugText.text = debugInfo;
    }
    
    string GetDetailedConditionsStatus()
    {
        if (!kinectManager.IsUserDetected() || !basketDetector)
            return "❌ Sistem hazır değil\n";
        
        string conditions = "";
        long userId = kinectManager.GetPrimaryUserID();
        
        // 1. El takibi kontrolü
        bool leftTracked = kinectManager.IsJointTracked(userId, (int)KinectInterop.JointType.HandLeft);
        bool rightTracked = kinectManager.IsJointTracked(userId, (int)KinectInterop.JointType.HandRight);
        conditions += $"👀 El Takibi: {(leftTracked && rightTracked ? "✅" : "❌")} ";
        conditions += $"(Sol:{(leftTracked ? "✅" : "❌")}, Sağ:{(rightTracked ? "✅" : "❌")})\n";
        
        // Kolay mod ise sadece el takibi yeterli + memory sistemi
        if (basketDetector.easyDetectionMode)
        {
            conditions += $"🎮 KOLAY MOD: {(basketDetector.usePositionMemory ? "🧠 Hafızalı" : "⚡ Hafızasız")}\n";
            
            if (basketDetector.usePositionMemory)
            {
                // Hafızalı mod detayları
                if (basketDetector.HasValidMemory)
                {
                    float memoryAge = Time.time - basketDetector.LastValidCenterTime;
                    conditions += $"💾 Pozisyon Hafızası: ✅ {memoryAge:F1}s önce\n";
                }
                else
                {
                    conditions += "💾 Pozisyon Hafızası: ❌ Yok\n";
                }
                
                conditions += $"🔄 Tek El Desteği: {(basketDetector.useSingleHandFallback ? "✅" : "❌")}\n";
                conditions += $"🎯 Atlama Önleme: {(basketDetector.preventPositionJumping ? "✅" : "❌")}\n";
            }
            else
            {
                // Hafızasız mod
                conditions += "⚡ Basit Davranış: Sadece 2 el = yeşil\n";
                conditions += "💾 Hafıza: Kapalı (eski davranış)\n";
            }
            
            return conditions;
        }
        
        // 2. El mesafesi kontrolü
        float handDistance = basketDetector.HandDistance;
        float maxDistance = basketDetector.maxHandDistance;
        bool distanceOK = handDistance <= maxDistance;
        conditions += $"📏 El Mesafesi: {(distanceOK ? "✅" : "❌")} ";
        conditions += $"{handDistance:F2}m ≤ {maxDistance:F2}m\n";
        
        // 3. El yüksekliği kontrolü
        Vector3 leftHandPos = kinectManager.GetJointKinectPosition(userId, (int)KinectInterop.JointType.HandLeft);
        Vector3 rightHandPos = kinectManager.GetJointKinectPosition(userId, (int)KinectInterop.JointType.HandRight);
        Vector3 spinePos = kinectManager.GetJointKinectPosition(userId, (int)KinectInterop.JointType.SpineBase);
        
        float leftHeight = leftHandPos.y - spinePos.y;
        float rightHeight = rightHandPos.y - spinePos.y;
        float minHeight = basketDetector.minHandHeight;
        bool heightOK = leftHeight >= minHeight && rightHeight >= minHeight;
        
        conditions += $"📐 El Yüksekliği: {(heightOK ? "✅" : "❌")} ";
        conditions += $"Sol:{leftHeight:F2}m, Sağ:{rightHeight:F2}m ≥ {minHeight:F2}m\n";
        
        // 4. El durumu kontrolü
        var leftHandState = kinectManager.GetLeftHandState(userId);
        var rightHandState = kinectManager.GetRightHandState(userId);
        bool leftClosed = (leftHandState == KinectInterop.HandState.Closed || leftHandState == KinectInterop.HandState.Lasso);
        bool rightClosed = (rightHandState == KinectInterop.HandState.Closed || rightHandState == KinectInterop.HandState.Lasso);
        bool handStateOK = leftClosed || rightClosed;
        
        conditions += $"✊ El Durumu: {(handStateOK ? "✅" : "❌")} ";
        conditions += $"Sol:{GetHandStateIcon(leftHandState)}, Sağ:{GetHandStateIcon(rightHandState)}\n";
        
        // 5. Pozisyon kontrolü (vücudun önünde)
        Vector3 spineForward = kinectManager.GetJointKinectPosition(userId, (int)KinectInterop.JointType.SpineMid);
        bool leftPosOK = leftHandPos.z <= spineForward.z + 0.1f;
        bool rightPosOK = rightHandPos.z <= spineForward.z + 0.1f;
        bool positionOK = leftPosOK && rightPosOK;
        
        conditions += $"🎯 Pozisyon: {(positionOK ? "✅" : "❌")} ";
        conditions += $"Eller vücudun önünde (Sol:{(leftPosOK ? "✅" : "❌")}, Sağ:{(rightPosOK ? "✅" : "❌")})\n";
        
        // 6. Frame consistency
        int detectionFrames = basketDetector.ConsecutiveDetectionFrames;
        int nonDetectionFrames = basketDetector.ConsecutiveNonDetectionFrames;
        int threshold = basketDetector.detectionFrameThreshold;
        bool consistencyOK = detectionFrames >= threshold;
        
        conditions += $"⏱️ Kararlılık: {(consistencyOK ? "✅" : "❌")} ";
        conditions += $"{detectionFrames}/{threshold} frame (Kayıp:{nonDetectionFrames})\n";
        
        return conditions;
    }
    
    string GetHandStateIcon(KinectInterop.HandState handState)
    {
        switch (handState)
        {
            case KinectInterop.HandState.Open:
                return "✋";
            case KinectInterop.HandState.Closed:
                return "✊✅";
            case KinectInterop.HandState.Lasso:
                return "👌✅";
            case KinectInterop.HandState.Unknown:
                return "❓";
            case KinectInterop.HandState.NotTracked:
                return "❌";
            default:
                return "⚠️";
        }
    }
    
    string GetHandStateText(KinectInterop.HandState handState)
    {
        switch (handState)
        {
            case KinectInterop.HandState.Open:
                return "✋ Açık";
            case KinectInterop.HandState.Closed:
                return "✊ Kapalı";
            case KinectInterop.HandState.Lasso:
                return "👌 İşaret";
            case KinectInterop.HandState.Unknown:
                return "❓ Bilinmiyor";
            case KinectInterop.HandState.NotTracked:
                return "❌ Takip edilmiyor";
            default:
                return "⚠️ Tanımsız";
        }
    }
    
    void OnDebugToggleChanged(bool value)
    {
        showDebugInfo = value;
        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebugInfo);
        }
    }
    
    // Public methods for UI buttons
    public void RecalibrateBasket()
    {
        if (basketController != null)
        {
            basketController.Calibrate();
            Debug.Log("Sepet kalibrasyon yenilendi!");
        }
    }
    
    public void ToggleDetectorDebug()
    {
        if (basketDetector != null)
        {
            basketDetector.enableDebugLogs = !basketDetector.enableDebugLogs;
            Debug.Log($"PhysicalBasketDetector debug: {basketDetector.enableDebugLogs}");
        }
    }
    
    public void ToggleDetectorGizmos()
    {
        if (basketDetector != null)
        {
            basketDetector.drawDebugGizmos = !basketDetector.drawDebugGizmos;
            Debug.Log($"PhysicalBasketDetector gizmos: {basketDetector.drawDebugGizmos}");
        }
    }
}