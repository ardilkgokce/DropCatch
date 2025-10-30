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
        rb = GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.down * fallSpeed;
        
        // Rastgele rotasyon ekle
        rb.angularVelocity = Random.Range(-180f, 180f);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Basket"))
        {
            // Puan ekle
            GameManager2D.Instance.AddScore(pointValue);
            
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