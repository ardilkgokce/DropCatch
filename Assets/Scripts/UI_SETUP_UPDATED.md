# Unity UI Setup Guide - Updated

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
   - **LeaderboardContainer** (opsiyonel - eski sistem için)

2. **GameOverPanel** (Panel)
   - **Title** (TextMeshProUGUI): "SKORLAR"
   - **LeaderboardContainer**:
     - **Score1** (TextMeshProUGUI)
     - **Score2** (TextMeshProUGUI)
     - **Score3** (TextMeshProUGUI)
     - **Score4** (TextMeshProUGUI)
     - **Score5** (TextMeshProUGUI)
     - **Score6** (TextMeshProUGUI)
     - **Score7** (TextMeshProUGUI)
     - **Score8** (TextMeshProUGUI)
     - **Score9** (TextMeshProUGUI)
     - **Score10** (TextMeshProUGUI)
   - **InfoText** (TextMeshProUGUI): "Menüye dönmek için herhangi bir tuşa basın"

## MenuManager Component Ayarları

### Inspector'da Bağlanması Gereken Alanlar:

#### Panel Referansları
- **Menu Panel**: MainCanvas/MenuPanel
- **Game Over Panel**: MainCanvas/GameOverPanel

#### Kayıt Form Elemanları
- **Name Input**: MenuPanel'deki NameInput
- **Email Input**: MenuPanel'deki EmailInput (eski phoneInput yerine)
- **Start Button**: MenuPanel'deki StartButton
- **Error Text**: MenuPanel'deki ErrorText

#### Oyun Bitiş
- **Leaderboard Texts**: 10 elemanlı array
  - Element 0: GameOverPanel/Score1
  - Element 1: GameOverPanel/Score2
  - ...
  - Element 9: GameOverPanel/Score10
- **Game Over Timeout**: 10 (saniye)

#### Eski Liderlik Tablosu (opsiyonel)
- **Leaderboard Content**: MenuPanel'deki leaderboard container (varsa)
- **Leaderboard Entry Prefab**: null bırakılabilir

## Component Kurulumu

1. MenuManager GameObject'ine şu componentleri ekleyin:
   - MenuManager.cs
   - JsonLeaderboardManager.cs (otomatik eklenecek)
   - LeaderboardManager.cs (varsa deaktif edilecek)

## Önemli Notlar

1. **PhoneInput yerine EmailInput**: Tüm telefon referansları email olarak değiştirildi
2. **JSON Dosya Yolu**: Oyun verileri `Application.persistentDataPath/leaderboard.json` konumunda saklanır
3. **Otomatik Dönüş**: GameOverPanel 10 saniye sonra otomatik olarak menüye döner
4. **Manuel Dönüş**: Herhangi bir tuşa basarak timeout'u atlayabilirsiniz

## Skor Formatı

GameOverPanel'deki skorlar şu formatta gösterilir:
```
1. ARDIL GÖKÇE 960 PUAN
2. MEHMET YILMAZ 850 PUAN
3. ---------- --- PUAN
...
10. ---------- --- PUAN
```

## Test Etmek İçin

1. Unity Editor'de MenuManager component'inde sağ tık → "Test Start Button"
2. Veya runtime'da Inspector'dan "Show File Path" ile JSON dosya konumunu görebilirsiniz
3. "Clear Leaderboard" ile tüm skorları temizleyebilirsiniz