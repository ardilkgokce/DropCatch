using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class JsonLeaderboardManager : MonoBehaviour
{
    private const string FILENAME = "leaderboard.json";
    private const int MAX_ENTRIES = 10;
    
    private List<PlayerData> leaderboard = new List<PlayerData>();
    private string filePath;
    
    void Awake()
    {
        // JSON dosyasının yolu
        filePath = Path.Combine(Application.persistentDataPath, FILENAME);
        Debug.Log($"Leaderboard JSON dosya yolu: {filePath}");
        
        LoadLeaderboard();
    }
    
    public void AddScore(PlayerData playerData)
    {
        leaderboard.Add(playerData);
        leaderboard.Sort(); // PlayerData'nın CompareTo metodunu kullanır
        
        // Sadece ilk 10'u tut
        if (leaderboard.Count > MAX_ENTRIES)
        {
            leaderboard = leaderboard.Take(MAX_ENTRIES).ToList();
        }
        
        SaveLeaderboard();
    }
    
    public List<PlayerData> GetTopScores()
    {
        return leaderboard;
    }
    
    public bool IsHighScore(int score)
    {
        if (leaderboard.Count < MAX_ENTRIES)
            return true;
            
        return score > leaderboard[leaderboard.Count - 1].score;
    }
    
    private void SaveLeaderboard()
    {
        try
        {
            string json = JsonUtility.ToJson(new SerializableList<PlayerData>(leaderboard), true);
            File.WriteAllText(filePath, json);
            Debug.Log($"Leaderboard JSON dosyaya kaydedildi: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Leaderboard kaydetme hatası: {e.Message}");
        }
    }
    
    private void LoadLeaderboard()
    {
        try
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                SerializableList<PlayerData> data = JsonUtility.FromJson<SerializableList<PlayerData>>(json);
                leaderboard = data.items ?? new List<PlayerData>();
                Debug.Log($"Leaderboard yüklendi. {leaderboard.Count} kayıt bulundu.");
            }
            else
            {
                Debug.Log("Leaderboard dosyası bulunamadı. Yeni bir tane oluşturulacak.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Leaderboard yükleme hatası: {e.Message}");
            leaderboard = new List<PlayerData>();
        }
    }
    
    // Liderlik tablosunu temizle (geliştirme için)
    [ContextMenu("Clear Leaderboard")]
    public void ClearLeaderboard()
    {
        leaderboard.Clear();
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"Leaderboard dosyası silindi: {filePath}");
        }
        Debug.Log("Liderlik tablosu temizlendi!");
    }
    
    // JSON dosya yolunu göster (debug için)
    [ContextMenu("Show File Path")]
    public void ShowFilePath()
    {
        Debug.Log($"Leaderboard JSON dosya yolu: {filePath}");
        Debug.Log($"Dosya mevcut mu: {File.Exists(filePath)}");
    }
}