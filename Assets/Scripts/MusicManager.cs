using UnityEngine;

/// <summary>
/// Oyun boyunca arka plan müziğini ve özel anlar için ses efektlerini yönetir.
/// Singleton pattern ile tüm sesler tek bir yerden kontrol edilir.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Audio Source Referansları")]
    [Tooltip("Müzik için AudioSource içeren GameObject")]
    public GameObject musicSourceObject;
    [Tooltip("SFX için AudioSource içeren GameObject")]
    public GameObject sfxSourceObject;

    [Header("Müzik Ayarları")]
    [Tooltip("Sürekli çalacak arka plan müziği (loop)")]
    public AudioClip backgroundMusic;

    [Header("Oyun Sonu Sesleri")]
    [Tooltip("Oyun sonu kutlama sesi")]
    public AudioClip endGameSound;

    [Header("Ses Yüksekliği Ayarları")]
    [Tooltip("Arka plan müziği başlangıç ses yüksekliği")]
    [Range(0f, 1f)]
    public float backgroundMusicVolume = 1.0f;

    [Tooltip("Oyun sonu müzik ses yüksekliği (kısılmış)")]
    [Range(0f, 1f)]
    public float backgroundMusicLoweredVolume = 0.2f;

    [Tooltip("Ses efektleri (SFX) ses yüksekliği")]
    [Range(0f, 1f)]
    public float sfxVolume = 1.0f;

    private AudioSource musicSource;  // Arka plan müziği için
    private AudioSource sfxSource;    // Ses efektleri için

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // AudioSource'ları GameObject referanslarından al
        if (musicSourceObject != null)
        {
            musicSource = musicSourceObject.GetComponent<AudioSource>();
            if (musicSource == null)
            {
                Debug.LogError("MusicManager: musicSourceObject'te AudioSource componenti bulunamadı!");
            }
            else
            {
                musicSource.playOnAwake = false;
            }
        }
        else
        {
            Debug.LogError("MusicManager: musicSourceObject referansı atanmamış!");
        }

        if (sfxSourceObject != null)
        {
            sfxSource = sfxSourceObject.GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                Debug.LogError("MusicManager: sfxSourceObject'te AudioSource componenti bulunamadı!");
            }
            else
            {
                sfxSource.playOnAwake = false;
            }
        }
        else
        {
            Debug.LogError("MusicManager: sfxSourceObject referansı atanmamış!");
        }
    }

    void Start()
    {
        PlayBackgroundMusic();
    }

    /// <summary>
    /// Arka plan müziğini loop olarak başlatır
    /// </summary>
    void PlayBackgroundMusic()
    {
        if (backgroundMusic && musicSource)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = backgroundMusicVolume;
            musicSource.Play();
            Debug.Log($"MusicManager: Arka plan müziği başlatıldı (loop, volume: {backgroundMusicVolume})");
        }
        else
        {
            Debug.LogWarning("MusicManager: Background music clip atanmamış!");
        }
    }

    /// <summary>
    /// Müzik volume seviyesini ayarlar
    /// </summary>
    /// <param name="lowered">True ise kısılmış volume, false ise normal volume</param>
    public void SetMusicVolume(bool lowered)
    {
        if (musicSource)
        {
            musicSource.volume = lowered ? backgroundMusicLoweredVolume : backgroundMusicVolume;
            Debug.Log($"MusicManager: Müzik volume: {musicSource.volume} ({(lowered ? "Kısık" : "Normal")})");
        }
    }

    /// <summary>
    /// EndGame kutlama sesi çalar
    /// </summary>
    public void PlayEndGameSound()
    {
        if (endGameSound && sfxSource)
        {
            sfxSource.PlayOneShot(endGameSound, sfxVolume);
            Debug.Log($"MusicManager: EndGame sesi çalıyor (volume: {sfxVolume})");
        }
        else
        {
            Debug.LogWarning("MusicManager: EndGame sound clip atanmamış!");
        }
    }
}
