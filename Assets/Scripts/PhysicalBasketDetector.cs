using UnityEngine;

public class PhysicalBasketDetector : MonoBehaviour
{
    [Header("Dirsek Objeleri")]
    [Tooltip("Sol dirsek takip objesi (JointOverlayer ile kontrol ediliyor)")]
    public Transform leftElbowObject;
    
    [Tooltip("Sağ dirsek takip objesi (JointOverlayer ile kontrol ediliyor)")]
    public Transform rightElbowObject;
    
    [Header("Algılama Ayarları")]
    [Tooltip("Sepet tutuldu sayılmak için her iki dirsek objesinin de aktif olması gerekir")]
    public bool requireBothElbows = true;
    
    // Private variables
    private KinectManager kinectManager;
    private Vector3 centerPos;
    private bool isHoldingBasket = false;
    
    // Public properties
    public bool IsHoldingBasket => isHoldingBasket;
    public Vector3 BasketCenterPosition => centerPos;
    
    void Start()
    {
        kinectManager = KinectManager.Instance;
        
        if (!leftElbowObject)
        {
            Debug.LogError("Sol dirsek objesi (leftElbowObject) atanmamış!");
        }
        
        if (!rightElbowObject)
        {
            Debug.LogError("Sağ dirsek objesi (rightElbowObject) atanmamış!");
        }
    }
    
    void Update()
    {
        if (!kinectManager || !kinectManager.IsUserDetected())
        {
            isHoldingBasket = false;
            centerPos = Vector3.zero;
            return;
        }
        
        if (!leftElbowObject || !rightElbowObject)
        {
            isHoldingBasket = false;
            centerPos = Vector3.zero;
            return;
        }
        
        // Basit kontrol: Her iki dirsek objesi de aktif mi?
        bool leftActive = leftElbowObject.gameObject.activeInHierarchy;
        bool rightActive = rightElbowObject.gameObject.activeInHierarchy;
        
        if (requireBothElbows)
        {
            isHoldingBasket = leftActive && rightActive;
        }
        else
        {
            isHoldingBasket = leftActive || rightActive;
        }
        
        // Orta noktayı hesapla
        if (leftActive && rightActive)
        {
            // İki dirsek de aktif - orta noktayı al
            centerPos = (leftElbowObject.position + rightElbowObject.position) / 2f;
        }
        else if (leftActive)
        {
            // Sadece sol aktif
            centerPos = leftElbowObject.position;
        }
        else if (rightActive)
        {
            // Sadece sağ aktif
            centerPos = rightElbowObject.position;
        }
        else
        {
            // Hiçbiri aktif değil
            centerPos = Vector3.zero;
        }
    }
    
    
    // El durumu metodları (geriye dönük uyumluluk için)
    public bool AreBothHandsClosed()
    {
        // Artık kullanılmıyor ama uyumluluk için bırakıldı
        return isHoldingBasket;
    }
    
    public bool IsLeftHandClosed()
    {
        return leftElbowObject && leftElbowObject.gameObject.activeInHierarchy;
    }
    
    public bool IsRightHandClosed()
    {
        return rightElbowObject && rightElbowObject.gameObject.activeInHierarchy;
    }
    
    // Basit property'ler
    public float HandDistance => leftElbowObject && rightElbowObject ? 
        Vector3.Distance(leftElbowObject.position, rightElbowObject.position) : 0f;
}