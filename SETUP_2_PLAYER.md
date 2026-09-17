# 2 Oyunculu Mod - Unity Scene Setup Talimatları

Bu doküman, 2 oyunculu modu Unity scene'de nasıl yapılandıracağınızı adım adım açıklamaktadır.

## 1. BASKET SETUP (Her İki Oyuncu İçin)

### Player 1 Basket (Sol Taraf)
1. **Basket GameObject'i seçin** (zaten mevcut)
2. **Component'leri ekleyin/kontrol edin:**
   - `PlayerBasket` component
     - `playerIndex = 0`
   - `BasketController2D` component
     - `playerIndex = 0`
     - `basket2D` referansını kendisine ata
     - `horizontalRange = 8`
   - `PhysicalBasketDetector` component
     - `playerIndex = 0`
     - `leftElbowObject` → LeftElbow_P0 (aşağıda oluşturacağız)
     - `rightElbowObject` → RightElbow_P0 (aşağıda oluşturacağız)
     - `requireBothElbows = true`
   - `BoxCollider2D` veya `CircleCollider2D`
     - `Is Trigger = true`
   - `SpriteRenderer` (zaten var)
   - **Tag = "Basket"** (önemli!)

### Player 2 Basket (Sağ Taraf)
1. **Basket2 GameObject'i seçin** (zaten kopyaladınız)
2. **Component'leri ekleyin:**
   - `PlayerBasket` component ekleyin
     - `playerIndex = 1`
   - `BasketController2D` component ekleyin
     - `playerIndex = 1`
     - `basket2D` referansını kendisine ata
     - `horizontalRange = 8`
   - `PhysicalBasketDetector` component ekleyin
     - `playerIndex = 1`
     - `leftElbowObject` → LeftElbow_P1 (aşağıda oluşturacağız)
     - `rightElbowObject` → RightElbow_P1 (aşağıda oluşturacağız)
     - `requireBothElbows = true`
   - `BoxCollider2D` veya `CircleCollider2D` ekleyin
     - `Is Trigger = true`
   - `SpriteRenderer` (zaten var)
   - **Tag = "Basket"** (önemli!)

---

## 2. ELBOW TRACKING OBJECTS (Her İki Oyuncu İçin)

Her oyuncu için 2 elbow tracking GameObject'i oluşturmanız gerekiyor.

### Player 1 Elbow Objects

#### LeftElbow_P0
1. **Yeni Empty GameObject oluşturun**
2. **İsim:** `LeftElbow_P0`
3. **JointOverlayer component ekleyin** (K2Examples klasöründen)
   - `kinectManager` → KinectManager'ı atayın
   - `playerIndex = 0`
   - `jointType = ElbowLeft` (veya Kinect2.JointType.ElbowLeft)
   - `smoothFactor = 5` (opsiyonel, smoothing için)

#### RightElbow_P0
1. **Yeni Empty GameObject oluşturun**
2. **İsim:** `RightElbow_P0`
3. **JointOverlayer component ekleyin**
   - `kinectManager` → KinectManager'ı atayın
   - `playerIndex = 0`
   - `jointType = ElbowRight` (veya Kinect2.JointType.ElbowRight)
   - `smoothFactor = 5`

### Player 2 Elbow Objects

#### LeftElbow_P1
1. **Yeni Empty GameObject oluşturun**
2. **İsim:** `LeftElbow_P1`
3. **JointOverlayer component ekleyin**
   - `kinectManager` → KinectManager'ı atayın
   - `playerIndex = 1`
   - `jointType = ElbowLeft`
   - `smoothFactor = 5`

#### RightElbow_P1
1. **Yeni Empty GameObject oluşturun**
2. **İsim:** `RightElbow_P1`
3. **JointOverlayer component ekleyin**
   - `kinectManager` → KinectManager'ı atayın
   - `playerIndex = 1`
   - `jointType = ElbowRight`
   - `smoothFactor = 5`

**NOT:** Elbow object'lerinin isimleri önemli! PhysicalBasketDetector bu isimlere göre otomatik arama yapabilir. Manuel atama yapmak daha garantilidir.

---

## 3. KINECT MANAGER AYARLARI

1. **KinectManager GameObject'ini seçin**
2. **KinectManager component'te:**
   - `maxTrackedUsers = 2` ✅ (zaten ayarlamışsınız)
   - `displayMapsWidthPercent = 100` (opsiyonel, görselleştirme için)
   - Diğer ayarları varsayılan değerlerde bırakabilirsiniz

---

## 4. OBJECT SPAWNERS (2 Adet)

İki ayrı ObjectSpawner2D oluşturacaksınız - biri sol taraf, biri sağ taraf için.

