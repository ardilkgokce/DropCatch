using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>
/// Burger King "King's Catch" sahnesini sıfırdan kurar:
/// sprite import ayarları → TMP font asset'leri → ürün prefab'ları → tam sahne (kamera, Kinect, sepet, spawner, HUD, ekranlar, ayarlar).
/// Menü: DropCatch > Burger King > Sahneyi Oluştur
/// Tekrar çalıştırmak sahneyi ve prefab'ları yeniden üretir (elle yapılan sahne düzenlemeleri kaybolur).
/// </summary>
public static class BKSceneBuilder
{
    const string ROOT = "Assets/BurgerKing";
    const string SPRITES = ROOT + "/Sprites";
    const string ITEMS = SPRITES + "/Items";
    const string UI = SPRITES + "/UI";
    const string BACKGROUNDS = SPRITES + "/Backgrounds";
    const string FONTS = ROOT + "/Fonts";
    const string PREFABS = ROOT + "/Prefabs";
    const string AUDIO = ROOT + "/Audio";
    const string SCENE_PATH = ROOT + "/Scenes/BurgerKingCatch.unity";

    // Bitiş / liderlik arka planı (finish_background.png, 1080x1920 canvas px): SKOR pill gövdesi ve krem panel
    const float END_PILL_TOP = 294f, END_PILL_H = 156f, END_PILL_W = 480f;
    const float END_PANEL_TOP = 510f, END_PANEL_BOTTOM = 1830f;

    // Dünya birimleri: kamera ortho size 19.2, 9:16 → görünür alan 21.6 x 38.4 (arka plan 2160x3840 px @ PPU 100)
    const float ORTHO_SIZE = 19.2f;
    // Krem oyun paneli sınırları (gameplay_background.png'den ölçüldü)
    const float PANEL_LEFT = -9.05f, PANEL_RIGHT = 9.04f, PANEL_TOP = 14.71f, PANEL_BOTTOM = -17.58f;
    const float BAG_SCALE = 0.36f;
    const float BAG_Y = -15.0f;

    static readonly Color BK_RED = new Color32(208, 38, 30, 255);
    static readonly Color CREAM = new Color32(245, 235, 220, 255);
    static readonly Color CREAM_DARK = new Color32(232, 219, 200, 255);
    static readonly Color DARK = new Color32(25, 22, 20, 255);
    static readonly Color SHADOW = new Color32(10, 10, 10, 255);

    class ItemDef
    {
        public string file;        // Sprites/Items altındaki dosya adı (uzantısız)
        public string name;        // prefab adı
        public BKItemType type;
        public int points;
        public float scale;
        public float weight;
        public float minProgress;

        public ItemDef(string file, string name, BKItemType type, int points, float scale, float weight, float minProgress)
        {
            this.file = file; this.name = name; this.type = type; this.points = points;
            this.scale = scale; this.weight = weight; this.minProgress = minProgress;
        }
    }

    // Puanlar tasarım dokümanından (1 en düşük → 10 en yüksek). Bomba puanı BKGameManager.bombPenalty'den okunur.
    static readonly ItemDef[] ITEM_DEFS =
    {
        new ItemDef("tursu",            "Tursu",        BKItemType.Tursu,        1,  0.30f, 10f, 0f),
        new ItemDef("sogan",            "Sogan",        BKItemType.Sogan,        2,  0.24f, 10f, 0f),
        new ItemDef("domates",          "Domates",      BKItemType.Domates,      3,  0.24f, 10f, 0f),
        new ItemDef("sogan_halkasi",    "SoganHalkasi", BKItemType.SoganHalkasi, 4,  0.28f, 10f, 0f),
        new ItemDef("patates",          "Patates",      BKItemType.Patates,      5,  0.30f, 10f, 0f),
        new ItemDef("marul",            "Marul",        BKItemType.Marul,        6,  0.24f, 10f, 0f),
        new ItemDef("kofte",            "Kofte",        BKItemType.Kofte,        7,  0.24f, 10f, 0f),
        new ItemDef("hamburger_ekmegi", "Ekmek",        BKItemType.Ekmek,        8,  0.24f, 10f, 0f),
        new ItemDef("bardak",           "Bardak",       BKItemType.Bardak,       9,  0.20f, 10f, 0f),
        new ItemDef("tamamlanan_menu",  "TamMenu",      BKItemType.TamMenu,      10, 0.15f, 4f,  0.5f),
        new ItemDef("bomba",            "Bomba",        BKItemType.Bomba,        0,  0.24f, 8f,  0.08f),
    };

    class Fonts
    {
        public TMP_FontAsset sans;   // FlameSans-Regular: gövde metinleri
        public TMP_FontAsset bold;   // Flame-Bold: vurgu
        public TMP_FontAsset display; // Flame-Regular
    }

    // ------------------------------------------------------------------
    // Menü
    // ------------------------------------------------------------------

    [MenuItem("DropCatch/Burger King/Sahneyi Oluştur (import + prefab + sahne)", false, 1)]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        try
        {
            EditorUtility.DisplayProgressBar("Burger King", "Asset'ler içe aktarılıyor...", 0.1f);
            AssetDatabase.Refresh();
            EnsureFolders();
            ImportSprites();
            ImportAudio();

            EditorUtility.DisplayProgressBar("Burger King", "Fontlar oluşturuluyor...", 0.3f);
            Fonts fonts = CreateFonts();

            EditorUtility.DisplayProgressBar("Burger King", "Prefab'lar oluşturuluyor...", 0.5f);
            Dictionary<string, BKFallingItem> prefabs = CreateItemPrefabs();
            BKFloatingText floating = CreateFloatingTextPrefab(fonts);

            EditorUtility.DisplayProgressBar("Burger King", "Sahne kuruluyor...", 0.7f);
            BuildScene(fonts, prefabs, floating);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem("DropCatch/Burger King/Sadece Asset'leri Yeniden İçe Aktar", false, 20)]
    public static void ReimportOnly()
    {
        AssetDatabase.Refresh();
        EnsureFolders();
        ImportSprites();
        CreateFonts();
        Debug.Log("Burger King asset'leri yeniden içe aktarıldı.");
    }

