using UnityEngine;

public class ActiveArrowGlow : MonoBehaviour
{
    public float pulseSpeed = 4f;
    public float pulseScaleAmount = 0.15f;

    public Color inactiveColor = Color.white;
    public Color activeColor = Color.cyan;

    private SpriteRenderer sr;
    private Vector3 baseScale;
    private bool active;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        SetActive(false);
    }

    private void Update()
    {
        if (!active) return;

        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseScaleAmount;
        transform.localScale = baseScale * (1f + pulse);
    }

    public void SetActive(bool value)
    {
        active = value;

        if (sr != null)
            sr.color = active ? activeColor : inactiveColor;

        transform.localScale = baseScale;
    }
}
