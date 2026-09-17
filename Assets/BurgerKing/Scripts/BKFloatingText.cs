using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Yakalama anında yükselip solan "+5" / "-9" dünya-uzayı yazısı.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class BKFloatingText : MonoBehaviour
{
    public float riseDistance = 1.8f;
    public float duration = 0.9f;
    public float randomX = 0.4f;
    public int sortingOrder = 8;

    private TextMeshPro tmp;

    public void Play(string text, Color color)
    {
        tmp = GetComponent<TextMeshPro>();
        tmp.text = text;
        tmp.color = color;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr) mr.sortingOrder = sortingOrder;

        transform.position += new Vector3(Random.Range(-randomX, randomX), 0f, 0f);
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        Vector3 start = transform.position;
        Vector3 baseScale = transform.localScale;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            transform.position = start + Vector3.up * (riseDistance * BKEase.OutQuad(k));
            float punch = k < 0.25f
                ? Mathf.Lerp(0.6f, 1.15f, BKEase.OutBack(k / 0.25f))
                : Mathf.Lerp(1.15f, 1f, (k - 0.25f) / 0.75f);
            transform.localScale = baseScale * punch;
            if (tmp) tmp.alpha = k > 0.55f ? 1f - (k - 0.55f) / 0.45f : 1f;
            yield return null;
        }

        Destroy(gameObject);
    }
}
