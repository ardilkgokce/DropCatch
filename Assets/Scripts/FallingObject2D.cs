using UnityEngine;

public class FallingObject2D : MonoBehaviour
{
    public int pointValue = 10;
    public float fallSpeed = 5f;
    
    [Header("Görsel Efektler")]
    public GameObject collectEffect;
    public AudioClip collectSound;
    
    private Rigidbody2D rb;
    
    void Start()
    {
        fallSpeed = Random.Range(2f, 5f);
        rb = GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.down * fallSpeed;
        
        // Rastgele rotasyon ekle
        rb.angularVelocity = Random.Range(-180f, 180f);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Basket"))
        {
            // Hangi oyuncunun sepeti olduğunu belirle
            PlayerBasket playerBasket = other.GetComponent<PlayerBasket>();

            if (playerBasket != null)
            {
                // Player-specific puan ekle
                GameManager2D.Instance.AddScore(playerBasket.playerIndex, pointValue);
            }
            else
            {
                // Fallback: PlayerBasket componenti yoksa varsayılan olarak Player 0'a ekle
                Debug.LogWarning("Basket'te PlayerBasket componenti bulunamadı! Player 0'a puan ekleniyor.");
                GameManager2D.Instance.AddScore(0, pointValue);
            }

            // Basket sprite'ını güncelle
            BasketController2D basketController = other.GetComponentInParent<BasketController2D>();
            if (basketController != null)
            {
                basketController.OnObjectCaught();
            }
            else
            {
                Debug.LogWarning("BasketController2D bulunamadı! Sprite güncellenemedi.");
            }

            // Efekt oluştur
            if(collectEffect)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }

            // Ses çal
            if(collectSound)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            Destroy(gameObject);
        }
        else if(other.CompareTag("DeathZone"))
        {
            Destroy(gameObject);
        }
    }
}