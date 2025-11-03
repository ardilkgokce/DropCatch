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
            // Sepet durumu
            debugInfo += "=== SEPET DURUMU ===\n";
            debugInfo += $"🧺 Sepet Tutuluyor: {(basketDetector.IsHoldingBasket ? "✅ EVET" : "❌ Hayır")}\n";
            
            // Dirsek durumu
            bool leftElbowActive = basketDetector.leftElbowObject && basketDetector.leftElbowObject.gameObject.activeInHierarchy;
            bool rightElbowActive = basketDetector.rightElbowObject && basketDetector.rightElbowObject.gameObject.activeInHierarchy;
            
            debugInfo += $"💪 Sol Dirsek: {(leftElbowActive ? "✅ Aktif" : "❌ Pasif")}\n";
            debugInfo += $"💪 Sağ Dirsek: {(rightElbowActive ? "✅ Aktif" : "❌ Pasif")}\n";
            debugInfo += $"📏 Dirsek Mesafesi: {basketDetector.HandDistance:F2}m\n\n";
            
            // Pozisyon bilgisi
            debugInfo += "=== POZİSYON BİLGİSİ ===\n";
            Vector3 basketPos = basketDetector.BasketCenterPosition;
            debugInfo += $"📍 Sepet Merkezi: ({basketPos.x:F2}, {basketPos.y:F2}, {basketPos.z:F2})\n";
            debugInfo += $"🎯 Oyun Pozisyonu: {basketController.transform.position.x:F2}\n\n";
            
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
    
    void OnDebugToggleChanged(bool value)
    {
        showDebugInfo = value;
        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebugInfo);
        }
    }
    
}