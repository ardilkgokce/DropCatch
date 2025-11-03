using UnityEngine;

// NOT: Bu script artık kullanılmıyor. Yeni basitleştirilmiş sistemde smoothing yok.
// Geriye dönük uyumluluk için bırakıldı ama hiçbir işlevi yok.
public class KinectSmoothingManager : MonoBehaviour
{
    [Header("DEPRECATED - KULLANILMIYOR")]
    [TextArea(3, 5)]
    public string warning = "Bu script artık kullanılmıyor!\n\nYeni sistemde smoothing yok.\nPhysicalBasketDetector direkt olarak dirsek objelerinin pozisyonunu kullanıyor.";
    
    [Header("Eski Smoothing Ayarları (Artık İşlevsiz)")]
    [Range(0f, 1f)]
    public float smoothing = 0.5f;
    
    [Range(0f, 1f)]
    public float correction = 0.5f;
    
    [Range(0f, 1f)]
    public float prediction = 0.5f;
    
    public float jitterRadius = 0.05f;
    public float maxDeviationRadius = 0.04f;
    
    void Start()
    {
        Debug.LogWarning("KinectSmoothingManager artık kullanılmıyor. Bu component'i kaldırabilirsiniz.");
    }
}