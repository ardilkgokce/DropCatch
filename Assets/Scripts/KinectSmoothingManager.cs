using UnityEngine;

public class KinectSmoothingManager : MonoBehaviour
{
    [Header("Kinect Smoothing Settings")]
    [Tooltip("Ana smoothing faktörü (0-1, 1 = smooth değil)")]
    [Range(0f, 1f)]
    public float smoothing = 0.5f;
    
    [Tooltip("Correction (hata düzeltme) faktörü (0-1)")]
    [Range(0f, 1f)]
    public float correction = 0.5f;
    
    [Tooltip("Prediction (tahmin) faktörü (0-1)")]
    [Range(0f, 1f)]
    public float prediction = 0.5f;
    
    [Tooltip("Jitter radius (titreşim eşiği, metre)")]
    public float jitterRadius = 0.05f;
    
    [Tooltip("Max deviation radius (maksimum sapma, metre)")]
    public float maxDeviationRadius = 0.04f;
    
    [Header("Info")]
    [Tooltip("Bu component smoothing ayarlarını tutar. Asıl smoothing PhysicalBasketDetector'da yapılır.")]
    [TextArea(2, 3)]
    public string info = "Bu smoothing ayarları PhysicalBasketDetector tarafından kullanılır. Değerleri değiştirdikten sonra Play mode'da test edin.";
    
    private KinectManager kinectManager;
    private PhysicalBasketDetector basketDetector;
    
    void Start()
    {
        kinectManager = KinectManager.Instance;
        basketDetector = FindObjectOfType<PhysicalBasketDetector>();
        
        if (basketDetector != null)
        {
            // PhysicalBasketDetector'daki smoothing ayarlarını güncelle
            UpdateBasketDetectorSmoothing();
            Debug.Log("Smoothing parametreleri PhysicalBasketDetector'a uygulandı");
        }
        else
        {
            Debug.LogWarning("PhysicalBasketDetector bulunamadı. Smoothing ayarları uygulanamadı.");
        }
    }
    
    void UpdateBasketDetectorSmoothing()
    {
        if (basketDetector != null)
        {
            // PhysicalBasketDetector'daki smoothing faktörünü güncelle
            basketDetector.positionSmoothFactor = Mathf.Lerp(0.1f, 0.9f, smoothing);
        }
    }
    
    void Update()
    {
        // Inspector'da değerler değiştirildiğinde güncelle
        if (Application.isEditor && basketDetector != null)
        {
            UpdateBasketDetectorSmoothing();
        }
    }
    
    // Recommended preset configurations
    [ContextMenu("Apply Stable Preset")]
    public void ApplyStablePreset()
    {
        smoothing = 0.7f;
        correction = 0.3f;
        prediction = 0.4f;
        jitterRadius = 0.03f;
        maxDeviationRadius = 0.05f;
        UpdateBasketDetectorSmoothing();
        Debug.Log("Stable smoothing preset uygulandı");
    }
    
    [ContextMenu("Apply Responsive Preset")]
    public void ApplyResponsivePreset()
    {
        smoothing = 0.3f;
        correction = 0.6f;
        prediction = 0.6f;
        jitterRadius = 0.05f;
        maxDeviationRadius = 0.03f;
        UpdateBasketDetectorSmoothing();
        Debug.Log("Responsive smoothing preset uygulandı");
    }
    
    [ContextMenu("Apply Gaming Preset")]
    public void ApplyGamingPreset()
    {
        smoothing = 0.5f;
        correction = 0.5f;
        prediction = 0.5f;
        jitterRadius = 0.04f;
        maxDeviationRadius = 0.04f;
        UpdateBasketDetectorSmoothing();
        Debug.Log("Gaming smoothing preset uygulandı");
    }
    
    void OnValidate()
    {
        // Inspector'da değerler değiştirildiğinde otomatik uygula
        if (Application.isPlaying && basketDetector != null)
        {
            UpdateBasketDetectorSmoothing();
        }
    }
}