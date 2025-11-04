# Unity UI Setup Guide - Final Version

Bu belge, güncellenmiş menu sisteminin Unity'de nasıl kurulacağını açıklar.

## Canvas Yapısı

### MainCanvas
1. **MenuPanel** (Panel)
   - **Title** (TextMeshProUGUI): "BASKET CATCH"
   - **Form Container**:
     - **NameInput** (TMP_InputField)
       - Placeholder: "İsminiz"
     - **EmailInput** (TMP_InputField) 
       - Placeholder: "E-mail Adresiniz"
     - **StartButton** (Button)
       - Text: "BAŞLA"
     - **ErrorText** (TextMeshProUGUI)
       - Color: Red
   - **LeaderboardContainer**:
     - **MenuScore1** (TextMeshProUGUI)
     - **MenuScore2** (TextMeshProUGUI)
     - **MenuScore3** (TextMeshProUGUI)
     - **MenuScore4** (TextMeshProUGUI)
     - **MenuScore5** (TextMeshProUGUI)
     - **MenuScore6** (TextMeshProUGUI)
     - **MenuScore7** (TextMeshProUGUI)
     - **MenuScore8** (TextMeshProUGUI)
     - **MenuScore9** (TextMeshProUGUI)
     - **MenuScore10** (TextMeshProUGUI)

2. **GameOverPanel** (Panel)
   - **Title** (TextMeshProUGUI): "SKORLAR"
   - **LeaderboardContainer**:
     - **GameOverScore1** (TextMeshProUGUI)
     - **GameOverScore2** (TextMeshProUGUI)
     - **GameOverScore3** (TextMeshProUGUI)
     - **GameOverScore4** (TextMeshProUGUI)
     - **GameOverScore5** (TextMeshProUGUI)
     - **GameOverScore6** (TextMeshProUGUI)
     - **GameOverScore7** (TextMeshProUGUI)
     - **GameOverScore8** (TextMeshProUGUI)
     - **GameOverScore9** (TextMeshProUGUI)
     - **GameOverScore10** (TextMeshProUGUI)
   - **InfoText** (TextMeshProUGUI): "Menüye dönmek için herhangi bir tuşa basın"

## MenuManager Component Ayarları

### Inspector'da Bağlanması Gereken Alanlar:

#### Panel Referansları
- **Menu Panel**: MainCanvas/MenuPanel
- **Game Over Panel**: MainCanvas/GameOverPanel

#### Kayıt Form Elemanları
- **Name Input**: MenuPanel'deki NameInput
- **Email Input**: MenuPanel'deki EmailInput
- **Start Button**: MenuPanel'deki StartButton
- **Error Text**: MenuPanel'deki ErrorText

#### Oyun Bitiş
- **Leaderboard Texts**: 10 elemanlı array (GameOverPanel için)
  - Element 0: GameOverPanel/GameOverScore1
  - Element 1: GameOverPanel/GameOverScore2
  - ...
  - Element 9: GameOverPanel/GameOverScore10
- **Game Over Timeout**: 10 (saniye)

#### Menü Liderlik Tablosu
- **Menu Leaderboard Texts**: 10 elemanlı array (MenuPanel için)
  - Element 0: MenuPanel/MenuScore1
  - Element 1: MenuPanel/MenuScore2
  - ...
  - Element 9: MenuPanel/MenuScore10

## Component Kurulumu

1. MenuManager GameObject'ine şu componentleri ekleyin:
   - MenuManager.cs
   - JsonLeaderboardManager.cs (otomatik eklenecek)
   - LeaderboardManager.cs (varsa deaktif edilecek)

## Önemli Notlar

1. **İki Ayrı Leaderboard**: Hem MenuPanel hem de GameOverPanel'de ayrı leaderboard gösterimi var
2. **JSON Dosya Yolu**: Oyun verileri `Application.persistentDataPath/leaderboard.json` konumunda saklanır
3. **Otomatik Dönüş**: GameOverPanel 10 saniye sonra otomatik olarak menüye döner
4. **Manuel Dönüş**: Herhangi bir tuşa basarak timeout'u atlayabilirsiniz
5. **Sıralama**: Skorlar otomatik olarak büyükten küçüğe sıralanır

## Skor Formatı

Hem MenuPanel hem de GameOverPanel'deki skorlar şu formatta gösterilir:
```
1. ARDIL GÖKÇE 960 PUAN
2. MEHMET YILMAZ 850 PUAN
3. ---------- --- PUAN
...
10. ---------- --- PUAN
```

## Text Ayarları

Tüm leaderboard text'leri için önerilen ayarlar:
- Font Size: 18-24
- Alignment: Left veya Center
- Color: Beyaz veya oyun temasına uygun
- Font: TMP varsayılan veya özel font

## Test Etmek İçin

1. Unity Editor'de MenuManager component'inde sağ tık → "Test Start Button"
2. Runtime'da Inspector'dan "Show File Path" ile JSON dosya konumunu görebilirsiniz
3. "Clear Leaderboard" ile tüm skorları temizleyebilirsiniz

## Oyun Akışı

1. **Menu Panel** gösterilir (Name, Email, Start button + Top 10 skorlar)
2. Oyuncu bilgilerini girer ve Start'a basar
3. Oyun başlar
4. Oyun bitince **GameOverPanel** gösterilir (Top 10 skorlar)
5. 10 saniye sonra veya herhangi bir tuşa basılınca menüye dönülür
6. Döngü tekrarlanır