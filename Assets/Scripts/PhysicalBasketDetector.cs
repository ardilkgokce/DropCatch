using UnityEngine;

public class PhysicalBasketDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Kolay algılama modu - sadece 2 el görünüyorsa yeşil state")]
    public bool easyDetectionMode = true;
    
    [Tooltip("Maksimum el mesafesi sepet tutuldu sayılmak için (metre) - sadece easyDetectionMode kapalıysa")]
    public float maxHandDistance = 0.4f;
    
    [Tooltip("Minimum el yüksekliği tutma pozisyonu için (göğüs seviyesinden yüksek) - sadece easyDetectionMode kapalıysa")]
    public float minHandHeight = -0.2f;
    
    [Tooltip("Hand state confidence threshold - sadece easyDetectionMode kapalıysa")]
    public float handStateConfidence = 0.7f;
    
    [Header("Smoothing")]
    [Tooltip("Pozisyon smoothing faktörü (0-1)")]
    public float positionSmoothFactor = 0.8f;
    
    [Tooltip("Detection state smoothing (kaç frame boyunca consistent olmalı)")]
    public int detectionFrameThreshold = 3;
    
    [Header("Position Memory System")]
    [Tooltip("Hafıza sistemini kullan - kapatırsan eski basit davranış")]
    public bool usePositionMemory = true;
    
    [Tooltip("Eller kaybolduğunda son pozisyonu ne kadar süre hatırla (saniye)")]
    [Range(0.5f, 5f)]
    public float positionMemoryTime = 2f;
    
    [Tooltip("State değişiminde pozisyon atlama önleme")]
    public bool preventPositionJumping = true;
    
    [Tooltip("Tek el görünüyorsa o eli kullan")]
    public bool useSingleHandFallback = true;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    public bool drawDebugGizmos = false;
    
    // Private variables
    private KinectManager kinectManager;
    private long currentUserId = 0;
    
    // Hand tracking
    private Vector3 leftHandPos;
    private Vector3 rightHandPos;
    private Vector3 centerPos;
    private Vector3 smoothedCenterPos;
    
    // State tracking
    private bool isHoldingBasket = false;
    private int consecutiveDetectionFrames = 0;
    private int consecutiveNonDetectionFrames = 0;
    
    // Hand states
    private KinectInterop.HandState leftHandState;
    private KinectInterop.HandState rightHandState;
    
    // Position memory system
    private Vector3 lastValidCenterPos;
    private float lastValidCenterTime;
    private bool hasValidMemory = false;
    
    public bool IsHoldingBasket => isHoldingBasket;
    public Vector3 BasketCenterPosition => smoothedCenterPos;
    public float HandDistance => Vector3.Distance(leftHandPos, rightHandPos);
    
    // Debug properties
    public int ConsecutiveDetectionFrames => consecutiveDetectionFrames;
    public int ConsecutiveNonDetectionFrames => consecutiveNonDetectionFrames;
    public bool HasValidMemory => hasValidMemory;
    public float LastValidCenterTime => lastValidCenterTime;
    
    void Start()
    {
        kinectManager = KinectManager.Instance;
        smoothedCenterPos = Vector3.zero;
    }
    
    void Update()
    {
        if (!kinectManager || !kinectManager.IsUserDetected())
        {
            ResetTracking();
            return;
        }
        
        currentUserId = kinectManager.GetPrimaryUserID();
        if (currentUserId == 0)
        {
            ResetTracking();
            return;
        }
        
        UpdateHandTracking();
        UpdateBasketDetection();
        UpdateSmoothedPosition();
        
        if (enableDebugLogs)
        {
            LogDebugInfo();
        }
    }
    
    void UpdateHandTracking()
    {
        // Get hand positions
        leftHandPos = kinectManager.GetJointKinectPosition(currentUserId, (int)KinectInterop.JointType.HandLeft);
        rightHandPos = kinectManager.GetJointKinectPosition(currentUserId, (int)KinectInterop.JointType.HandRight);
        
        // Get hand states
        leftHandState = kinectManager.GetLeftHandState(currentUserId);
        rightHandState = kinectManager.GetRightHandState(currentUserId);
        
        // Calculate center position with fallback logic
        centerPos = CalculateSmartCenterPosition();
    }
    
    Vector3 CalculateSmartCenterPosition()
    {
        bool leftTracked = kinectManager.IsJointTracked(currentUserId, (int)KinectInterop.JointType.HandLeft);
        bool rightTracked = kinectManager.IsJointTracked(currentUserId, (int)KinectInterop.JointType.HandRight);
        
        // Hafıza sistemi kapalıysa basit davranış
        if (!usePositionMemory)
        {
            if (leftTracked && rightTracked)
            {
                return (leftHandPos + rightHandPos) / 2f;
            }
            else
            {
                // Direkt spine pozisyonu
                return kinectManager.GetJointKinectPosition(currentUserId, (int)KinectInterop.JointType.SpineBase);
            }
        }
        
        // Hafıza sistemi açık - gelişmiş davranış
        Vector3 calculatedPos = Vector3.zero;
        bool isValidPosition = false;
        
        if (leftTracked && rightTracked)
        {
            // İki el de görünüyor - normal hesaplama
            calculatedPos = (leftHandPos + rightHandPos) / 2f;
            isValidPosition = true;
        }
        else if (useSingleHandFallback && (leftTracked || rightTracked))
        {
            // Tek el görünüyor - o eli kullan
            calculatedPos = leftTracked ? leftHandPos : rightHandPos;
            isValidPosition = true;
        }
        else
        {
            // Hiç el görünmüyor - hafızaya veya spine'a fall back
            if (hasValidMemory && (Time.time - lastValidCenterTime) < positionMemoryTime)
            {
                // Hafızadaki son pozisyonu kullan
                calculatedPos = lastValidCenterPos;
                isValidPosition = false; // Hafıza kullanıyoruz, yeni pozisyon değil
            }
            else
            {
                // Spine pozisyonuna fall back
                calculatedPos = kinectManager.GetJointKinectPosition(currentUserId, (int)KinectInterop.JointType.SpineBase);
                isValidPosition = false;
            }
        }
        
        // Geçerli pozisyon varsa hafızayı güncelle
        if (isValidPosition)
        {
            lastValidCenterPos = calculatedPos;
            lastValidCenterTime = Time.time;
            hasValidMemory = true;
        }
        
        return calculatedPos;
    }
    
    void UpdateBasketDetection()
    {
        bool detectedThisFrame = CheckBasketHoldingConditions();
        
        if (detectedThisFrame)
        {
            consecutiveDetectionFrames++;
            consecutiveNonDetectionFrames = 0;
            
            // Require consistent detection over multiple frames
            if (consecutiveDetectionFrames >= detectionFrameThreshold)
            {
                isHoldingBasket = true;
            }
        }
        else
        {
            consecutiveNonDetectionFrames++;
            consecutiveDetectionFrames = 0;
            
            // Allow faster loss of detection for responsiveness
            if (consecutiveNonDetectionFrames >= 2)
            {
                isHoldingBasket = false;
            }
        }
    }
    
    bool CheckBasketHoldingConditions()
    {
        bool leftTracked = kinectManager.IsJointTracked(currentUserId, (int)KinectInterop.JointType.HandLeft);
        bool rightTracked = kinectManager.IsJointTracked(currentUserId, (int)KinectInterop.JointType.HandRight);
        
        // Kolay algılama modu
        if (easyDetectionMode)
        {
            // Hafıza sistemi kapalıysa basit davranış - sadece 2 el
            if (!usePositionMemory)
            {
                return leftTracked && rightTracked;
            }
            
            // Hafıza sistemi açık - gelişmiş davranış
            // İki el görünüyorsa kesinlikle tutuluyordur
            if (leftTracked && rightTracked)
            {
                return true;
            }
            
            // Tek el fallback aktifse ve tek el görünüyorsa, hafızada da geçerli pozisyon varsa tutuluyordur
            if (useSingleHandFallback && (leftTracked || rightTracked))
            {
                // Hafızada geçerli pozisyon varsa (yakın zamanda iki el görünmüştü)
                return hasValidMemory && (Time.time - lastValidCenterTime) < positionMemoryTime;
            }
            
            // Hiç el görünmüyorsa ama yakın zamanda görünmüşse hala tutuluyordur
            if (hasValidMemory && (Time.time - lastValidCenterTime) < (positionMemoryTime * 0.5f))
            {
                return true;
            }
            
            return false;
        }
        
        // Gelişmiş algılama modu - en az iki el görünmeli
        if (!leftTracked || !rightTracked)
        {
            return false;
        }
        
        return CheckAdvancedHoldingConditions();
    }
    
    bool CheckAdvancedHoldingConditions()
    {
        // Check hand distance (hands should be reasonably close for holding a basket)
        float handDistance = Vector3.Distance(leftHandPos, rightHandPos);
        if (handDistance > maxHandDistance)
        {
            return false;
        }
        
        // Check hand height relative to spine
        Vector3 spinePos = kinectManager.GetJointKinectPosition(currentUserId, (int)KinectInterop.JointType.SpineBase);
        float leftHandHeight = leftHandPos.y - spinePos.y;
        float rightHandHeight = rightHandPos.y - spinePos.y;
        
        if (leftHandHeight < minHandHeight || rightHandHeight < minHandHeight)
        {
            return false;
        }
        
        // Check hand states - at least one hand should be closed/grasping
        bool leftHandClosed = (leftHandState == KinectInterop.HandState.Closed || 
                              leftHandState == KinectInterop.HandState.Lasso);
        bool rightHandClosed = (rightHandState == KinectInterop.HandState.Closed || 
                               rightHandState == KinectInterop.HandState.Lasso);
        
        // For basket holding, we want at least one hand closed, ideally both
        if (!leftHandClosed && !rightHandClosed)
        {
            return false;
        }
        
        // Additional check: hands should be in front of the body
        Vector3 spineForward = kinectManager.GetJointKinectPosition(currentUserId, (int)KinectInterop.JointType.SpineMid);
        if (leftHandPos.z > spineForward.z + 0.1f || rightHandPos.z > spineForward.z + 0.1f)
        {
            return false;
        }
        
        return true;
    }
    
    void UpdateSmoothedPosition()
    {
        Vector3 targetPos;
        float lerpSpeed;
        
        if (isHoldingBasket)
        {
            // Sepet tutuluyorken center position kullan
            targetPos = centerPos;
            lerpSpeed = positionSmoothFactor * Time.deltaTime * 10f;
        }
        else
        {
            // Hafıza sistemi kapalıysa basit davranış
            if (!usePositionMemory)
            {
                // Direkt spine pozisyonu
                targetPos = kinectManager.GetJointKinectPosition(currentUserId, (int)KinectInterop.JointType.SpineBase);
                lerpSpeed = positionSmoothFactor * Time.deltaTime * 5f;
            }
            else
            {
                // Hafıza sistemi açık - position jumping önle
                if (preventPositionJumping && hasValidMemory && 
                    (Time.time - lastValidCenterTime) < positionMemoryTime)
                {
                    // Hafızadaki son pozisyonu kullanarak yumuşak geçiş
                    targetPos = Vector3.Lerp(lastValidCenterPos, 
                        kinectManager.GetJointKinectPosition(currentUserId, (int)KinectInterop.JointType.SpineBase),
                        (Time.time - lastValidCenterTime) / positionMemoryTime);
                    lerpSpeed = positionSmoothFactor * Time.deltaTime * 3f; // Daha yavaş geçiş
                }
                else
                {
                    // Normal spine pozisyonu
                    targetPos = kinectManager.GetJointKinectPosition(currentUserId, (int)KinectInterop.JointType.SpineBase);
                    lerpSpeed = positionSmoothFactor * Time.deltaTime * 5f;
                }
            }
        }
        
        smoothedCenterPos = Vector3.Lerp(smoothedCenterPos, targetPos, lerpSpeed);
    }
    
    void ResetTracking()
    {
        currentUserId = 0;
        isHoldingBasket = false;
        consecutiveDetectionFrames = 0;
        consecutiveNonDetectionFrames = 0;
        leftHandPos = Vector3.zero;
        rightHandPos = Vector3.zero;
        centerPos = Vector3.zero;
        
        // Position memory reset
        hasValidMemory = false;
        lastValidCenterTime = 0f;
        lastValidCenterPos = Vector3.zero;
    }
    
    void LogDebugInfo()
    {
        float memoryAge = hasValidMemory ? (Time.time - lastValidCenterTime) : -1f;
        Debug.Log($"Basket Detection - Holding: {isHoldingBasket}, " +
                 $"Hand Distance: {HandDistance:F2}m, " +
                 $"Left Hand: {leftHandState}, Right Hand: {rightHandState}, " +
                 $"Consecutive Frames: {consecutiveDetectionFrames}, " +
                 $"Memory Age: {memoryAge:F1}s");
    }
    
    void OnDrawGizmos()
    {
        if (!drawDebugGizmos || !kinectManager || currentUserId == 0)
            return;
        
        // Draw hand positions
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(leftHandPos, 0.05f);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(rightHandPos, 0.05f);
        
        // Draw center position
        Gizmos.color = isHoldingBasket ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(centerPos, 0.08f);
        
        // Draw smoothed position
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(smoothedCenterPos, 0.06f);
        
        // Draw connection line between hands
        Gizmos.color = HandDistance <= maxHandDistance ? Color.green : Color.red;
        Gizmos.DrawLine(leftHandPos, rightHandPos);
    }
    
    // Public methods for external access
    public bool IsLeftHandClosed()
    {
        return leftHandState == KinectInterop.HandState.Closed || leftHandState == KinectInterop.HandState.Lasso;
    }
    
    public bool IsRightHandClosed()
    {
        return rightHandState == KinectInterop.HandState.Closed || rightHandState == KinectInterop.HandState.Lasso;
    }
    
    public bool AreBothHandsClosed()
    {
        return IsLeftHandClosed() && IsRightHandClosed();
    }
    
    // Kalibrasyon için center position offset'i döndür
    public float GetCenterXOffset()
    {
        return smoothedCenterPos.x;
    }
    
    // Kolay ayarlama için preset'ler
    [ContextMenu("Çok Kolay Mod (Hafızalı)")]
    public void SetVeryEasyDetection()
    {
        easyDetectionMode = true;
        usePositionMemory = true;
        detectionFrameThreshold = 2;
        positionMemoryTime = 2f;
        preventPositionJumping = true;
        useSingleHandFallback = true;
        Debug.Log("Çok kolay algılama - Hafızalı smooth transition");
    }
    
    [ContextMenu("Çok Kolay Mod (Hafızasız)")]
    public void SetVeryEasyDetectionNoMemory()
    {
        easyDetectionMode = true;
        usePositionMemory = false;
        detectionFrameThreshold = 2;
        preventPositionJumping = false;
        useSingleHandFallback = false;
        Debug.Log("Çok kolay algılama - Hafızasız basit davranış");
    }
    
    [ContextMenu("Hassas Algılama (Easy Detection)")]
    public void SetEasyDetection()
    {
        easyDetectionMode = false;
        maxHandDistance = 0.6f;
        minHandHeight = -0.4f;
        detectionFrameThreshold = 2;
        Debug.Log("Hassas algılama ayarları uygulandı");
    }
    
    [ContextMenu("Normal Algılama (Default)")]
    public void SetNormalDetection()
    {
        easyDetectionMode = false;
        maxHandDistance = 0.4f;
        minHandHeight = -0.2f;
        detectionFrameThreshold = 3;
        Debug.Log("Normal algılama ayarları uygulandı");
    }
    
    [ContextMenu("Katı Algılama (Strict Detection)")]
    public void SetStrictDetection()
    {
        easyDetectionMode = false;
        maxHandDistance = 0.3f;
        minHandHeight = 0.0f;
        detectionFrameThreshold = 5;
        Debug.Log("Katı algılama ayarları uygulandı");
    }
}