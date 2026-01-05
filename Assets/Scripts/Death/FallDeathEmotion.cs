using UnityEngine;

public class FallDeathEmotion : MonoBehaviour
{
    public float deathY = -5f;

    private void Update()
    {
        if (transform.position.y < deathY)
        {
            GameManager.I?.RegisterDeathAndRespawn("Emotion");
        }
    }
}