### Sol Spawner (Player 1 için)
1. **Mevcut ObjectSpawner GameObject'ini seçin**
2. **İsim:** `ObjectSpawner_Left`
3. **ObjectSpawner2D component ayarları:**
   - `spawnRangeX = 4` (sadece sol yarıda spawn olması için)
   - `spawnHeight = 6` (ekranın üst kısmı)
   - **Transform pozisyonu:** `X = -4, Y = 6, Z = 0` (sol üst köşe)
   - `initialSpawnRate = 2`
   - `minSpawnRate = 0.5`
   - Prefab'ları atayın

### Sağ Spawner (Player 2 için)
1. **Yeni ObjectSpawner GameObject oluşturun** (veya sol spawner'ı kopyalayın)
2. **İsim:** `ObjectSpawner_Right`
3. **ObjectSpawner2D component ayarları:**
   - `spawnRangeX = 4` (sadece sağ yarıda spawn olması için)
   - `spawnHeight = 6`
   - **Transform pozisyonu:** `X = 4, Y = 6, Z = 0` (sağ üst köşe)
   - `initialSpawnRate = 2`
   - `minSpawnRate = 0.5`
   - Prefab'ları atayın

**ÖNEMLI:** ObjectSpawner2D script'inde spawnRangeX'i kullanırken, spawn pozisyonunu GameObject'in kendi pozisyonuna göre hesaplatın:
```csharp
float randomX = transform.position.x + Random.Range(-spawnRangeX, spawnRangeX);
```

Eğer script bunu yapmıyorsa, her spawner için farklı spawn range mantığı eklemeniz gerekebilir.

---

## 5. UI SETUP

GameManager2D'nin UI referanslarını ayarlamanız gerekiyor.

### Gameplay UI

#### Player 1 Score/Combo (Sol Üst)
1. **Canvas → Yeni TextMeshPro Text oluşturun**
2. **İsim:** `ScoreText_P0`
3. **Rect Transform:**
   - Anchor: Top-Left
   - Pos X: 100, Pos Y: -50
4. **TextMeshPro ayarları:**
   - Font Size: 36
   - Alignment: Left
   - Text: "P1: 0"
5. **Yeni TextMeshPro Text oluşturun** (Combo)
6. **İsim:** `ComboText_P0`
7. **Rect Transform:**
   - Anchor: Top-Left
   - Pos X: 100, Pos Y: -100
8. **TextMeshPro ayarları:**
   - Font Size: 28
   - Alignment: Left
   - Text: "x1!"
   - **GameObject başlangıçta inactive olmalı**

#### Player 2 Score/Combo (Sağ Üst)
1. **Canvas → Yeni TextMeshPro Text oluşturun**
2. **İsim:** `ScoreText_P1`
3. **Rect Transform:**
   - Anchor: Top-Right
   - Pos X: -100, Pos Y: -50
4. **TextMeshPro ayarları:**
   - Font Size: 36
   - Alignment: Right
   - Text: "P2: 0"
5. **Yeni TextMeshPro Text oluşturun** (Combo)
6. **İsim:** `ComboText_P1`
7. **Rect Transform:**
   - Anchor: Top-Right
   - Pos X: -100, Pos Y: -100
8. **TextMeshPro ayarları:**
   - Font Size: 28
   - Alignment: Right
   - Text: "x1!"
   - **GameObject başlangıçta inactive olmalı**

#### Timer (Orta Üst)
1. **Canvas → Yeni TextMeshPro Text oluşturun** (veya mevcut timer'ı kullanın)
2. **İsim:** `TimerText`
3. **Rect Transform:**
   - Anchor: Top-Center
   - Pos X: 0, Pos Y: -50
4. **TextMeshPro ayarları:**
   - Font Size: 48
   - Alignment: Center
   - Text: "Süre: 60"

### End Game Panel (Bitiş Ekranı)

1. **Canvas → Yeni Panel oluşturun**
2. **İsim:** `EndGamePanel`
3. **Rect Transform:** Full screen (anchor: stretch)
4. **Panel başlangıçta inactive olmalı**

#### Player 1 Final Score (Sol Orta)
1. **EndGamePanel içinde yeni TextMeshPro Text oluşturun**
2. **İsim:** `EndGameScore_P0`
3. **Rect Transform:**
   - Anchor: Middle-Left
   - Pos X: 250, Pos Y: 0
   - Width: 400, Height: 200
4. **TextMeshPro ayarları:**
   - Font Size: 72
   - Alignment: Center
   - Text: "Player 1\n0"

#### Player 2 Final Score (Sağ Orta)
1. **EndGamePanel içinde yeni TextMeshPro Text oluşturun**
2. **İsim:** `EndGameScore_P1`
3. **Rect Transform:**
   - Anchor: Middle-Right
   - Pos X: -250, Pos Y: 0
   - Width: 400, Height: 200
4. **TextMeshPro ayarları:**
   - Font Size: 72
   - Alignment: Center
   - Text: "Player 2\n0"

---

## 6. GAMEMANAGER2D REFERANSLARI

1. **GameManager GameObject'ini seçin**
2. **GameManager2D component'te UI referanslarını atayın:**

**UI Elemanları - Player 1 (Sol):**
- `scoreText_P0` → ScoreText_P0
- `comboText_P0` → ComboText_P0

**UI Elemanları - Player 2 (Sağ):**
- `scoreText_P1` → ScoreText_P1
- `comboText_P1` → ComboText_P1

**UI Elemanları - Paylaşılan:**
- `timerText` → TimerText

**Bitiş Paneli:**
- `endGamePanel` → EndGamePanel
- `endGameScore_P0` → EndGameScore_P0
- `endGameScore_P1` → EndGameScore_P1

**Oyun Ayarları:**
- `gameDuration = 60` (veya istediğiniz süre)
- `basePoints = 10`
- `comboResetTime = 2`

---

## 7. FALLING OBJECTS

Düşen object prefab'larınızın **"Basket" tag'ine sahip collider'larla trigger olacak şekilde** ayarlandığından emin olun:

1. **FallingObject prefab'ını açın**
2. **Rigidbody2D component kontrol edin:**
   - `Body Type = Dynamic`
   - `Gravity Scale = 0` (kendi fallSpeed'i kullanıyor)
3. **Collider2D component:**
   - `Is Trigger = true`
4. **FallingObject2D script:**
   - `pointValue = 10` (veya istediğiniz değer)
   - `fallSpeed = 5`

---

## 8. TEST ETME

### Adım 1: Kinect Bağlantısını Test Edin
1. Play tuşuna basın
2. Console'da "Player X: ... dirsek objesi otomatik bulundu" mesajlarını görmelisiniz
3. Eğer "bulunamadı" uyarısı görüyorsanız, elbow object referanslarını manuel olarak atayın

### Adım 2: 2 Kullanıcı Algılamayı Test Edin
1. 2 kişi Kinect önünde durun
2. Console'da "Bekleniyor... Player 1: Algılandı, Player 2: Algılandı" görmeli
3. Space tuşuna basın
4. "Oyun başladı!" mesajını görmelisiniz

### Adım 3: Gameplay Test
1. Her iki oyuncu da ellerini tutup sepeti kontrol edin
2. Düşen objeleri yakalayın
3. Her oyuncunun ayrı skorunu ve combo'sunu görmelisiniz
4. Süre bitince bitiş panelini görmelisiniz

### Adım 4: Yeniden Başlatma Test
1. Bitiş panelinde Space tuşuna basın
2. Ana gameplay'e dönmeli
3. Tekrar Space tuşuna basarak oyunu yeniden başlatabilmelisiniz

---

## SORUN GİDERME

### Problem: Elbow object'leri bulunamıyor
**Çözüm:** PhysicalBasketDetector component'te leftElbowObject ve rightElbowObject referanslarını manuel olarak atayın.

### Problem: Basket yakaladığında puan eklenmiyor
**Çözüm:**
1. Basket GameObject'in "Basket" tag'ine sahip olduğundan emin olun
2. PlayerBasket component'inin eklendiğinden emin olun
3. Collider'ın "Is Trigger = true" olduğunu kontrol edin

### Problem: Her iki oyuncu da algılanmıyor
**Çözüm:**
1. KinectManager'da maxTrackedUsers = 2 olduğunu kontrol edin
2. Her iki oyuncunun da Kinect'in görüş alanında olduğundan emin olun
3. JointOverlayer component'lerinde playerIndex'lerin doğru ayarlandığını kontrol edin

### Problem: Objeler sadece bir tarafa düşüyor
**Çözüm:**
1. İki ayrı ObjectSpawner olduğunu kontrol edin
2. Her spawner'ın Transform pozisyonunun farklı olduğunu kontrol edin
3. Spawner'ların enabled = false durumunda başladığını kontrol edin

### Problem: UI görünmüyor
**Çözüm:**
1. Canvas'ın Render Mode'unun "Screen Space - Overlay" olduğunu kontrol edin
2. GameManager2D'de tüm UI referanslarının atanmış olduğunu kontrol edin
3. ComboText'lerin başlangıçta inactive olması gerektiğini unutmayın

---

## BAŞARILAR!

Artık 2 oyunculu modunuz hazır! Oyuncularınızla birlikte test edin ve gerektiğinde ayarları (spawn rate, game duration, combo reset time) değiştirin.

**Oyun Akışı Özeti:**
1. Sahne yüklenir
2. Space tuşuna basılır
3. Her iki oyuncu algılanana kadar beklenir
4. Oyun başlar (timer başlar, objeler düşer)
5. 60 saniye oynanır
6. Bitiş paneli gösterilir (her oyuncunun skoru)
7. Space tuşuna basılır → Ana gameplay'e dönülür
8. Tekrar Space'e basılır → Oyun yeniden başlar

Kolay gelsin! 🎮
