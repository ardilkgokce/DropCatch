using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Başlangıç ekranı: BAŞLA butonu (veya Space) oyunu başlatır.</summary>
public class BKStartScreenUI : MonoBehaviour
{
    public GameObject root;
    public Button startButton;
    public TextMeshProUGUI hintText;

    void Awake()
    {
        if (startButton) startButton.onClick.AddListener(OnStartClicked);
    }

    void OnStartClicked()
    {
        if (BKGameManager.Instance) BKGameManager.Instance.OnStartPressed();
    }

    public void Show()
    {
        if (root) root.SetActive(true);
    }

    public void Hide()
    {
        if (root) root.SetActive(false);
    }
}
