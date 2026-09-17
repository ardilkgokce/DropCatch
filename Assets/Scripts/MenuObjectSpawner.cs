using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Menü ekranında dekoratif amaçlı sürekli obje düşürür.
/// Object pool kullanarak performans optimize edilmiştir (max 30 obje).
/// Oluşturulan objeler bu spawner'ın child'ı olur, spawner kapandığında tüm objeler silinir.
/// </summary>
public class MenuObjectSpawner : MonoBehaviour
{
    [Header("Spawn Ayarları")]
    [Tooltip("Düşecek obje prefabları")]
    public GameObject[] objectPrefabs;

    [Header("Pozisyon Ayarları")]
    [Tooltip("Spawner merkezinden sağa ve sola ne kadar uzaklığa spawn yapılacak")]
    public float spawnRangeX = 8f;

    [Header("Zaman Ayarları")]
    [Tooltip("Kaç saniyede bir obje spawn edilecek")]
    public float spawnRate = 0.5f;

    [Header("Object Pool Ayarları")]
    [Tooltip("Maksimum obje sayısı (pool limiti)")]
    public int maxPoolSize = 30;
    [Tooltip("Objeler bu Y pozisyonunun altına düşerse pool'a geri döner")]
    public float despawnYPosition = -10f;

    // Object Pool
    private List<GameObject> objectPool = new List<GameObject>();
    private int activeObjectCount = 0;

    // Spawn tracking
    private float nextSpawnTime;
    private Vector3 startPosition; // Spawner'ın başlangıç pozisyonu
    private int totalSpawnCount = 0; // Debug için toplam spawn sayısı

    void OnEnable()
    {
        // Başlangıç pozisyonunu kaydet
        startPosition = transform.position;

        // İlk spawn zamanını ayarla
        nextSpawnTime = Time.time + spawnRate;

        // Sayaçları sıfırla
        totalSpawnCount = 0;
        activeObjectCount = 0;

        Debug.Log($"MenuObjectSpawner başlatıldı: Pozisyon {startPosition}, SpawnRate: {spawnRate}s, Range: ±{spawnRangeX}, MaxPool: {maxPoolSize}");
    }

    void Update()
    {
        // Sabit spawn rate ile çalış
        if (Time.time >= nextSpawnTime)
        {
            SpawnObject();
            nextSpawnTime = Time.time + spawnRate;
        }

        // Ekrandan çıkan objeleri kontrol et ve pool'a geri al
        CheckForDespawn();
    }

    void SpawnObject()
    {
        if (objectPrefabs == null || objectPrefabs.Length == 0)
        {
            Debug.LogWarning("MenuObjectSpawner: Hiç prefab atanmamış!");
            return;
        }

        // Pool doluysa spawn yapma
        if (activeObjectCount >= maxPoolSize)
        {
            Debug.LogWarning($"Pool dolu! Aktif: {activeObjectCount}/{maxPoolSize}");
            return;
        }

        // Tam genişlik boyunca rastgele pozisyon (-spawnRangeX'ten +spawnRangeX'e)
        float xPos = startPosition.x + Random.Range(-spawnRangeX, spawnRangeX);
        Vector2 spawnPos = new Vector2(xPos, startPosition.y);

        // Pool'dan obje al veya yeni oluştur
        GameObject obj = GetFromPool();
        if (obj != null)
        {
            obj.transform.position = spawnPos;
            obj.transform.rotation = Quaternion.identity;
            obj.SetActive(true);
            activeObjectCount++;
            totalSpawnCount++;

            Debug.Log($"MenuSpawn #{totalSpawnCount}: Pos: {xPos:F2} | Aktif: {activeObjectCount}/{maxPoolSize}");
        }
    }

