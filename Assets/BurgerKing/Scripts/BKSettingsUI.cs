using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ESC ile açılan saha ayar paneli. Arayüz çalışma zamanında kodla kurulur; değerler PlayerPrefs'e (BK_ öneki) kaydedilir
/// ve sahne açılışında uygulanır. Panel açıkken oyun durur (Time.timeScale = 0).
/// </summary>
public class BKSettingsUI : MonoBehaviour
{
    [Header("Referanslar")]
    public BKGameManager gameManager;
    public BKItemSpawner spawner;
    public BasketController2D basketController;
    public Canvas canvas;

    [Header("Görünüm")]
    public TMP_FontAsset font;
    public Sprite panelSprite;
    public Sprite sliderSprite;
    public Sprite knobSprite;
    public Color panelColor = new Color(0.96f, 0.92f, 0.86f, 1f);
    public Color textColor = new Color(0.1f, 0.09f, 0.08f, 1f);
    public Color accentColor = new Color(0.82f, 0.15f, 0.12f, 1f);

    [Header("Davranış")]
    public KeyCode toggleKey = KeyCode.Escape;
    public bool pauseWhileOpen = true;

    private const string PREFIX = "BK_";

    private class Entry
    {
        public string label;
        public string key;
        public float min, max;
        public int decimals;
        public Func<float> get;
        public Action<float> set;
        public float defaultValue;
        public Slider slider;
        public TextMeshProUGUI valueText;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private GameObject panel;
    private bool isOpen;
    private bool wasFocusedLastFrame; // TMP alanı ESC'yi aynı karede bırakabilir; önceki karenin odağını da say

