using System.Collections;
using UnityEngine;

public class FallDeathEmotion : MonoBehaviour
{
    public float deathY = 0f;
    public float resetMargin = 0.5f;

    private bool locked;

    private void Update()
    {
        if (locked) return;

        if (transform.position.y < deathY)
        {
            locked = true;

            if (GameManager.I != null)
            {
                GameManager.I.RegisterDeathAndRespawn("Emotion");
                GameManager.I.RespawnEmotion();
            }

            StartCoroutine(UnlockWhenSafe());
        }
    }

    private IEnumerator UnlockWhenSafe()
    {
        while (transform.position.y < deathY + resetMargin)
            yield return null;

        locked = false;
    }
}
