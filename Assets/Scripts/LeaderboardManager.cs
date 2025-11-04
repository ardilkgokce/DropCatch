using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardManager : MonoBehaviour
{
    private const string LEADERBOARD_KEY = "DropCatchLeaderboard";
    private const int MAX_ENTRIES = 10;
    
    private List<PlayerData> leaderboard = new List<PlayerData>();
    
    void Awake()
    {
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
        string json = JsonUtility.ToJson(new SerializableList<PlayerData>(leaderboard));
        PlayerPrefs.SetString(LEADERBOARD_KEY, json);
        PlayerPrefs.Save();
    }
    
    private void LoadLeaderboard()
    {
        if (PlayerPrefs.HasKey(LEADERBOARD_KEY))
        {
            string json = PlayerPrefs.GetString(LEADERBOARD_KEY);
            SerializableList<PlayerData> data = JsonUtility.FromJson<SerializableList<PlayerData>>(json);
            leaderboard = data.items ?? new List<PlayerData>();
        }
    }
    
    // Liderlik tablosunu temizle (geliştirme için)
    [ContextMenu("Clear Leaderboard")]
    public void ClearLeaderboard()
    {
        leaderboard.Clear();
        PlayerPrefs.DeleteKey(LEADERBOARD_KEY);
        PlayerPrefs.Save();
        Debug.Log("Liderlik tablosu temizlendi!");
    }
}

// JsonUtility List serialization için wrapper
[System.Serializable]
public class SerializableList<T>
{
    public List<T> items;
    
    public SerializableList(List<T> list)
    {
        items = list;
    }
}