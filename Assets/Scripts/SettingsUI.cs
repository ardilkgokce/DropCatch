using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("Components")]
    public GameObject settingsPanel;
    public GameManager2D gameManager;
    public BasketController2D basketController_P0;
    public BasketController2D basketController_P1;
    public ObjectSpawner2D spawner_Left;
    public ObjectSpawner2D spawner_Right;

    [Header("EndGameDuration")]
    public Slider endGameDurationSlider;
    public TMP_InputField endGameDurationInput;

    [Header("GameDuration")]
    public Slider gameDurationSlider;
    public TMP_InputField gameDurationInput;

    [Header("SpawnRate")]
    public Slider spawnRateSlider;
    public TMP_InputField spawnRateInput;

    [Header("HorizontalRange")]
    public Slider horizontalRangeSlider;
    public TMP_InputField horizontalRangeInput;

    [Header("CoordinateScale")]
    public Slider coordinateScaleSlider;
    public TMP_InputField coordinateScaleInput;

    [Header("MovementSensitivity")]
    public Slider movementSensitivitySlider;
    public TMP_InputField movementSensitivityInput;

    [Header("SmoothingSpeed")]
    public Slider smoothingSpeedSlider;
    public TMP_InputField smoothingSpeedInput;

    // PlayerPrefs Keys
    private const string KEY_END_GAME_DURATION = "Settings_EndGameDuration";
    private const string KEY_GAME_DURATION = "Settings_GameDuration";
    private const string KEY_SPAWN_RATE = "Settings_SpawnRate";
    private const string KEY_HORIZONTAL_RANGE = "Settings_HorizontalRange";
    private const string KEY_COORDINATE_SCALE = "Settings_CoordinateScale";
    private const string KEY_MOVEMENT_SENSITIVITY = "Settings_MovementSensitivity";
    private const string KEY_SMOOTHING_SPEED = "Settings_SmoothingSpeed";

    // Slider range değerleri
    private struct SliderRange
    {
        public float min;
        public float max;
        public SliderRange(float min, float max) { this.min = min; this.max = max; }
    }

    private SliderRange endGameDurationRange = new SliderRange(0f, 10f);
    private SliderRange gameDurationRange = new SliderRange(30f, 180f);
    private SliderRange spawnRateRange = new SliderRange(0.5f, 5f);
    private SliderRange horizontalRangeRange = new SliderRange(2f, 15f);
    private SliderRange coordinateScaleRange = new SliderRange(1f, 10f);
    private SliderRange movementSensitivityRange = new SliderRange(0.1f, 3f);
    private SliderRange smoothingSpeedRange = new SliderRange(1f, 20f);

    void Start()
    {
        // Panel'i gizle
        if (settingsPanel)
        {
            settingsPanel.SetActive(false);
        }

        // Birkaç frame bekle sonra ayarları yükle (component'ler hazır olsun)
        StartCoroutine(InitializeAfterDelay());
    }

    System.Collections.IEnumerator InitializeAfterDelay()
    {
        // 2 frame bekle (component'lerin değerleri uygulanması için)
        yield return null;
        yield return null;

        // Kaydedilmiş ayarları yükle
        LoadSettings();

        // UI'ı başlat
        InitializeUI();

        // Input field'ları manuel güncelle
        UpdateAllInputFields();

        Debug.Log("SettingsUI başlatıldı ve input field'lar güncellendi");
    }

    void Update()
    {
        // ESC tuşu ile panel aç/kapa
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePanel();
        }
    }

    void InitializeUI()
    {
        // Slider range'lerini ayarla
        SetSliderRange(endGameDurationSlider, endGameDurationRange);
        SetSliderRange(gameDurationSlider, gameDurationRange);
        SetSliderRange(spawnRateSlider, spawnRateRange);
        SetSliderRange(horizontalRangeSlider, horizontalRangeRange);
        SetSliderRange(coordinateScaleSlider, coordinateScaleRange);
        SetSliderRange(movementSensitivitySlider, movementSensitivityRange);
        SetSliderRange(smoothingSpeedSlider, smoothingSpeedRange);

        // Slider listener'ları ekle
        endGameDurationSlider.onValueChanged.AddListener(val => OnSliderChanged(endGameDurationInput, val, 1));
        gameDurationSlider.onValueChanged.AddListener(val => OnSliderChanged(gameDurationInput, val, 0));
        spawnRateSlider.onValueChanged.AddListener(val => OnSliderChanged(spawnRateInput, val, 2));
        horizontalRangeSlider.onValueChanged.AddListener(val => OnSliderChanged(horizontalRangeInput, val, 1));
        coordinateScaleSlider.onValueChanged.AddListener(val => OnSliderChanged(coordinateScaleInput, val, 1));
        movementSensitivitySlider.onValueChanged.AddListener(val => OnSliderChanged(movementSensitivityInput, val, 2));
        smoothingSpeedSlider.onValueChanged.AddListener(val => OnSliderChanged(smoothingSpeedInput, val, 1));

        // Input field listener'ları ekle
        endGameDurationInput.onValueChanged.AddListener(text => OnInputChanged(endGameDurationSlider, endGameDurationRange, text));
        gameDurationInput.onValueChanged.AddListener(text => OnInputChanged(gameDurationSlider, gameDurationRange, text));
        spawnRateInput.onValueChanged.AddListener(text => OnInputChanged(spawnRateSlider, spawnRateRange, text));
        horizontalRangeInput.onValueChanged.AddListener(text => OnInputChanged(horizontalRangeSlider, horizontalRangeRange, text));
        coordinateScaleInput.onValueChanged.AddListener(text => OnInputChanged(coordinateScaleSlider, coordinateScaleRange, text));
        movementSensitivityInput.onValueChanged.AddListener(text => OnInputChanged(movementSensitivitySlider, movementSensitivityRange, text));
        smoothingSpeedInput.onValueChanged.AddListener(text => OnInputChanged(smoothingSpeedSlider, smoothingSpeedRange, text));

        // Başlangıç değerlerini göster
        LoadCurrentValues();
    }

    void SetSliderRange(Slider slider, SliderRange range)
    {
        if (slider)
        {
            slider.minValue = range.min;
            slider.maxValue = range.max;
        }
    }

    void OnSliderChanged(TMP_InputField inputField, float value, int decimals)
    {
        if (inputField)
        {
            if (decimals == 0)
            {
                inputField.text = Mathf.RoundToInt(value).ToString();
            }
            else
            {
                inputField.text = value.ToString("F" + decimals);
            }
        }
    }

    void OnInputChanged(Slider slider, SliderRange range, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (float.TryParse(text, out float value))
        {
            // Değeri clamp et
            value = Mathf.Clamp(value, range.min, range.max);

            // Slider'ı güncelle
            if (slider)
            {
                slider.value = value;
            }
        }
    }

    public void TogglePanel()
    {
        if (!settingsPanel) return;

        bool isActive = !settingsPanel.activeSelf;
        settingsPanel.SetActive(isActive);

        if (isActive)
        {
            // Panel açıldı - oyunu durdur ve güncel değerleri yükle
            Time.timeScale = 0f;
            LoadCurrentValues();
            Debug.Log("Settings panel açıldı (ESC ile kapat)");
        }
        else
        {
            // Panel kapandı - oyunu devam ettir
            Time.timeScale = 1f;
            Debug.Log("Settings panel kapatıldı");
        }
    }

    void LoadCurrentValues()
    {
        // GameManager'dan değerleri çek
        if (gameManager)
        {
            endGameDurationSlider.value = gameManager.endGamePanelDuration;
            gameDurationSlider.value = gameManager.gameDuration;
        }

        // Spawner'lardan değerleri çek (ilk spawner'ı referans al)
        if (spawner_Left)
        {
            spawnRateSlider.value = spawner_Left.spawnRate;
        }

        // Basket controller'lardan değerleri çek (ilk basket'ı referans al)
        if (basketController_P0)
        {
            horizontalRangeSlider.value = basketController_P0.horizontalRange;
            coordinateScaleSlider.value = basketController_P0.coordinateScale;
            movementSensitivitySlider.value = basketController_P0.movementSensitivity;
            smoothingSpeedSlider.value = basketController_P0.smoothingSpeed;
        }

        // Input field'ları da güncelle
        UpdateAllInputFields();
    }

    void UpdateAllInputFields()
    {
        // Tüm input field'ları slider değerlerine göre güncelle
        OnSliderChanged(endGameDurationInput, endGameDurationSlider.value, 1);
        OnSliderChanged(gameDurationInput, gameDurationSlider.value, 0);
        OnSliderChanged(spawnRateInput, spawnRateSlider.value, 2);
        OnSliderChanged(horizontalRangeInput, horizontalRangeSlider.value, 1);
        OnSliderChanged(coordinateScaleInput, coordinateScaleSlider.value, 1);
        OnSliderChanged(movementSensitivityInput, movementSensitivitySlider.value, 3);
        OnSliderChanged(smoothingSpeedInput, smoothingSpeedSlider.value, 1);
    }

    public void SaveSettings()
    {
        // Değerleri PlayerPrefs'e kaydet
        PlayerPrefs.SetFloat(KEY_END_GAME_DURATION, endGameDurationSlider.value);
        PlayerPrefs.SetFloat(KEY_GAME_DURATION, gameDurationSlider.value);
        PlayerPrefs.SetFloat(KEY_SPAWN_RATE, spawnRateSlider.value);
        PlayerPrefs.SetFloat(KEY_HORIZONTAL_RANGE, horizontalRangeSlider.value);
        PlayerPrefs.SetFloat(KEY_COORDINATE_SCALE, coordinateScaleSlider.value);
        PlayerPrefs.SetFloat(KEY_MOVEMENT_SENSITIVITY, movementSensitivitySlider.value);
        PlayerPrefs.SetFloat(KEY_SMOOTHING_SPEED, smoothingSpeedSlider.value);
        PlayerPrefs.Save();

        // Ayarları uygula
        ApplySettings(
            endGameDurationSlider.value,
            gameDurationSlider.value,
            spawnRateSlider.value,
            horizontalRangeSlider.value,
            coordinateScaleSlider.value,
            movementSensitivitySlider.value,
            smoothingSpeedSlider.value
        );

        // Panel'i kapat
        if (settingsPanel)
        {
            settingsPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        Debug.Log("Settings kaydedildi ve uygulandı!");
    }

    void LoadSettings()
    {
        // Inspector'dan default değerleri al (component'lerde ayarlanmış değerler)
        float defaultEndGameDuration = gameManager ? gameManager.endGamePanelDuration : 5f;
        float defaultGameDuration = gameManager ? gameManager.gameDuration : 60f;
        float defaultSpawnRate = spawner_Left ? spawner_Left.spawnRate : 0.75f;
        float defaultHorizontalRange = basketController_P0 ? basketController_P0.horizontalRange : 5f;
        float defaultCoordinateScale = basketController_P0 ? basketController_P0.coordinateScale : 10f;
        float defaultMovementSensitivity = basketController_P0 ? basketController_P0.movementSensitivity : 0.175f;
        float defaultSmoothingSpeed = basketController_P0 ? basketController_P0.smoothingSpeed : 20f;

        // PlayerPrefs'ten yükle, yoksa inspector değerlerini kullan
        float endGameDuration = PlayerPrefs.GetFloat(KEY_END_GAME_DURATION, defaultEndGameDuration);
        float gameDuration = PlayerPrefs.GetFloat(KEY_GAME_DURATION, defaultGameDuration);
        float spawnRate = PlayerPrefs.GetFloat(KEY_SPAWN_RATE, defaultSpawnRate);
        float horizontalRange = PlayerPrefs.GetFloat(KEY_HORIZONTAL_RANGE, defaultHorizontalRange);
        float coordinateScale = PlayerPrefs.GetFloat(KEY_COORDINATE_SCALE, defaultCoordinateScale);
        float movementSensitivity = PlayerPrefs.GetFloat(KEY_MOVEMENT_SENSITIVITY, defaultMovementSensitivity);
        float smoothingSpeed = PlayerPrefs.GetFloat(KEY_SMOOTHING_SPEED, defaultSmoothingSpeed);

        // Slider'lara yükle
        if (endGameDurationSlider) endGameDurationSlider.value = endGameDuration;
        if (gameDurationSlider) gameDurationSlider.value = gameDuration;
        if (spawnRateSlider) spawnRateSlider.value = spawnRate;
        if (horizontalRangeSlider) horizontalRangeSlider.value = horizontalRange;
        if (coordinateScaleSlider) coordinateScaleSlider.value = coordinateScale;
        if (movementSensitivitySlider) movementSensitivitySlider.value = movementSensitivity;
        if (smoothingSpeedSlider) smoothingSpeedSlider.value = smoothingSpeed;

        // Ayarları uygula (DEĞİŞKENLERDEN)
        ApplySettings(endGameDuration, gameDuration, spawnRate, horizontalRange,
                      coordinateScale, movementSensitivity, smoothingSpeed);

        Debug.Log("Settings yüklendi ve uygulandı");
    }

    void ApplySettings(float endGameDur, float gameDur, float spawnRate,
                       float horizRange, float coordScale, float moveSens, float smoothSpeed)
    {
        // GameManager değerlerini güncelle
        if (gameManager)
        {
            gameManager.endGamePanelDuration = endGameDur;
            gameManager.gameDuration = gameDur;
        }

        // Spawner değerlerini güncelle (her iki spawner için)
        if (spawner_Left)
        {
            spawner_Left.spawnRate = spawnRate;
        }
        if (spawner_Right)
        {
            spawner_Right.spawnRate = spawnRate;
        }

        // Basket controller değerlerini güncelle (her iki basket için)
        if (basketController_P0)
        {
            basketController_P0.horizontalRange = horizRange;
            basketController_P0.coordinateScale = coordScale;
            basketController_P0.movementSensitivity = moveSens;
            basketController_P0.smoothingSpeed = smoothSpeed;
        }
        if (basketController_P1)
        {
            basketController_P1.horizontalRange = horizRange;
            basketController_P1.coordinateScale = coordScale;
            basketController_P1.movementSensitivity = moveSens;
            basketController_P1.smoothingSpeed = smoothSpeed;
        }

        Debug.Log("Settings uygulandı: " +
                  $"EndGame={endGameDur}s, " +
                  $"GameTime={gameDur}s, " +
                  $"SpawnRate={spawnRate}s, " +
                  $"HorizRange={horizRange}, " +
                  $"CoordScale={coordScale}, " +
                  $"MoveSens={moveSens}, " +
                  $"SmoothSpeed={smoothSpeed}");
    }
}
