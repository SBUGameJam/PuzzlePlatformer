using System.Collections;
using UnityEngine;

public class RevealOnScan : MonoBehaviour, IRevealable
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D col;

    public float fadeInSeconds = 0.4f;
    public bool hiddenAsTrigger = true;

    private bool revealedPermanently;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (col == null) col = GetComponent<Collider2D>();

        if (col != null) col.enabled = true;

        SetHiddenState();
    }

    public void Reveal(float _)
    {
        if (revealedPermanently) return;
        revealedPermanently = true;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeInAndStay());
    }

    private IEnumerator FadeInAndStay()
    {
        if (col != null) col.isTrigger = false;

        if (sr == null) yield break;

        sr.enabled = true;

        Color c = sr.color;
        c.a = 0f;
        sr.color = c;

        float dur = Mathf.Max(0.01f, fadeInSeconds);
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / dur);
            sr.color = c;
            yield return null;
        }

        c.a = 1f;
        sr.color = c;
    }

    private void SetHiddenState()
    {
        if (sr != null)
        {
            sr.enabled = true;
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
        }

        if (col != null) col.isTrigger = hiddenAsTrigger;
    }
}
