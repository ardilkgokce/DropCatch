using UnityEngine;

public class ObjectSpawner2D : MonoBehaviour
{
    [Header("Spawn Ayarları")]
    public GameObject[] objectPrefabs;

    [Header("Pozisyon Ayarları")]
    [Tooltip("Spawner merkezinden sağa ve sola ne kadar uzaklığa spawn yapılacak")]
    public float spawnRangeX = 8f;

    [Header("Zaman Ayarları")]
    [Tooltip("Kaç saniyede bir obje spawn edilecek")]
    public float spawnRate = 2f;

    // Spawn tracking
    private float nextSpawnTime;
    private Vector3 startPosition; // Spawner'ın başlangıç pozisyonu

    // Dengeli spawn için sayaçlar
    private int rightSpawnCount = 0;
    private int leftSpawnCount = 0;
    
    void OnEnable()
    {
        // Başlangıç pozisyonunu kaydet
        startPosition = transform.position;

        // İlk spawn zamanını ayarla
        nextSpawnTime = Time.time + spawnRate;

        // Sayaçları sıfırla
        rightSpawnCount = 0;
        leftSpawnCount = 0;

        Debug.Log($"Spawner başlatıldı: Pozisyon {startPosition}, SpawnRate: {spawnRate}s");
    }

    void Update()
    {
        // Sabit spawn rate ile çalış
        if(Time.time >= nextSpawnTime)
        {
            SpawnObject();
            nextSpawnTime = Time.time + spawnRate;
        }
    }
    
    void SpawnObject()
    {
        if (objectPrefabs == null || objectPrefabs.Length == 0)
        {
            Debug.LogWarning("ObjectSpawner: Hiç prefab atanmamış!");
            return;
        }

        // Rastgele nesne seç
        GameObject prefab = objectPrefabs[Random.Range(0, objectPrefabs.Length)];

        // Dengeli spawn: Hangisi daha az spawn edilmişse ona spawn yap
        bool spawnRight;

        if (rightSpawnCount < leftSpawnCount)
        {
            // Sağa daha az spawn edilmiş, sağa spawn yap
            spawnRight = true;
        }
        else if (leftSpawnCount < rightSpawnCount)
        {
            // Sola daha az spawn edilmiş, sola spawn yap
            spawnRight = false;
        }
        else
        {
            // Eşitler, rastgele seç
            spawnRight = Random.value > 0.5f;
        }

        // Pozisyonu hesapla
        float xPos;
        if (spawnRight)
        {
            // Sağ bölge: [0, +spawnRangeX]
            xPos = startPosition.x + Random.Range(0, spawnRangeX);
            rightSpawnCount++;
        }
        else
        {
            // Sol bölge: [-spawnRangeX, 0]
            xPos = startPosition.x + Random.Range(-spawnRangeX, 0);
            leftSpawnCount++;
        }

        Vector2 spawnPos = new Vector2(xPos, startPosition.y);

        // Nesneyi oluştur
        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);

        Debug.Log($"Spawn: {(spawnRight ? "Sağ" : "Sol")} | Pos: {xPos:F2} | Toplam - Sol: {leftSpawnCount}, Sağ: {rightSpawnCount}");
    }

    // Scene view'da spawn range'i göster
    void OnDrawGizmosSelected()
    {
        // Spawn pozisyonu (bu spawner'ın transform pozisyonu)
        Vector3 center = transform.position;

        // Sol ve sağ bölge limitleri
        float leftZoneStart = center.x - spawnRangeX;  // Sol bölge başlangıcı
        float leftZoneEnd = center.x;                   // Sol bölge sonu (merkez)
        float rightZoneStart = center.x;                // Sağ bölge başlangıcı (merkez)
        float rightZoneEnd = center.x + spawnRangeX;   // Sağ bölge sonu

        // SOL BÖLGE (MAVİ)
        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.8f); // Açık mavi
        Vector3 leftStart = new Vector3(leftZoneStart, center.y, center.z);
        Vector3 leftEnd = new Vector3(leftZoneEnd, center.y, center.z);
        Gizmos.DrawLine(leftStart, leftEnd);

        // Sol bölge dikey çizgiler
        Gizmos.DrawLine(leftStart + Vector3.up * 0.5f, leftStart + Vector3.down * 0.5f);

        // SAĞ BÖLGE (TURUNCU)
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f); // Turuncu
        Vector3 rightStart = new Vector3(rightZoneStart, center.y, center.z);
        Vector3 rightEnd = new Vector3(rightZoneEnd, center.y, center.z);
        Gizmos.DrawLine(rightStart, rightEnd);

        // Sağ bölge dikey çizgiler
        Gizmos.DrawLine(rightEnd + Vector3.up * 0.5f, rightEnd + Vector3.down * 0.5f);

        // MERKEZ NOKTA (SARI)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 0.3f);

        // Text label (Unity Editor'da)
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;

        // Üst label - Genel bilgiler
        UnityEditor.Handles.Label(center + Vector3.up * 0.7f,
            $"Spawn Rate: {spawnRate}s | Range: ±{spawnRangeX}");

        // Alt label - Spawn sayaçları (Play mode'da)
        if (Application.isPlaying)
        {
            UnityEditor.Handles.color = new Color(0.3f, 0.5f, 1f); // Mavi
            UnityEditor.Handles.Label(leftEnd + Vector3.up * 0.3f + Vector3.left * 1f,
                $"◄ Sol: {leftSpawnCount}");

            UnityEditor.Handles.color = new Color(1f, 0.6f, 0.2f); // Turuncu
            UnityEditor.Handles.Label(rightStart + Vector3.up * 0.3f + Vector3.right * 1f,
                $"Sağ: {rightSpawnCount} ►");
        }
        else
        {
            // Play mode değilken bölge bilgileri
            UnityEditor.Handles.color = new Color(0.7f, 0.7f, 0.7f);
            UnityEditor.Handles.Label(center + Vector3.down * 0.5f,
                $"Sol: [{leftZoneStart:F1}, {leftZoneEnd:F1}] | Sağ: [{rightZoneStart:F1}, {rightZoneEnd:F1}]");
        }
        #endif
    }
}