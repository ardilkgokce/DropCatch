using UnityEngine;

/// <summary>
/// Her basket GameObject'ine eklenir ve hangi oyuncuya ait olduğunu belirtir.
/// FallingObject2D collision'da bu component'i kullanarak hangi oyuncunun yakaladığını tespit eder.
/// </summary>
public class PlayerBasket : MonoBehaviour
{
    [Header("Player Identification")]
    [Tooltip("0 = Sol oyuncu (Player 1), 1 = Sağ oyuncu (Player 2)")]
    public int playerIndex = 0;
}