    /// <summary>
    /// Pool'dan inaktif obje al, yoksa yeni oluştur
    /// </summary>
    GameObject GetFromPool()
    {
        // Önce pool'da inaktif obje var mı kontrol et
        foreach (GameObject obj in objectPool)
        {
            if (obj != null && !obj.activeInHierarchy)
            {
                return obj;
            }
        }

        // Pool'da inaktif obje yok, yeni oluştur (max limiti aşmadıysak)
        if (objectPool.Count < maxPoolSize)
        {
            GameObject prefab = objectPrefabs[Random.Range(0, objectPrefabs.Length)];
            GameObject newObj = Instantiate(prefab, transform);
            objectPool.Add(newObj);
            return newObj;
        }

        // Pool dolu ve hiç inaktif obje yok
        return null;
    }

    /// <summary>
    /// Objeyi deaktif et ve pool'a geri koy
    /// </summary>
    void ReturnToPool(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(false);
            activeObjectCount--;
        }
    }

    /// <summary>
    /// Ekrandan çıkan objeleri kontrol et ve pool'a geri al
    /// </summary>
    void CheckForDespawn()
    {
        foreach (GameObject obj in objectPool)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                // Y pozisyonu despawn limitinin altına düştüyse pool'a geri al
                if (obj.transform.position.y < despawnYPosition)
                {
                    ReturnToPool(obj);
                }
            }
        }
    }

    // Scene view'da spawn range'i göster
    void OnDrawGizmosSelected()
    {
        // Spawn pozisyonu (bu spawner'ın transform pozisyonu)
        Vector3 center = transform.position;

        // Sol ve sağ limitler
        float leftLimit = center.x - spawnRangeX;
        float rightLimit = center.x + spawnRangeX;

        // ANA ÇIZGI (Yeşil) - Tam genişlik
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f); // Açık yeşil
        Vector3 leftPoint = new Vector3(leftLimit, center.y, center.z);
        Vector3 rightPoint = new Vector3(rightLimit, center.y, center.z);
        Gizmos.DrawLine(leftPoint, rightPoint);

        // SOL LIMIT İŞARETİ
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(leftPoint + Vector3.up * 0.5f, leftPoint + Vector3.down * 0.5f);
        Gizmos.DrawWireSphere(leftPoint, 0.2f);

        // SAĞ LIMIT İŞARETİ
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(rightPoint + Vector3.up * 0.5f, rightPoint + Vector3.down * 0.5f);
        Gizmos.DrawWireSphere(rightPoint, 0.2f);

        // MERKEZ NOKTA (Sarı)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 0.3f);

        // Text label (Unity Editor'da)
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;

        // Üst label - Genel bilgiler
        UnityEditor.Handles.Label(center + Vector3.up * 0.8f,
            $"MenuSpawner | Rate: {spawnRate}s | Range: ±{spawnRangeX}");

        // Alt label - Pool ve Spawn bilgileri (Play mode'da)
        if (Application.isPlaying)
        {
            UnityEditor.Handles.color = new Color(0.2f, 1f, 0.3f);
            UnityEditor.Handles.Label(center + Vector3.down * 0.6f,
                $"Pool: {activeObjectCount}/{maxPoolSize} | Toplam Spawn: {totalSpawnCount}");

            // Despawn line göster
            UnityEditor.Handles.color = Color.red;
            Vector3 despawnLeft = new Vector3(leftLimit, despawnYPosition, center.z);
            Vector3 despawnRight = new Vector3(rightLimit, despawnYPosition, center.z);
            UnityEditor.Handles.DrawDottedLine(despawnLeft, despawnRight, 3f);
            UnityEditor.Handles.Label(despawnLeft + Vector3.left * 0.5f, $"Despawn Y: {despawnYPosition:F1}");
        }
        else
        {
            // Play mode değilken limit bilgileri
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.Label(leftPoint + Vector3.down * 0.4f, $"{leftLimit:F1}");
            UnityEditor.Handles.Label(rightPoint + Vector3.down * 0.4f, $"{rightLimit:F1}");

            // Max pool size bilgisi
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(center + Vector3.down * 0.6f, $"Max Pool: {maxPoolSize}");
        }
        #endif
    }
}
