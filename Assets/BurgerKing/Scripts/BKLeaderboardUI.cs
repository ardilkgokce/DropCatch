using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>İlk 10 listesini gösterir; oyuncunun yeni girdisini vurgular, satırları sırayla belirtir.</summary>
public class BKLeaderboardUI : MonoBehaviour
{
    public GameObject root;

    [Tooltip("Sıra + isim yazıları (sola hizalı)")]
    public TextMeshProUGUI[] rows;

    [Tooltip("Puan yazıları (sağa hizalı); boşsa puan isim satırına eklenir")]
    public TextMeshProUGUI[] scoreTexts;

    [Tooltip("Vurgulanan satırın arka planı")]
    public Image[] rowBackgrounds;

    public Color normalColor = new Color(0.1f, 0.09f, 0.08f, 1f);
    public Color highlightColor = new Color(0.82f, 0.15f, 0.12f, 1f);
    public Color highlightBackground = new Color(0.91f, 0.86f, 0.78f, 1f);

    [Header("Animasyon")]
    public float rowFadeDuration = 0.25f;
    public float rowStagger = 0.06f;

    // Türkçe büyük harf (i → İ, ı → I); invariant culture 'i'yi 'I' yapar
    private static readonly System.Globalization.CultureInfo TurkishCulture = new System.Globalization.CultureInfo("tr-TR");

    private Coroutine revealRoutine;

    public void Show(List<PlayerData> top, string highlightName, int highlightScore)
    {
        if (root) root.SetActive(true);
        if (rows == null) return;

        bool highlighted = false;
        for (int i = 0; i < rows.Length; i++)
        {
            if (!rows[i]) continue;

            TextMeshProUGUI score = scoreTexts != null && i < scoreTexts.Length ? scoreTexts[i] : null;
            Image background = rowBackgrounds != null && i < rowBackgrounds.Length ? rowBackgrounds[i] : null;
            bool isMine = false;

            if (top != null && i < top.Count && top[i] != null)
            {
                PlayerData p = top[i];
                string name = string.IsNullOrEmpty(p.name) ? "-" : p.name.ToUpper(TurkishCulture);
                if (score)
                {
                    rows[i].text = (i + 1) + ".  " + name;
                    score.text = p.score.ToString();
                }
                else
                {
                    rows[i].text = (i + 1) + ".  " + name + "<pos=80%>" + p.score;
                }

                isMine = !highlighted && highlightName != null && p.name == highlightName && p.score == highlightScore;
                if (isMine) highlighted = true;
            }
            else
            {
                rows[i].text = (i + 1) + ".  ---";
                if (score) score.text = "-";
                else rows[i].text += "<pos=80%>-";
            }

            rows[i].color = isMine ? highlightColor : normalColor;
            if (score) score.color = isMine ? highlightColor : normalColor;
            if (background)
            {
                Color bg = highlightBackground;
                bg.a = isMine ? highlightBackground.a : 0f;
                background.color = bg;
            }
        }

        if (revealRoutine != null) StopCoroutine(revealRoutine);
        revealRoutine = StartCoroutine(RevealRows());
    }

    public void Hide()
    {
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }
        if (root) root.SetActive(false);
    }

    IEnumerator RevealRows()
    {
        SetRowsAlpha(0f);

        float total = rowFadeDuration + rowStagger * Mathf.Max(0, rows.Length - 1);
        float t = 0f;
        while (t < total)
        {
            t += Time.deltaTime;
            for (int i = 0; i < rows.Length; i++)
            {
                float a = Mathf.Clamp01((t - i * rowStagger) / Mathf.Max(0.01f, rowFadeDuration));
                SetRowAlpha(i, a);
            }
            yield return null;
        }

        SetRowsAlpha(1f);
        revealRoutine = null;
    }

    void SetRowsAlpha(float a)
    {
        for (int i = 0; i < rows.Length; i++) SetRowAlpha(i, a);
    }

    void SetRowAlpha(int i, float a)
    {
        if (rows[i]) rows[i].alpha = a;
        if (scoreTexts != null && i < scoreTexts.Length && scoreTexts[i]) scoreTexts[i].alpha = a;
    }
}
