using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelExitPortal : MonoBehaviour
{
    public float fadeOutDuration = 0.5f;
    public float loadDelayAfterFade = 0.05f;

    public Image fadeImage;

    private bool emotionInside;
    private bool logicInside;
    private bool transitioning;

    private void Awake()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<EmotionController>() != null)
            emotionInside = true;

        if (other.GetComponent<LogicController>() != null)
            logicInside = true;

        TryCompleteLevel();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<EmotionController>() != null)
            emotionInside = false;

        if (other.GetComponent<LogicController>() != null)
            logicInside = false;
    }

    private void TryCompleteLevel()
    {
        if (transitioning) return;
        if (emotionInside && logicInside)
            StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        transitioning = true;

        if (fadeImage != null)
        {
            yield return Fade(0f, 1f, fadeOutDuration);
        }

        if (loadDelayAfterFade > 0f)
            yield return new WaitForSeconds(loadDelayAfterFade);

        int current = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(current + 1);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeImage == null) yield break;

        float t = 0f;
        Color c = fadeImage.color;
        c.a = from;
        fadeImage.color = c;

        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float smooth = k * k * (3f - 2f * k);
            c.a = Mathf.Lerp(from, to, smooth);
            fadeImage.color = c;
            yield return null;
        }

        c.a = to;
        fadeImage.color = c;
    }
}
