using UnityEngine;

public class PhysicalBasketDetector : MonoBehaviour
{
    [Header("Oyuncu Ayarları")]
    [Tooltip("0 = Sol oyuncu (Player 1), 1 = Sağ oyuncu (Player 2)")]
    public int playerIndex = 0;

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

        // Eğer elbow object'leri manuel atanmamışsa, otomatik bulmaya çalış
        if (!leftElbowObject)
        {
            // Örnek isimler: "LeftElbow_P0", "ElbowLeft_P0", "LeftElbowObject_P0"
            string[] possibleNames = new string[] {
                "LeftElbow_P" + playerIndex,
                "ElbowLeft_P" + playerIndex,
                "LeftElbowObject_P" + playerIndex
            };

            foreach (string name in possibleNames)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    leftElbowObject = obj.transform;
                    Debug.Log($"Player {playerIndex}: Sol dirsek objesi otomatik bulundu: {name}");
                    break;
                }
            }

            if (!leftElbowObject)
            {
                Debug.LogWarning($"Player {playerIndex}: Sol dirsek objesi bulunamadı! Inspector'dan manuel atayın.");
            }
        }

        if (!rightElbowObject)
        {
            string[] possibleNames = new string[] {
                "RightElbow_P" + playerIndex,
                "ElbowRight_P" + playerIndex,
                "RightElbowObject_P" + playerIndex
            };

            foreach (string name in possibleNames)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    rightElbowObject = obj.transform;
                    Debug.Log($"Player {playerIndex}: Sağ dirsek objesi otomatik bulundu: {name}");
                    break;
                }
            }

            if (!rightElbowObject)
            {
                Debug.LogWarning($"Player {playerIndex}: Sağ dirsek objesi bulunamadı! Inspector'dan manuel atayın.");
            }
        }
    }
    
    void Update()
    {
        // Player-specific user detection kontrolü
        if (!kinectManager)
        {
            isHoldingBasket = false;
            centerPos = Vector3.zero;
            return;
        }

        // Bu player'ın Kinect tarafından takip edilip edilmediğini kontrol et
        long userId = kinectManager.GetUserIdByIndex(playerIndex);
        if (userId == 0) // 0 = takip edilen kullanıcı yok
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