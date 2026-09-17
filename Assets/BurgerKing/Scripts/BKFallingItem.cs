using System.Collections;
using UnityEngine;

/// <summary>
/// Düşen Burger King ürünü / bomba. Spawner tarafından hız verilir; sepete veya DeathZone'a çarpınca
/// BKGameManager'a haber verir. Yakalanınca sepetin içine kayarak kaybolur.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BKFallingItem : MonoBehaviour
{
    [Header("Ürün")]
    public BKItemType itemType = BKItemType.Tursu;

    [Tooltip("Yakalandığında kazandırdığı puan (bomba için ceza BKGameManager.bombPenalty'den okunur)")]
    public int points = 1;

    public bool IsBomb => itemType == BKItemType.Bomba;
    public bool IsFullMenu => itemType == BKItemType.TamMenu;

    /// <summary>Obje yok edildiğinde tetiklenir (spawner canlı listesini günceller).</summary>
    public event System.Action<BKFallingItem> Destroyed;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private bool resolved;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0f;
    }

    /// <summary>Sabit düşme hızı ve dönüş ver (spawner çağırır).</summary>
    public void Launch(float fallSpeed, float spinDegreesPerSecond)
    {
        rb.velocity = Vector2.down * fallSpeed;
        rb.angularVelocity = spinDegreesPerSecond;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (resolved) return;

        if (other.CompareTag("Basket"))
        {
            resolved = true;
            if (BKGameManager.Instance != null)
                BKGameManager.Instance.OnItemCaught(this, other.transform);
            else
                Destroy(gameObject);
        }
        else if (other.CompareTag("DeathZone"))
        {
            resolved = true;
            if (BKGameManager.Instance != null)
                BKGameManager.Instance.OnItemMissed(this);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sepete girme animasyonu: fiziği kapatır, sepetin (hareket etse bile) içine doğru küçülerek kayar ve yok olur.
    /// </summary>
    public void PlayCaughtAnimation(Transform bag, Vector3 bagOffset, float duration, int sortingOrderBehindBag)
    {
        if (col) col.enabled = false;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
        if (sr) sr.sortingOrder = sortingOrderBehindBag;
        StartCoroutine(CaughtRoutine(bag, bagOffset, Mathf.Max(0.05f, duration)));
    }

    IEnumerator CaughtRoutine(Transform bag, Vector3 bagOffset, float duration)
    {
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        Quaternion startRot = transform.rotation;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = BKEase.InQuad(t / duration);
            Vector3 target = bag ? bag.position + bagOffset : startPos;
            transform.position = Vector3.Lerp(startPos, target, k);
            transform.localScale = Vector3.Lerp(startScale, startScale * 0.35f, k);
            transform.rotation = Quaternion.Slerp(startRot, Quaternion.identity, k);
            yield return null;
        }

        Destroy(gameObject);
    }

    /// <summary>Anında yok et (bomba veya oyun dışı durum).</summary>
    public void Vanish()
    {
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        Destroyed?.Invoke(this);
    }
}
