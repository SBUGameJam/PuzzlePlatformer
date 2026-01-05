using UnityEngine;

public class EnemyHead : MonoBehaviour
{
    public Enemy enemy;
    public float bounceForce = 3.5f;

    private void Awake()
    {
        if (enemy == null) enemy = GetComponentInParent<Enemy>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var emotion = other.GetComponent<EmotionController>();
        if (emotion == null) return;

        enemy?.Kill();

        var rb = other.attachedRigidbody;
        if (rb != null)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
        }
    }
}
