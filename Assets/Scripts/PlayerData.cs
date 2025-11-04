using System;

[Serializable]
public class PlayerData : IComparable<PlayerData>
{
    public string name;
    public string email;
    public int score;
    public DateTime date;

    public PlayerData(string name, string email, int score)
    {
        this.name = name;
        this.email = email;
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