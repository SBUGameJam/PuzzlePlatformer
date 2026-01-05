using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class UnstablePlatform : MonoBehaviour
{
    public float shakeTime = 2f;
    public float shakeFrequency = 25f;
    public float shakeAmplitude = 0.06f;

    public float fallForSeconds = 1.2f;
    public float fallSpeed = 10f;

    public float waitBeforeReturn = 4f;

    public float returnDuration = 0.35f;

    private Vector3 startPos;
    private bool triggered;
    private Collider2D col;

    private void Awake()
    {
        startPos = transform.position;
        col = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (triggered) return;

        if (collision.collider.GetComponent<EmotionController>() == null &&
            collision.collider.GetComponent<LogicController>() == null)
            return;

        triggered = true;
        StartCoroutine(Routine());
    }

    private IEnumerator Routine()
    {
        float t = 0f;
        while (t < shakeTime)
        {
            t += Time.deltaTime;
            float x = Mathf.Sin(Time.time * shakeFrequency) * shakeAmplitude;
            transform.position = startPos + new Vector3(x, 0f, 0f);
            yield return null;
        }

        transform.position = startPos;

        if (col != null) col.enabled = false;

        float fallT = 0f;
        while (fallT < fallForSeconds)
        {
            fallT += Time.deltaTime;
            transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(waitBeforeReturn);

        Vector3 from = transform.position;
        float r = 0f;
        float dur = Mathf.Max(0.01f, returnDuration);

        while (r < dur)
        {
            r += Time.deltaTime;
            float k = Mathf.Clamp01(r / dur);
            float smooth = k * k * (3f - 2f * k);
            transform.position = Vector3.Lerp(from, startPos, smooth);
            yield return null;
        }

        transform.position = startPos;

        if (col != null) col.enabled = true;
        triggered = false;
    }
}
