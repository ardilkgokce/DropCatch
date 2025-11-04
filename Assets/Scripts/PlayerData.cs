using System;

[Serializable]
public class PlayerData : IComparable<PlayerData>
{
    public string name;
    public string phone;
    public int score;
    public DateTime date;

    public PlayerData(string name, string phone, int score)
    {
        this.name = name;
        this.phone = phone;
        this.score = score;
        this.date = DateTime.Now;
    }

    // Sıralama için (yüksek skor önce)
    public int CompareTo(PlayerData other)
    {
        if (other == null) return 1;
        return other.score.CompareTo(this.score);
    }
}