    // ------------------------------------------------------------------
    // Klasörler ve import
    // ------------------------------------------------------------------

    static void EnsureFolders()
    {
        EnsureFolder(ROOT, "Prefabs");
        EnsureFolder(PREFABS, "Items");
        EnsureFolder(PREFABS, "UI");
        EnsureFolder(ROOT, "Scenes");
        EnsureFolder(ROOT, "Audio");
    }

    static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }

    static void ImportSprites()
    {
        foreach (ItemDef d in ITEM_DEFS)
            ImportSprite(ITEMS + "/" + d.file + ".png", 100f, false, Vector4.zero, 2048, TextureImporterCompression.CompressedHQ);

        ImportSprite(ITEMS + "/sepet.png", 100f, false, Vector4.zero, 2048, TextureImporterCompression.CompressedHQ);
        ImportSprite(ITEMS + "/kofte_2.png", 100f, false, Vector4.zero, 2048, TextureImporterCompression.CompressedHQ);

        ImportSprite(BACKGROUNDS + "/gameplay_background.png", 100f, true, Vector4.zero, 4096, TextureImporterCompression.CompressedHQ);
        ImportSprite(BACKGROUNDS + "/start_background.png", 100f, true, Vector4.zero, 4096, TextureImporterCompression.CompressedHQ);
        ImportSprite(BACKGROUNDS + "/finish_background.png", 100f, true, Vector4.zero, 4096, TextureImporterCompression.CompressedHQ);

        ImportSprite(UI + "/burger_tutan_eller.png", 100f, true, Vector4.zero, 2048, TextureImporterCompression.CompressedHQ);
        ImportSprite(UI + "/ui_rounded_rect.png", 100f, true, new Vector4(64, 64, 64, 64), 256, TextureImporterCompression.Uncompressed);
        ImportSprite(UI + "/ui_pill.png", 100f, true, new Vector4(64, 64, 64, 64), 256, TextureImporterCompression.Uncompressed);
        ImportSprite(UI + "/ui_square.png", 100f, true, Vector4.zero, 128, TextureImporterCompression.Uncompressed);
        ImportSprite(UI + "/ui_circle.png", 100f, true, Vector4.zero, 128, TextureImporterCompression.Uncompressed);
    }

    /// <summary>Döngüde çalan puan sayma sesi PCM olsun: sıkıştırma ek yerinde boşluk/tık bırakmasın.</summary>
    static void ImportAudio()
    {
        foreach (string ext in new[] { ".wav", ".mp3", ".ogg" })
        {
            AudioImporter imp = AssetImporter.GetAtPath(AUDIO + "/bk_score_count" + ext) as AudioImporter;
            if (imp == null) continue;

            AudioImporterSampleSettings s = imp.defaultSampleSettings;
            if (s.compressionFormat == AudioCompressionFormat.PCM && s.loadType == AudioClipLoadType.DecompressOnLoad) continue;

            s.compressionFormat = AudioCompressionFormat.PCM;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            imp.defaultSampleSettings = s;
            imp.SaveAndReimport();
        }
    }

    static void ImportSprite(string path, float ppu, bool fullRect, Vector4 border, int maxSize, TextureImporterCompression compression)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null)
        {
            Debug.LogError("BKSceneBuilder: texture bulunamadı: " + path);
            return;
        }

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.spriteBorder = border;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.maxTextureSize = maxSize;
        imp.textureCompression = compression;
        imp.filterMode = FilterMode.Bilinear;
        imp.wrapMode = TextureWrapMode.Clamp;

        TextureImporterSettings settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteMeshType = fullRect ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
        settings.spriteBorder = border;
        settings.spritePixelsPerUnit = ppu;
        imp.SetTextureSettings(settings);

        imp.SaveAndReimport();
    }

    static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogError("BKSceneBuilder: sprite yüklenemedi: " + path);
        return s;
    }

    static readonly List<string> missingAudio = new List<string>();

    /// <summary>
    /// Assets/BurgerKing/Audio altındaki bk_* ses dosyasını (.wav/.mp3/.ogg) yükler.
    /// Yoksa (henüz üretilmediyse) fallback'i kullanır ve eksik listesine ekler.
    /// </summary>
    static AudioClip LoadSfx(string name, string fallbackPath = null)
    {
        foreach (string ext in new[] { ".wav", ".mp3", ".ogg" })
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO + "/" + name + ext);
            if (clip) return clip;
        }

        missingAudio.Add(name);
        return string.IsNullOrEmpty(fallbackPath) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(fallbackPath);
    }

    static T Load<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) Debug.LogWarning("BKSceneBuilder: asset bulunamadı (atlanıyor): " + path);
        return asset;
    }

    // ------------------------------------------------------------------
    // Fontlar
    // ------------------------------------------------------------------

    static Fonts CreateFonts()
    {
        return new Fonts
        {
            sans = CreateTmpFont(FONTS + "/FlameSans-Regular.ttf", FONTS + "/FlameSans-Regular SDF.asset"),
            bold = CreateTmpFont(FONTS + "/Flame-Bold.ttf", FONTS + "/Flame-Bold SDF.asset"),
            display = CreateTmpFont(FONTS + "/Flame-Regular.ttf", FONTS + "/Flame-Regular SDF.asset"),
        };
    }

    static TMP_FontAsset CreateTmpFont(string ttfPath, string assetPath)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        Font font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null)
        {
            Debug.LogError("BKSceneBuilder: font bulunamadı: " + ttfPath);
            return null;
        }

        // Dinamik atlas: Türkçe karakterler dahil tüm glifler ihtiyaç anında üretilir.
        TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        if (fa == null)
        {
            Debug.LogError("BKSceneBuilder: TMP font oluşturulamadı: " + ttfPath);
            return null;
        }

        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        fa.name = name;
        AssetDatabase.CreateAsset(fa, assetPath);

        fa.material.name = name + " Material";
        fa.atlasTexture.name = name + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);

        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(assetPath);

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
    }

    // ------------------------------------------------------------------
    // Prefab'lar
    // ------------------------------------------------------------------

    static Dictionary<string, BKFallingItem> CreateItemPrefabs()
    {
        var result = new Dictionary<string, BKFallingItem>();

        foreach (ItemDef d in ITEM_DEFS)
        {
            Sprite sprite = LoadSprite(ITEMS + "/" + d.file + ".png");
            if (sprite == null) continue;

            string path = PREFABS + "/Items/" + d.name + ".prefab";

            GameObject go = new GameObject(d.name);
            go.tag = "Collectible";
            go.transform.localScale = Vector3.one * d.scale;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 6; // sepetin (5) önünde düşer; yakalanınca animasyon sepetin arkasına alır
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; // panel dışında görünmez

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = sprite.bounds.size * 0.85f;

            BKFallingItem item = go.AddComponent<BKFallingItem>();
            item.itemType = d.type;
            item.points = d.points;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            result[d.name] = prefab.GetComponent<BKFallingItem>();
        }

        return result;
    }

    static BKFloatingText CreateFloatingTextPrefab(Fonts f)
    {
        string path = PREFABS + "/UI/FloatingText.prefab";

        GameObject go = new GameObject("FloatingText");
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.font = f.bold;
        tmp.fontSize = 14f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = DARK;
        tmp.enableWordWrapping = false;
        tmp.text = "+5";
        tmp.rectTransform.sizeDelta = new Vector2(8f, 3f);
        go.AddComponent<BKFloatingText>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<BKFloatingText>();
    }

    // ------------------------------------------------------------------
    // Sahne
    // ------------------------------------------------------------------

    static void BuildScene(Fonts f, Dictionary<string, BKFallingItem> prefabs, BKFloatingText floatingPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Kamera ---
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        // Ortografik; z=-50 ki 3B konfeti parçacıkları (z ±8) yakın düzlemde kırpılmasın
        camGo.transform.position = new Vector3(0f, 0f, -50f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = ORTHO_SIZE;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BK_RED;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<BKCameraFitter>();

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // --- Arka plan ---
        GameObject bgGo = new GameObject("Background");
        SpriteRenderer bgSr = bgGo.AddComponent<SpriteRenderer>();
        bgSr.sprite = LoadSprite(BACKGROUNDS + "/gameplay_background.png");
        bgSr.sortingOrder = -10;

        // --- Oyun alanı ---
        GameObject play = new GameObject("PlayArea");

        GameObject maskGo = new GameObject("PlayAreaMask");
        maskGo.transform.SetParent(play.transform, false);
        maskGo.transform.position = new Vector3((PANEL_LEFT + PANEL_RIGHT) * 0.5f, (PANEL_TOP + PANEL_BOTTOM) * 0.5f, 0f);
        maskGo.transform.localScale = new Vector3(PANEL_RIGHT - PANEL_LEFT, PANEL_TOP - PANEL_BOTTOM, 1f);
        SpriteMask mask = maskGo.AddComponent<SpriteMask>();
        Sprite maskSprite = LoadSprite(UI + "/ui_square.png"); // 100 px @ PPU 100 = 1 birim
        if (maskSprite == null)
        {
            // Maske sprite'ı yoksa VisibleInsideMask ürünlerin hiçbiri çizilmez; sessizce devam etme
            throw new System.InvalidOperationException("BKSceneBuilder: " + UI + "/ui_square.png yüklenemedi; maske olmadan ürünler görünmez.");
        }
        mask.sprite = maskSprite;

        GameObject itemsRoot = new GameObject("Items");
        itemsRoot.transform.SetParent(play.transform, false);

        GameObject floatingRoot = new GameObject("FloatingTexts");
        floatingRoot.transform.SetParent(play.transform, false);

        GameObject spawnerGo = new GameObject("ItemSpawner");
        spawnerGo.transform.SetParent(play.transform, false);
        spawnerGo.transform.position = new Vector3(0f, PANEL_TOP + 2.5f, 0f);
        BKItemSpawner spawner = spawnerGo.AddComponent<BKItemSpawner>();
        spawner.itemsRoot = itemsRoot.transform;
        spawner.spawnHalfWidth = 7.0f; // sepet ±6.7 + yakalama bölgesi yarı genişliği ≈ 1.9 → her ürün yakalanabilir
        foreach (ItemDef d in ITEM_DEFS)
        {
            if (!prefabs.ContainsKey(d.name)) continue;
            spawner.entries.Add(new BKItemSpawner.SpawnEntry
            {
                prefab = prefabs[d.name], weight = d.weight, minProgress = d.minProgress, maxProgress = 1f
            });
        }

        // --- Sepet (BK paketi) ---
        GameObject bagGo = new GameObject("Bag");
        bagGo.tag = "Basket";
        bagGo.transform.SetParent(play.transform, false);
        bagGo.transform.position = new Vector3(0f, BAG_Y, 0f);
        bagGo.transform.localScale = Vector3.one * BAG_SCALE;

        SpriteRenderer bagSr = bagGo.AddComponent<SpriteRenderer>();
        bagSr.sprite = LoadSprite(ITEMS + "/sepet.png");
        bagSr.sortingOrder = 5;

        // Yakalama bölgesi: paketin ağzı (yerel birim; sprite 12 x 12.4 birim, ağız üstte)
        BoxCollider2D catchZone = bagGo.AddComponent<BoxCollider2D>();
        catchZone.isTrigger = false;
        catchZone.size = new Vector2(10.5f, 1.2f);
        catchZone.offset = new Vector2(0f, 5.2f);

        PlayerBasket playerBasket = bagGo.AddComponent<PlayerBasket>();
        playerBasket.playerIndex = 0;

        PhysicalBasketDetector detector = bagGo.AddComponent<PhysicalBasketDetector>();
        detector.playerIndex = 0;
        detector.requireBothElbows = true;

        BasketController2D basket = bagGo.AddComponent<BasketController2D>();
        basket.playerIndex = 0;
        basket.basket2D = bagGo.transform;
        basket.horizontalRange = 6.7f;
        basket.coordinateScale = 10f;
        basket.movementSensitivity = 0.25f;
        basket.smoothingSpeed = 20f;
        basket.basketSprites = new List<Sprite> { bagSr.sprite };

        BKBagFeedback feedback = bagGo.AddComponent<BKBagFeedback>();
        feedback.bagRenderer = bagSr;

        BKKeyboardBasketFallback keyboard = bagGo.AddComponent<BKKeyboardBasketFallback>();
        keyboard.controller = basket;

        // --- DeathZone ---
        GameObject dz = new GameObject("DeathZone");
        dz.tag = "DeathZone";
        dz.transform.SetParent(play.transform, false);
        dz.transform.position = new Vector3(0f, PANEL_BOTTOM - 3.5f, 0f);
        BoxCollider2D dzCol = dz.AddComponent<BoxCollider2D>();
        dzCol.isTrigger = true;
        dzCol.size = new Vector2(40f, 2f);

        // --- Kinect ---
        GameObject kinect = new GameObject("KinectController");
        KinectManager km = kinect.AddComponent<KinectManager>();
        km.sensorHeight = 1.5f;
        km.sensorAngle = 0f;
        km.computeUserMap = KinectManager.UserMapType.RawUserDepth;
        km.computeColorMap = false;
        km.computeInfraredMap = false;
        km.displayUserMap = false;
        km.displayColorMap = false;
        km.displaySkeletonLines = false;
        km.useMultiSourceReader = true;
        km.minUserDistance = 0.5f;
        km.maxUserDistance = 3.5f;
        km.maxTrackedUsers = 1;
        km.showTrackedUsersOnly = true;
        km.userDetectionOrder = KinectManager.UserDetectionOrder.Distance; // sensöre en yakın kişi oynar
        km.ignoreInferredJoints = true;
        km.ignoreZCoordinates = true;
        km.smoothing = KinectManager.Smoothing.Default;
        km.allowedHandRotations = KinectManager.AllowedRotations.Default;

        GameObject elbowL = new GameObject("Player_1_ElbowLeft");
        elbowL.transform.SetParent(kinect.transform, false);
        GameObject elbowR = new GameObject("Player_1_ElbowRight");
        elbowR.transform.SetParent(kinect.transform, false);

        JointOverlayer overlayL = kinect.AddComponent<JointOverlayer>();
        overlayL.foregroundCamera = cam;
        overlayL.playerIndex = 0;
        overlayL.trackedJoint = KinectInterop.JointType.ElbowLeft;
        overlayL.overlayObject = elbowL.transform;
        overlayL.smoothFactor = 10f;

        JointOverlayer overlayR = kinect.AddComponent<JointOverlayer>();
        overlayR.foregroundCamera = cam;
        overlayR.playerIndex = 0;
        overlayR.trackedJoint = KinectInterop.JointType.ElbowRight;
        overlayR.overlayObject = elbowR.transform;
        overlayR.smoothFactor = 10f;

        detector.leftElbowObject = elbowL.transform;
        detector.rightElbowObject = elbowR.transform;

        // --- Ses ---
        GameObject sound = new GameObject("SoundManager");
        MusicManager music = sound.AddComponent<MusicManager>();

        GameObject musicSrc = new GameObject("MusicSource");
        musicSrc.transform.SetParent(sound.transform, false);
        AudioSource musicAudio = musicSrc.AddComponent<AudioSource>();
        musicAudio.playOnAwake = false;
        musicAudio.loop = true;

        GameObject sfxSrc = new GameObject("SfxSource");
        sfxSrc.transform.SetParent(sound.transform, false);
        AudioSource sfxAudio = sfxSrc.AddComponent<AudioSource>();
        sfxAudio.playOnAwake = false;

        music.musicSourceObject = musicSrc;
        music.sfxSourceObject = sfxSrc;
        music.backgroundMusic = Load<AudioClip>("Assets/A Bit off Balance - River Lume.mp3");
        music.endGameSound = Load<AudioClip>("Assets/Confetti pop with sound effect  #2.mp3");
        music.backgroundMusicVolume = 0.05f;
        music.backgroundMusicLoweredVolume = 0.01f;
        music.sfxVolume = 0.05f;

        // --- GameManager ---
        GameObject gmGo = new GameObject("GameManager");
        BKGameManager gm = gmGo.AddComponent<BKGameManager>();
        JsonLeaderboardManager lb = gmGo.AddComponent<JsonLeaderboardManager>();
        lb.fileName = "bk_leaderboard.json";
        AudioSource sfx = gmGo.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.volume = 0.5f; // bk_* sesleri -17..-26 LUFS'e dengelendi; ESC > Efekt sesi ile ayarlanır

        GameObject celebration = new GameObject("CelebrationPoint");
        celebration.transform.position = new Vector3(0f, 2f, 0f);

        // --- UI ---
        BKHudUI hud = BuildHud(cam, f, floatingPrefab, floatingRoot.transform);
        BKStartScreenUI start = BuildStartScreen(cam, f);
        BKEndScreenUI end = BuildEndScreen(cam, f, celebration.transform);
        BKLeaderboardUI board = BuildLeaderboard(cam, f);
        BuildSettings(f, gm, spawner, basket);

        // --- Bağlantılar ---
        gm.spawner = spawner;
        gm.basketController = basket;
        gm.bag = bagGo.transform;
        gm.bagFeedback = feedback;
        gm.hud = hud;
        gm.startScreen = start;
        gm.endScreen = end;
        gm.leaderboardScreen = board;
        gm.leaderboard = lb;
        gm.sfxSource = sfx;

        // Sesler: Assets/BurgerKing/Audio/bk_*.{wav,mp3,ogg}; henüz yoksa projedeki eski seslere düşer
        const string CONFETTI_POP = "Assets/Confetti pop with sound effect  #2.mp3";
        missingAudio.Clear();
        gm.catchSound = LoadSfx("bk_catch", "Assets/CollectSound.mp3");
        gm.bombSound = LoadSfx("bk_bomb");
        gm.menuCompleteSound = LoadSfx("bk_menu_complete");
        gm.countdownTickSound = LoadSfx("bk_countdown_tick");
        gm.countdownGoSound = LoadSfx("bk_countdown_go");
        gm.gameOverSound = LoadSfx("bk_time_up");
        gm.highScoreSound = LoadSfx("bk_high_score", CONFETTI_POP);
        gm.timeWarningSound = LoadSfx("bk_time_warning");
        gm.buttonSound = LoadSfx("bk_button");
        end.countSound = LoadSfx("bk_score_count");
        if (missingAudio.Count > 0)
        {
            Debug.Log("BKSceneBuilder: " + AUDIO + " içinde bulunamayan sesler (boş ya da eski sesle bırakıldı): " + string.Join(", ", missingAudio));
        }

        gm.catchEffectPrefab = Load<GameObject>("Assets/Epic Toon FX/Prefabs 2D/Explosions/StarBurst2D.prefab");
        gm.catchEffectScale = 2f;
        gm.bombEffectPrefab = Load<GameObject>("Assets/Epic Toon FX/Prefabs 2D/Explosions/SparkExplosion2D.prefab");
        gm.bombEffectScale = 3f;
        gm.bagSortingOrder = bagSr.sortingOrder;

        // --- Kaydet ---
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AddSceneToBuildSettings(SCENE_PATH);
        Debug.Log("Burger King sahnesi oluşturuldu: " + SCENE_PATH);
    }

    static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int index = scenes.FindIndex(s => s.path == scenePath);
        if (index < 0)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            index = scenes.Count - 1;
        }

        if (index != 0)
        {
            Debug.LogWarning("Burger King sahnesi Build Settings'te " + index + ". sırada; kiosk build'i bu sahneyle açılsın istiyorsan " +
                             "menüden 'DropCatch > Burger King > Build'de İlk Sahne Yap' çalıştır.");
        }
    }

    [MenuItem("DropCatch/Burger King/Build'de İlk Sahne Yap", false, 30)]
    public static void MakeFirstBuildScene()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == SCENE_PATH);
        scenes.Insert(0, new EditorBuildSettingsScene(SCENE_PATH, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("Burger King sahnesi Build Settings'te ilk sıraya alındı (diğer sahneler korunuyor).");
    }

    // ------------------------------------------------------------------
    // HUD
    // ------------------------------------------------------------------

    static BKHudUI BuildHud(Camera cam, Fonts f, BKFloatingText floatingPrefab, Transform floatingParent)
    {
        Canvas canvas = MakeCanvas("HUDCanvas", cam, 10, false);
        RectTransform root = DesignRoot("HUD", canvas.transform);
        BKHudUI hud = canvas.gameObject.AddComponent<BKHudUI>();
        hud.root = root.gameObject;

        // Üst satır: krem panel canvas'ta x 88..992, üst kenar y = 224 (köşe yarıçapı için içeriden başla)
        hud.scoreText = Txt(At("ScoreText", root, -235f, 262f, 330f, 84f), "SKOR  0", f.sans, 54f, DARK, TextAlignmentOptions.Left);
        Img(At("MenuIcon", root, -60f, 258f, 84f, 84f), LoadSprite(ITEMS + "/tamamlanan_menu.png"), Color.white, Image.Type.Simple, true);
        hud.menuCountText = Txt(At("MenuCountText", root, 68f, 262f, 120f, 84f), "×0", f.sans, 54f, DARK, TextAlignmentOptions.Left);
        hud.timeText = Txt(At("TimeText", root, 235f, 262f, 330f, 84f), "SÜRE  60", f.sans, 54f, DARK, TextAlignmentOptions.Right);

        hud.comboText = Txt(At("ComboText", root, 0f, 1180f, 500f, 60f), "", f.bold, 40f, BK_RED, TextAlignmentOptions.Center);
        hud.comboText.gameObject.SetActive(false);

        // Menü takip satırı (tüm normal ürünler; gerekli listede olmayanlar çalışma zamanında gizlenir)
        BKItemType[] types =
        {
            BKItemType.Ekmek, BKItemType.Kofte, BKItemType.Marul, BKItemType.Domates, BKItemType.Sogan,
            BKItemType.Tursu, BKItemType.Patates, BKItemType.Bardak, BKItemType.SoganHalkasi
        };
        string[] files =
        {
            "hamburger_ekmegi", "kofte", "marul", "domates", "sogan", "tursu", "patates", "bardak", "sogan_halkasi"
        };

        // Yatay layout: gizlenen slotlar (gerekli listede olmayan ürünler) satırı ortalı tutar
        RectTransform tracker = At("MenuTracker", root, 0f, 352f, 920f, 84f);
        HorizontalLayoutGroup trackerLayout = tracker.gameObject.AddComponent<HorizontalLayoutGroup>();
        trackerLayout.spacing = 16f;
        trackerLayout.childAlignment = TextAnchor.MiddleCenter;
        trackerLayout.childControlWidth = true;
        trackerLayout.childControlHeight = true;
        trackerLayout.childForceExpandWidth = false;
        trackerLayout.childForceExpandHeight = false;

        hud.trackerSlots = new Image[types.Length];
        hud.trackerIcons = new Image[types.Length];
        hud.trackerTypes = types;

        Sprite circle = LoadSprite(UI + "/ui_circle.png");
        for (int i = 0; i < types.Length; i++)
        {
            RectTransform slot = Rect("Slot_" + types[i], tracker);
            LayoutElement slotLayout = slot.gameObject.AddComponent<LayoutElement>();
            slotLayout.preferredWidth = 82f;
            slotLayout.preferredHeight = 82f;
            hud.trackerSlots[i] = Img(slot, circle, CREAM_DARK, Image.Type.Simple, false);
            RectTransform icon = At("Icon", slot, 0f, 9f, 64f, 64f);
            hud.trackerIcons[i] = Img(icon, LoadSprite(ITEMS + "/" + files[i] + ".png"), new Color(1f, 1f, 1f, 0.28f), Image.Type.Simple, true);
        }

        // Geri sayım ("BAŞLA!" 280 px'te panelden taşar → otomatik küçültme)
        RectTransform countdown = AtCentered("Countdown", root, 0f, 720f, 880f, 420f);
        hud.countdownRoot = countdown.gameObject;
        hud.countdownText = Txt(Full("Text", countdown), "3", f.bold, 280f, DARK, TextAlignmentOptions.Center, true);
        countdown.gameObject.SetActive(false);

        // Oyuncu bekleniyor
        RectTransform waiting = At("Waiting", root, 0f, 800f, 880f, 250f);
        hud.waitingRoot = waiting.gameObject;
        Pill(waiting, BK_RED, new Vector2(8f, -12f));
        hud.waitingText = Txt(Full("Text", waiting), "OYUNCU BEKLENİYOR\n<size=55%>Kinect'in önüne geç</size>", f.sans, 60f, CREAM, TextAlignmentOptions.Center);
        waiting.gameObject.SetActive(false);

        // +1 MENÜ animasyonu (ölçek punch'ı merkezden büyüsün diye pivot ortada)
        RectTransform popup = AtCentered("MenuPopup", root, 0f, 600f, 900f, 560f);
        hud.menuPopup = popup;
        hud.menuPopupGroup = popup.gameObject.AddComponent<CanvasGroup>();
        hud.menuPopupGroup.alpha = 0f;
        hud.menuPopupGroup.blocksRaycasts = false;
        hud.menuPopupGroup.interactable = false;
        Img(At("Icon", popup, 0f, 0f, 380f, 380f), LoadSprite(ITEMS + "/tamamlanan_menu.png"), Color.white, Image.Type.Simple, true);
        hud.menuPopupText = Txt(At("Text", popup, 0f, 400f, 900f, 140f), "+1 MENÜ!", f.bold, 120f, BK_RED, TextAlignmentOptions.Center);

        // Bomba flaşı (tam ekran kırmızı, alpha 0)
        RectTransform flash = Full("BombFlash", root);
        Img(flash, null, new Color(0.82f, 0.15f, 0.12f, 1f), Image.Type.Simple, false);
        hud.bombFlash = flash.gameObject.AddComponent<CanvasGroup>();
        hud.bombFlash.alpha = 0f;
        hud.bombFlash.blocksRaycasts = false;
        hud.bombFlash.interactable = false;

        hud.floatingTextPrefab = floatingPrefab;
        hud.floatingTextParent = floatingParent;

        root.gameObject.SetActive(false);
        return hud;
    }

    // ------------------------------------------------------------------
    // Başlangıç ekranı
    // ------------------------------------------------------------------

    static BKStartScreenUI BuildStartScreen(Camera cam, Fonts f)
    {
        Canvas canvas = MakeCanvas("StartCanvas", cam, 30, false);
        RectTransform root = DesignRoot("StartScreen", canvas.transform);
        Img(Full("Background", root), LoadSprite(BACKGROUNDS + "/start_background.png"), Color.white, Image.Type.Simple, false);

        // BAŞLA: tasarımda y 867..1049, genişlik 776 (1080x1920 canvas)
        Button btn = PillButton(root, "StartButton", 0f, 868f, 776f, 182f, "BAŞLA", f.sans, 92f, CREAM, DARK, new Vector2(10f, -16f));
        TextMeshProUGUI hint = Txt(At("Hint", root, 0f, 1080f, 800f, 50f), "veya SPACE tuşuna bas", f.sans, 28f, new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.Center);

        BKStartScreenUI ui = canvas.gameObject.AddComponent<BKStartScreenUI>();
        ui.root = root.gameObject;
        ui.startButton = btn;
        ui.hintText = hint;
        return ui;
    }

    // ------------------------------------------------------------------
    // Bitiş ekranı
    // ------------------------------------------------------------------

    static BKEndScreenUI BuildEndScreen(Camera cam, Fonts f, Transform celebrationPoint)
    {
        Canvas canvas = MakeCanvas("EndCanvas", cam, 40, false);
        RectTransform root = DesignRoot("EndScreen", canvas.transform);
        Img(Full("Background", root), LoadSprite(BACKGROUNDS + "/finish_background.png"), Color.white, Image.Type.Simple, false);

        BKEndScreenUI ui = canvas.gameObject.AddComponent<BKEndScreenUI>();
        ui.root = root.gameObject;

        // Konumlar bitis_ornek_sayfa.png'deki öğelerin finish_background paneline oranlanmasıyla ölçüldü.
        // Büyük harfli yazılar Capline hizalı: kutunun ortası = büyük harflerin ortası.

        // SKOR (arka plandaki boş pill'in içine)
        Txt(AtCentered("ScoreLabel", root, 0f, END_PILL_TOP, END_PILL_W, END_PILL_H), "SKOR", f.sans, 87f, DARK, TextAlignmentOptions.Capline);

        // Yüksek skor satırı: alevli köfte + "YÜKSEK SKOR" / değer
        Img(At("HighScoreIcon", root, -183f, 537f, 328f, 322f), LoadSprite(ITEMS + "/kofte_2.png"), Color.white, Image.Type.Simple, true);
        Txt(AtCentered("HighScoreLabel", root, 194f, 667f, 420f, 70f), "YÜKSEK SKOR", f.sans, 51f, DARK, TextAlignmentOptions.Capline);
        ui.highScoreText = Txt(AtCentered("HighScoreValue", root, 194f, 725f, 420f, 70f), "0", f.sans, 51f, DARK, TextAlignmentOptions.Capline);

        // Tasarım dışı eklemeler, boşluklara yerleşik: YENİ REKOR (köfte ile puan arası), menü bonusu (puan ile taç arası)
        ui.newRecordText = Txt(AtCentered("NewRecordText", root, 0f, 889f, 860f, 80f), "YENİ REKOR!", f.bold, 56f, BK_RED, TextAlignmentOptions.Capline);
        ui.newRecordText.gameObject.SetActive(false);

        // Oyuncu puanı ("245 PUAN": büyük harf yüksekliği 81 px, merkez y 1040)
        ui.finalScoreText = Txt(AtCentered("FinalScoreText", root, 0f, 970f, 860f, 140f), "0 PUAN", f.sans, 118f, DARK, TextAlignmentOptions.Capline, true);
        ui.breakdownText = Txt(AtCentered("BreakdownText", root, 0f, 1111f, 860f, 50f), "", f.sans, 38f, BK_RED, TextAlignmentOptions.Capline, true);

        // Taç + burger tutan eller: panelin alt kenarına oturur, panelden yanlara ~15 px taşar (tasarımdaki gibi).
        // Pivot altta: açılış animasyonu panelin dibinden büyür.
        RectTransform hero = At("HeroImage", root, 0f, 1194f, 933f, END_PANEL_BOTTOM - 1194f);
        hero.pivot = new Vector2(0.5f, 0f);
        hero.anchoredPosition = new Vector2(0f, -END_PANEL_BOTTOM);
        ui.heroImage = Img(hero, LoadSprite(UI + "/burger_tutan_eller.png"), Color.white, Image.Type.Simple, true);

        // İsim girişi: taç görseli solarken aynı alanın ortasında açılan kart (alan y 1194..1830, merkez 1512)
        RectTransform entry = AtCentered("NameEntry", root, 0f, 1347f, 880f, 330f);
        ui.nameEntryRoot = entry.gameObject;
        // Krem panel üstünde okunsun diye kırmızı kart; giriş alanı ve buton krem (BAŞLA pill'inin tersi)
        Pill(entry, BK_RED, new Vector2(20f, -16f));
        ui.nameEntryHint = Txt(At("Hint", entry, 0f, 40f, 820f, 60f), "Liderlik tablosuna girdin! İsmini yaz:", f.sans, 40f, CREAM, TextAlignmentOptions.Center, true);
        ui.nameInput = MakeInputField(entry, -130f, 140f, 540f, 120f, f.sans, "İsim...");
        ui.nameInput.GetComponent<Image>().color = CREAM;
        ui.saveButton = PillButton(entry, "SaveButton", 280f, 140f, 240f, 120f, "KAYDET", f.sans, 48f, CREAM, DARK, new Vector2(8f, -10f));
        entry.gameObject.SetActive(false);

        // Puan sayarken döngüde çalan ses
        AudioSource countSource = canvas.gameObject.AddComponent<AudioSource>();
        countSource.playOnAwake = false;
        countSource.loop = true;
        countSource.volume = 0.5f; // çalarken GameManager efekt sesi seviyesini alır
        ui.countSource = countSource;

        // Kutlama
        ui.confettiPrefabs = new[]
        {
            Load<GameObject>("Assets/Epic Toon FX/Prefabs/Environment/Confetti/Blast/ConfettiBlastRed.prefab"),
            Load<GameObject>("Assets/Epic Toon FX/Prefabs/Environment/Confetti/Blast/ConfettiBlastOrangePurple.prefab"),
            Load<GameObject>("Assets/Epic Toon FX/Prefabs/Environment/Confetti/Directional/ConfettiDirectionalRed.prefab"),
        };
        ui.confettiSpawnPoint = celebrationPoint;
        ui.confettiSortingOrder = 60;
        ui.confettiScale = 3f; // parçacık hızı ölçekle büyür; 3 → ~6-30 birim/sn, 1-2 sn ekranda kalır

        root.gameObject.SetActive(false);
        return ui;
    }

    // ------------------------------------------------------------------
    // Liderlik tablosu
    // ------------------------------------------------------------------

    static BKLeaderboardUI BuildLeaderboard(Camera cam, Fonts f)
    {
        Canvas canvas = MakeCanvas("LeaderboardCanvas", cam, 45, false);
        RectTransform root = DesignRoot("LeaderboardScreen", canvas.transform);
        Img(Full("Background", root), LoadSprite(BACKGROUNDS + "/finish_background.png"), Color.white, Image.Type.Simple, false);

        // Başlık, bitiş ekranındaki "SKOR" ile aynı pill ve tipografi ("LİDERLİK TABLOSU" 87 px'te pill'e sığmaz)
        Txt(AtCentered("TitleLabel", root, 0f, END_PILL_TOP, END_PILL_W, END_PILL_H), "SIRALAMA", f.sans, 87f, DARK, TextAlignmentOptions.Capline);

        BKLeaderboardUI ui = canvas.gameObject.AddComponent<BKLeaderboardUI>();
        ui.root = root.gameObject;
        ui.rows = new TextMeshProUGUI[10];
        ui.scoreTexts = new TextMeshProUGUI[10];
        ui.rowBackgrounds = new Image[10];
        ui.highlightBackground = CREAM_DARK;

        // 10 satır krem panelin içinde eşit aralıklı (panel y 510..1830)
        const float rowHeight = 100f;
        float pitch = 118f;
        float firstTop = END_PANEL_TOP + ((END_PANEL_BOTTOM - END_PANEL_TOP) - (pitch * 9f + rowHeight)) * 0.5f;
        Sprite rounded = LoadSprite(UI + "/ui_rounded_rect.png");

        for (int i = 0; i < 10; i++)
        {
            RectTransform row = At("Row" + (i + 1), root, 0f, firstTop + i * pitch, 840f, rowHeight);
            ui.rowBackgrounds[i] = Img(row, rounded, new Color(CREAM_DARK.r, CREAM_DARK.g, CREAM_DARK.b, 0f), Image.Type.Sliced, false);

            RectTransform nameRect = Full("Name", row);
            nameRect.offsetMin = new Vector2(36f, 0f);
            nameRect.offsetMax = new Vector2(-220f, 0f);
            ui.rows[i] = Txt(nameRect, (i + 1) + ".  ---", f.sans, 50f, DARK, TextAlignmentOptions.Left);
            ui.rows[i].overflowMode = TextOverflowModes.Ellipsis; // 16 karakterlik uzun isimler puana taşmasın

            RectTransform scoreRect = Full("Score", row);
            scoreRect.offsetMin = new Vector2(620f, 0f);
            scoreRect.offsetMax = new Vector2(-36f, 0f);
            ui.scoreTexts[i] = Txt(scoreRect, "-", f.sans, 50f, DARK, TextAlignmentOptions.Right);
        }

        root.gameObject.SetActive(false);
        return ui;
    }

    // ------------------------------------------------------------------
    // Ayarlar (ESC)
    // ------------------------------------------------------------------

    static void BuildSettings(Fonts f, BKGameManager gm, BKItemSpawner spawner, BasketController2D basket)
    {
        Canvas canvas = MakeCanvas("SettingsCanvas", null, 100, true);
        BKSettingsUI ui = canvas.gameObject.AddComponent<BKSettingsUI>();
        ui.gameManager = gm;
        ui.spawner = spawner;
        ui.basketController = basket;
        ui.canvas = canvas;
        ui.font = f.sans;
        ui.panelSprite = LoadSprite(UI + "/ui_rounded_rect.png");
        ui.sliderSprite = LoadSprite(UI + "/ui_pill.png");
        ui.knobSprite = LoadSprite(UI + "/ui_circle.png");
    }

    // ------------------------------------------------------------------
    // UI yardımcıları
    // ------------------------------------------------------------------

    static Canvas MakeCanvas(string name, Camera cam, int sortingOrder, bool overlay)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = overlay ? RenderMode.ScreenSpaceOverlay : RenderMode.ScreenSpaceCamera;
        c.worldCamera = overlay ? null : cam;
        c.planeDistance = 50f;
        c.sortingOrder = sortingOrder;

        CanvasScaler s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1080f, 1920f);
        s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        s.matchWidthOrHeight = 1f; // çalışma zamanında BKCameraFitter ekran oranına göre günceller
        s.referencePixelsPerUnit = 100f;
        return c;
    }

    static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    /// <summary>
    /// Canvas ortasına sabitlenmiş 1080x1920 tasarım alanı. Dünya (kamera contain) ile aynı şekilde
    /// letterbox'lanır; tüm ekran elemanları bunun altına yerleştirilir.
    /// </summary>
    static RectTransform DesignRoot(string name, Transform parent)
    {
        RectTransform r = Rect(name, parent);
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = new Vector2(1080f, 1920f);
        return r;
    }

    /// <summary>At() ile aynı yerleşim, ancak pivot ortada (ölçek animasyonları merkezden büyür).</summary>
    static RectTransform AtCentered(string name, Transform parent, float xCenter, float yTop, float w, float h)
    {
        RectTransform r = Rect(name, parent);
        r.anchorMin = new Vector2(0.5f, 1f);
        r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2(xCenter, -(yTop + h * 0.5f));
        r.sizeDelta = new Vector2(w, h);
        return r;
    }

    /// <summary>Ebeveyni tamamen kaplayan RectTransform.</summary>
    static RectTransform Full(string name, Transform parent)
    {
        RectTransform r = Rect(name, parent);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        return r;
    }

    /// <summary>Üst-orta çapa: xCenter = yatay ofset, yTop = ebeveyn üst kenarından mesafe (canvas px).</summary>
    static RectTransform At(string name, Transform parent, float xCenter, float yTop, float w, float h)
    {
        RectTransform r = Rect(name, parent);
        r.anchorMin = new Vector2(0.5f, 1f);
        r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(xCenter, -yTop);
        r.sizeDelta = new Vector2(w, h);
        return r;
    }

    static Image Img(RectTransform r, Sprite sprite, Color color, Image.Type type, bool preserveAspect)
    {
        Image i = r.gameObject.AddComponent<Image>();
        i.sprite = sprite;
        i.color = color;
        i.type = type;
        i.preserveAspect = preserveAspect;
        i.raycastTarget = false;
        return i;
    }

    static TextMeshProUGUI Txt(RectTransform r, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align, bool autoSize = false)
    {
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        if (autoSize)
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = size * 0.4f;
            t.fontSizeMax = size;
        }
        return t;
    }

    /// <summary>Tasarımdaki krem "pill": siyah ofset gölge + yuvarlatılmış gövde. Gövde Image'ini döndürür.</summary>
    static Image Pill(RectTransform r, Color body, Vector2 shadowOffset)
    {
        Sprite rounded = LoadSprite(UI + "/ui_rounded_rect.png");

        RectTransform shadow = Full("Shadow", r);
        shadow.anchoredPosition = shadowOffset;
        Img(shadow, rounded, SHADOW, Image.Type.Sliced, false);

        RectTransform bodyRect = Full("Body", r);
        return Img(bodyRect, rounded, body, Image.Type.Sliced, false);
    }

    static Button PillButton(Transform parent, string name, float xCenter, float yTop, float w, float h, string label,
        TMP_FontAsset font, float fontSize, Color bodyColor, Color textColor, Vector2 shadowOffset)
    {
        RectTransform root = At(name, parent, xCenter, yTop, w, h);
        Image body = Pill(root, bodyColor, shadowOffset);
        body.raycastTarget = true;
        Txt(Full("Label", (RectTransform)body.transform), label, font, fontSize, textColor, TextAlignmentOptions.Center);

        Button btn = body.gameObject.AddComponent<Button>();
        btn.targetGraphic = body;
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;
        return btn;
    }

    static TMP_InputField MakeInputField(RectTransform parent, float xCenter, float yTop, float w, float h, TMP_FontAsset font, string placeholderText)
    {
        TMP_DefaultControls.Resources res = new TMP_DefaultControls.Resources
        {
            inputField = LoadSprite(UI + "/ui_rounded_rect.png")
        };
        GameObject go = TMP_DefaultControls.CreateInputField(res);
        go.name = "NameInput";
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(xCenter, -yTop);
        rt.sizeDelta = new Vector2(w, h);

        Image img = go.GetComponent<Image>();
        img.color = CREAM_DARK;
        img.type = Image.Type.Sliced;

        TMP_InputField field = go.GetComponent<TMP_InputField>();
        field.characterLimit = 16;
        field.fontAsset = font;
        field.pointSize = 48f;

        TextMeshProUGUI text = field.textComponent as TextMeshProUGUI;
        if (text)
        {
            text.font = font;
            text.fontSize = 48f;
            text.color = DARK;
            text.alignment = TextAlignmentOptions.Left;
        }

        TextMeshProUGUI placeholder = field.placeholder as TextMeshProUGUI;
        if (placeholder)
        {
            placeholder.font = font;
            placeholder.fontSize = 48f;
            placeholder.fontStyle = FontStyles.Normal;
            placeholder.text = placeholderText;
            placeholder.color = new Color(DARK.r, DARK.g, DARK.b, 0.4f);
            placeholder.alignment = TextAlignmentOptions.Left;
        }

        if (field.textViewport)
        {
            field.textViewport.offsetMin = new Vector2(28f, 8f);
            field.textViewport.offsetMax = new Vector2(-28f, -8f);
        }

        return field;
    }
}
