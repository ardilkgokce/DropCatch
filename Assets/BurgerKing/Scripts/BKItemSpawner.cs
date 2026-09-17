using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ağırlıklı rastgele ürün düşürücü. Oyun ilerledikçe (progress 0→1) spawn aralığı kısalır, düşme hızı artar.
/// "Tamamlanan menü" gibi nadir ürünler minProgress ile oyunun sonuna doğru açılır.
/// </summary>
public class BKItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public BKFallingItem prefab;

        [Tooltip("Göreli çıkma olasılığı (diğer ürünlere göre)")]
        public float weight = 10f;

        [Range(0f, 1f)]
        [Tooltip("Oyunun bu oranından sonra düşmeye başlar (0 = baştan itibaren)")]
        public float minProgress = 0f;

        [Range(0f, 1f)]
        [Tooltip("Oyunun bu oranından sonra düşmeyi bırakır (1 = sona kadar)")]
        public float maxProgress = 1f;
    }

    [Header("Ürünler")]
    public List<SpawnEntry> entries = new List<SpawnEntry>();

    [Header("Konum")]
    [Tooltip("Spawner merkezinden sağa/sola en fazla bu kadar uzağa düşer")]
    public float spawnHalfWidth = 7.8f;

    [Tooltip("Oluşturulan ürünlerin toplandığı parent (boş bırakılabilir)")]
    public Transform itemsRoot;

    [Tooltip("Art arda iki ürün en az bu kadar farklı X'e düşsün (0 = kapalı)")]
    public float minSpawnSeparationX = 1.5f;

    [Header("Zorluk (progress 0 → 1)")]
    [Tooltip("Oyun başındaki spawn aralığı (saniye)")]
    public float spawnIntervalStart = 1.2f;

    [Tooltip("Oyun sonundaki spawn aralığı (saniye)")]
    public float spawnIntervalEnd = 0.55f;

    [Tooltip("Oyun başındaki düşme hızı (birim/sn)")]
    public float fallSpeedStart = 4f;

    [Tooltip("Oyun sonundaki düşme hızı (birim/sn)")]
    public float fallSpeedEnd = 9f;

    [Range(0f, 0.5f)]
    [Tooltip("Her ürünün hızına eklenen rastgele sapma oranı")]
    public float speedVariation = 0.15f;

    [Tooltip("progress → zorluk eğrisi (varsayılan doğrusal)")]
    public AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Görsel")]
    [Tooltip("Ürünlerin en fazla dönüş hızı (derece/sn)")]
    public float maxSpin = 120f;

    [Header("Kurallar")]
    [Tooltip("Art arda iki bomba düşmesin")]
    public bool avoidConsecutiveBombs = true;

    [Tooltip("İlk ürün oyun başladıktan kaç saniye sonra düşsün")]
    public float firstSpawnDelay = 0.6f;

    public float Progress { get; private set; }
    public bool IsSpawning { get; private set; }
    public int LiveCount => live.Count;

    public float CurrentFallSpeed => Mathf.Lerp(fallSpeedStart, fallSpeedEnd, Difficulty);
    public float CurrentInterval => Mathf.Max(0.05f, Mathf.Lerp(spawnIntervalStart, spawnIntervalEnd, Difficulty));
    float Difficulty => difficultyCurve != null ? Mathf.Clamp01(difficultyCurve.Evaluate(Progress)) : Progress;

    private float nextSpawnTime;
    private float lastX = float.NaN;
    private bool lastWasBomb;
    private readonly List<BKFallingItem> live = new List<BKFallingItem>();

    public void Begin()
    {
        IsSpawning = true;
        Progress = 0f;
        lastWasBomb = false;
        lastX = float.NaN;
        nextSpawnTime = Time.time + firstSpawnDelay;
    }

    public void Stop()
    {
        IsSpawning = false;
    }

    public void SetProgress(float progress)
    {
        Progress = Mathf.Clamp01(progress);
    }

    /// <summary>Sahnedeki tüm canlı ürünleri siler.</summary>
    public void ClearAll()
    {
        BKFallingItem[] copy = live.ToArray();
        live.Clear();
        foreach (BKFallingItem item in copy)
        {
            if (item) Destroy(item.gameObject);
        }
    }

    void Update()
    {
        if (!IsSpawning) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnOne();
            nextSpawnTime = Time.time + CurrentInterval;
        }
    }

    void SpawnOne()
    {
        SpawnEntry entry = PickEntry();
        if (entry == null) return;

        float x = RandomX();
        if (minSpawnSeparationX > 0f && !float.IsNaN(lastX) && Mathf.Abs(x - lastX) < minSpawnSeparationX)
        {
            x = RandomX(); // tek deneme daha; yeterince dağıtır
        }

        Vector3 pos = new Vector3(x, transform.position.y, 0f);
        BKFallingItem item = Instantiate(entry.prefab, pos, Quaternion.identity, itemsRoot);

        float speed = CurrentFallSpeed * Random.Range(1f - speedVariation, 1f + speedVariation);
        item.Launch(speed, Random.Range(-maxSpin, maxSpin));

        live.Add(item);
        item.Destroyed += OnItemDestroyed;

        lastX = x;
        lastWasBomb = item.IsBomb;
    }

    float RandomX()
    {
        return transform.position.x + Random.Range(-spawnHalfWidth, spawnHalfWidth);
    }

    void OnItemDestroyed(BKFallingItem item)
    {
        live.Remove(item);
    }

    SpawnEntry PickEntry()
    {
        float total = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            if (IsEligible(entries[i])) total += entries[i].weight;
        }
        if (total <= 0f)
        {
            // Yalnızca bombalar uygunsa "art arda bomba" kuralı spawn'ı tamamen kilitlemesin
            if (avoidConsecutiveBombs && lastWasBomb)
            {
                lastWasBomb = false;
                return PickEntry();
            }
            return null;
        }

        float r = Random.value * total;
        for (int i = 0; i < entries.Count; i++)
        {
            SpawnEntry e = entries[i];
            if (!IsEligible(e)) continue;
            r -= e.weight;
            if (r <= 0f) return e;
        }

        // Kayan nokta artığı: son uygun girdiyi döndür
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (IsEligible(entries[i])) return entries[i];
        }
        return null;
    }

    bool IsEligible(SpawnEntry e)
    {
        if (e == null || e.prefab == null || e.weight <= 0f) return false;
        if (Progress < e.minProgress || Progress > e.maxProgress) return false;
        if (avoidConsecutiveBombs && lastWasBomb && e.prefab.IsBomb) return false;
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 c = transform.position;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        Gizmos.DrawLine(c + Vector3.left * spawnHalfWidth, c + Vector3.right * spawnHalfWidth);
        Gizmos.DrawWireSphere(c, 0.3f);
    }
}