    void Start()
    {
        BuildEntries();
        LoadAndApply();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey) && !IsTextFieldFocused() && !wasFocusedLastFrame) Toggle();
    }

    void LateUpdate()
    {
        wasFocusedLastFrame = IsTextFieldFocused();
    }

    /// <summary>İsim yazılırken ESC alanı iptal eder; ayar paneli açılmasın.</summary>
    static bool IsTextFieldFocused()
    {
        EventSystem es = EventSystem.current;
        if (!es || !es.currentSelectedGameObject) return false;
        TMP_InputField field = es.currentSelectedGameObject.GetComponent<TMP_InputField>();
        return field && field.isFocused;
    }

    // ------------------------------------------------------------------

    void BuildEntries()
    {
        entries.Clear();

        if (gameManager)
        {
            Add("Oyun süresi (sn)", "GameDuration", 20, 180, 0, () => gameManager.gameDuration, v => gameManager.gameDuration = v);
            Add("Geri sayım (sn)", "Countdown", 0, 5, 0, () => gameManager.countdownSeconds, v => gameManager.countdownSeconds = Mathf.RoundToInt(v));
            Add("Bomba cezası", "BombPenalty", 0, 30, 0, () => gameManager.bombPenalty, v => gameManager.bombPenalty = Mathf.RoundToInt(v));
            Add("Menü bonusu", "MenuBonus", 0, 200, 0, () => gameManager.menuBonusPoints, v => gameManager.menuBonusPoints = Mathf.RoundToInt(v));
            Add("Bitiş ekranı (sn)", "EndScreen", 3, 60, 0, () => gameManager.endScreenDuration, v => gameManager.endScreenDuration = v);
            Add("Liderlik ekranı (sn)", "LeaderboardScreen", 2, 60, 0, () => gameManager.leaderboardScreenDuration, v => gameManager.leaderboardScreenDuration = v);
        }

        if (spawner)
        {
            Add("Spawn aralığı başlangıç", "SpawnStart", 0.2f, 3f, 2, () => spawner.spawnIntervalStart, v => spawner.spawnIntervalStart = v);
            Add("Spawn aralığı bitiş", "SpawnEnd", 0.2f, 3f, 2, () => spawner.spawnIntervalEnd, v => spawner.spawnIntervalEnd = v);
            Add("Düşme hızı başlangıç", "SpeedStart", 1f, 15f, 1, () => spawner.fallSpeedStart, v => spawner.fallSpeedStart = v);
            Add("Düşme hızı bitiş", "SpeedEnd", 1f, 20f, 1, () => spawner.fallSpeedEnd, v => spawner.fallSpeedEnd = v);
        }

        if (basketController)
        {
            Add("Sepet yatay aralık", "HorizontalRange", 1f, 9f, 1, () => basketController.horizontalRange, v => basketController.horizontalRange = v);
            Add("Koordinat ölçeği", "CoordScale", 1f, 30f, 1, () => basketController.coordinateScale, v => basketController.coordinateScale = v);
            Add("Hareket hassasiyeti", "Sensitivity", 0.05f, 2f, 2, () => basketController.movementSensitivity, v => basketController.movementSensitivity = v);
            Add("Yumuşatma hızı", "Smoothing", 1f, 20f, 1, () => basketController.smoothingSpeed, v => basketController.smoothingSpeed = v);
        }

        if (gameManager && gameManager.sfxSource)
        {
            Add("Efekt sesi", "SfxVolume", 0f, 1f, 2, () => gameManager.sfxSource.volume, v => gameManager.sfxSource.volume = v);
        }

        Add("Müzik sesi", "MusicVolume", 0f, 1f, 2,
            () => MusicManager.Instance ? MusicManager.Instance.backgroundMusicVolume : 0.5f,
            v =>
            {
                if (!MusicManager.Instance) return;
                MusicManager.Instance.backgroundMusicVolume = v;
                MusicManager.Instance.SetMusicVolume(false);
            });
    }

    void Add(string label, string key, float min, float max, int decimals, Func<float> get, Action<float> set)
    {
        entries.Add(new Entry
        {
            label = label, key = key, min = min, max = max, decimals = decimals,
            get = get, set = set, defaultValue = get()
        });
    }

    void LoadAndApply()
    {
        foreach (Entry e in entries)
        {
            string k = PREFIX + e.key;
            if (PlayerPrefs.HasKey(k))
            {
                e.set(Mathf.Clamp(PlayerPrefs.GetFloat(k), e.min, e.max));
            }
        }
    }

    public void SaveAll()
    {
        foreach (Entry e in entries)
        {
            PlayerPrefs.SetFloat(PREFIX + e.key, e.get());
        }
        PlayerPrefs.Save();
        Debug.Log("BKSettingsUI: ayarlar kaydedildi");
    }

    public void ResetDefaults()
    {
        foreach (Entry e in entries)
        {
            e.set(e.defaultValue);
            PlayerPrefs.DeleteKey(PREFIX + e.key);
        }
        PlayerPrefs.Save();
        RefreshUI();
    }

    public void Toggle()
    {
        if (!panel) BuildPanel();
        if (!panel) return;

        isOpen = !isOpen;
        panel.SetActive(isOpen);
        if (pauseWhileOpen) Time.timeScale = isOpen ? 0f : 1f;
        if (isOpen) RefreshUI();
    }

    public void Close()
    {
        if (!isOpen) return;
        Toggle();
    }

    void RefreshUI()
    {
        foreach (Entry e in entries)
        {
            if (!e.slider) continue;
            e.slider.SetValueWithoutNotify(e.get());
            UpdateValueText(e);
        }
    }

    void UpdateValueText(Entry e)
    {
        if (e.valueText) e.valueText.text = e.get().ToString("F" + e.decimals);
    }

    // ------------------------------------------------------------------
    // Arayüz kurulumu
    // ------------------------------------------------------------------

    void BuildPanel()
    {
        if (!canvas)
        {
            Debug.LogWarning("BKSettingsUI: canvas atanmadı");
            return;
        }

        panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform pr = (RectTransform)panel.transform;
        pr.anchorMin = Vector2.zero;
        pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        box.transform.SetParent(panel.transform, false);
        RectTransform br = (RectTransform)box.transform;
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
        br.pivot = new Vector2(0.5f, 0.5f);
        br.sizeDelta = new Vector2(980f, 1720f);
        Image boxImg = box.GetComponent<Image>();
        boxImg.sprite = panelSprite;
        boxImg.type = Image.Type.Sliced;
        boxImg.color = panelColor;

        VerticalLayoutGroup vl = box.GetComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(40, 40, 36, 36);
        vl.spacing = 8f;
        vl.childAlignment = TextAnchor.UpperCenter;
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        TextMeshProUGUI title = MakeText(box.transform, "AYARLAR  (ESC: kapat)", 52, TextAlignmentOptions.Center, accentColor);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 80f;

        foreach (Entry e in entries) BuildRow(box.transform, e);

        GameObject buttons = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        buttons.transform.SetParent(box.transform, false);
        HorizontalLayoutGroup hl = buttons.GetComponent<HorizontalLayoutGroup>();
        hl.spacing = 20f;
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = true;
        hl.childForceExpandHeight = false;
        buttons.AddComponent<LayoutElement>().preferredHeight = 96f;

        MakeButton(buttons.transform, "KAYDET", () => { SaveAll(); Close(); });
        MakeButton(buttons.transform, "VARSAYILAN", ResetDefaults);
        MakeButton(buttons.transform, "KAPAT", Close);
    }

    void BuildRow(Transform parent, Entry e)
    {
        GameObject row = new GameObject(e.key, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup hl = row.GetComponent<HorizontalLayoutGroup>();
        hl.spacing = 16f;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;
        row.AddComponent<LayoutElement>().preferredHeight = 78f;

        TextMeshProUGUI label = MakeText(row.transform, e.label, 32, TextAlignmentOptions.Left, textColor);
        label.gameObject.AddComponent<LayoutElement>().preferredWidth = 400f;

        DefaultControls.Resources res = new DefaultControls.Resources
        {
            standard = sliderSprite, background = sliderSprite, knob = knobSprite
        };
        GameObject sliderGo = DefaultControls.CreateSlider(res);
        sliderGo.transform.SetParent(row.transform, false);
        LayoutElement sl = sliderGo.AddComponent<LayoutElement>();
        sl.flexibleWidth = 1f;
        sl.preferredHeight = 40f;

        Slider slider = sliderGo.GetComponent<Slider>();
        slider.minValue = e.min;
        slider.maxValue = e.max;
        slider.wholeNumbers = e.decimals == 0;
        slider.SetValueWithoutNotify(e.get());
        Image fill = slider.fillRect ? slider.fillRect.GetComponent<Image>() : null;
        if (fill) fill.color = accentColor;

        TextMeshProUGUI value = MakeText(row.transform, "", 32, TextAlignmentOptions.Right, textColor);
        value.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;

        e.slider = slider;
        e.valueText = value;
        UpdateValueText(e);

        slider.onValueChanged.AddListener(v =>
        {
            e.set(v);
            UpdateValueText(e);
        });
    }

    TextMeshProUGUI MakeText(Transform parent, string text, float size, TextAlignmentOptions align, Color color)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    void MakeButton(Transform parent, string label, Action onClick)
    {
        GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = panelSprite;
        img.type = Image.Type.Sliced;
        img.color = accentColor;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick());

        TextMeshProUGUI t = MakeText(go.transform, label, 36, TextAlignmentOptions.Center, Color.white);
        RectTransform tr = t.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
    }
}
