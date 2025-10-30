using UnityEngine;

public class ObjectSpawner2D : MonoBehaviour
{
    [Header("Spawn Ayarları")]
    public GameObject[] objectPrefabs;
    public float spawnRangeX = 8f;
    public float initialSpawnRate = 2f;
    public float minSpawnRate = 0.5f;
    public float spawnRateDecreaseTime = 30f;
    
    private float nextSpawnTime;
    private float currentSpawnRate;
    private float gameStartTime;
    
    void OnEnable()
    {
        currentSpawnRate = initialSpawnRate;
        gameStartTime = Time.time;
        nextSpawnTime = Time.time + currentSpawnRate;
    }
    
    void Update()
    {
        // Zorluk artışı
        float timeSinceStart = Time.time - gameStartTime;
        currentSpawnRate = Mathf.Lerp(initialSpawnRate, minSpawnRate, 
            timeSinceStart / spawnRateDecreaseTime);
        
        if(Time.time >= nextSpawnTime)
        {
            SpawnObject();
            nextSpawnTime = Time.time + currentSpawnRate;
        }
    }
    
    void SpawnObject()
    {
        // Rastgele nesne seç
        GameObject prefab = objectPrefabs[Random.Range(0, objectPrefabs.Length)];
        
        // Rastgele X pozisyonu
        float xPos = Random.Range(-spawnRangeX, spawnRangeX);
        Vector2 spawnPos = new Vector2(xPos, transform.position.y);
        
        // Nesneyi oluştur
        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        
        // Opsiyonel: Rastgele boyut
        float scale = Random.Range(0.8f, 1.2f);
        obj.transform.localScale = Vector3.one * scale;
    }
}