# UI Kurulum Kılavuzu - DropCatch Giriş Menüsü

## 1. Canvas Oluşturma
1. Hierarchy'de sağ tık → UI → Canvas
2. Canvas'ı "MainCanvas" olarak adlandırın
3. Canvas Scaler:
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920x1080
   - Screen Match Mode: 0.5

## 2. Menu Panel (Ana Panel)
1. MainCanvas altında: Sağ tık → UI → Panel
2. "MenuPanel" olarak adlandırın
3. Rect Transform: Stretch all (tam ekran)
4. Image Component:
   - Color: Koyu arka plan (örn: 20, 20, 30, 255)
5. Canvas Group component ekleyin (fade animasyonu için)

## 3. Sol Panel - Liderlik Tablosu
1. MenuPanel altında: UI → Panel → "LeaderboardPanel"
2. Rect Transform:
   - Anchor: Middle Left
   - Pivot: (0.5, 0.5)
   - Pos X: 300, Pos Y: 0
   - Width: 500, Height: 600
3. Vertical Layout Group ekleyin:
   - Padding: 20
   - Spacing: 10

### Liderlik Tablosu Başlığı
1. LeaderboardPanel altında: UI → Text - TextMeshPro → "LeaderboardTitle"
2. Text: "EN İYİ 10 OYUNCU"
3. Font Size: 32
4. Alignment: Center
5. Font Style: Bold

### Liderlik Tablosu İçeriği
1. LeaderboardPanel altında: UI → Scroll View → "LeaderboardScrollView"
2. Content GameObject'ine:
   - Vertical Layout Group
   - Content Size Fitter (Vertical Fit: Preferred Size)

## 4. Sağ Panel - Kayıt Formu
1. MenuPanel altında: UI → Panel → "RegistrationPanel"
2. Rect Transform:
   - Anchor: Middle Right
   - Pivot: (0.5, 0.5)
   - Pos X: -300, Pos Y: 0
   - Width: 500, Height: 400
3. Vertical Layout Group:
   - Padding: 30
   - Spacing: 20

### Form Elemanları
1. **Logo/Başlık**: UI → Text - TextMeshPro
   - Text: "DROPCATCH"
   - Font Size: 48
   - Alignment: Center

2. **İsim Input**: UI → Input Field - TextMeshPro
   - Placeholder Text: "İsminiz..."
   - Character Limit: 20
   - Content Type: Standard

3. **Telefon Input**: UI → Input Field - TextMeshPro
   - Placeholder Text: "Telefon Numaranız..."
   - Character Limit: 11
   - Content Type: Integer Number

4. **Error Text**: UI → Text - TextMeshPro
   - Color: Red
   - Font Size: 16
   - Initial text: "" (boş)

5. **Başla Butonu**: UI → Button - TextMeshPro
   - Text: "OYUNA BAŞLA"
   - Font Size: 24
   - Colors: Normal (yeşil tonları)
   - Height: 60

## 5. Game Over Panel
1. MainCanvas altında: UI → Panel → "GameOverPanel"
2. Rect Transform: Stretch all
3. Image: Yarı saydam siyah (0, 0, 0, 200)
4. SetActive: false (başlangıçta kapalı)

### Game Over İçeriği
1. GameOverPanel altında merkeze bir Container Panel
2. İçinde:
   - "OYUN BİTTİ!" başlığı
   - Final skor text'i
   - Konfeti particle efekti için boş GameObject

## 6. Component Bağlantıları

### MenuManager GameObject'i:
1. Boş GameObject oluştur: "MenuManager"
2. Components ekle:
   - MenuManager script
   - LeaderboardManager script
   - Audio Source (game over ses efekti için)

### MenuManager Script Bağlantıları:
- Menu Panel: MenuPanel
- Leaderboard Panel: LeaderboardPanel
- Registration Panel: RegistrationPanel
- Name Input: İsim input field
- Phone Input: Telefon input field
- Start Button: Başla butonu
- Error Text: Error text
- Leaderboard Content: ScrollView/Viewport/Content
- Game Over Panel: GameOverPanel
- Final Score Text: GameOverPanel içindeki skor text
- Confetti Effect: (Particle System - opsiyonel)
- Game Over Audio: Audio Source component
- Success Sound: Alkış/başarı ses dosyası

### GameManager2D Güncellemesi:
- GameManager2D objesini bulun
- Inspector'da görünen yeni UI alanlarının hepsinin doğru bağlandığından emin olun

## 7. Particle System (Konfeti)
1. GameOverPanel altında: Effects → Particle System
2. Ayarlar:
   - Start Lifetime: 3-5
   - Start Speed: 5-10
   - Gravity Modifier: 1-2
   - Emission Rate: 50-100
   - Shape: Cone, Angle: 45
   - Renderer: Sprites-Default material
   - Start Color: Random Between Two Colors

## 8. Test Etme
1. Play mode'a geç
2. İsim ve telefon gir
3. Başla butonuna tıkla
4. Oyun bitince game over ekranını kontrol et
5. Liderlik tablosunun güncellendiğini kontrol et