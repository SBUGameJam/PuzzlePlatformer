using UnityEngine;
using TMPro;

public class AbilityUsageBarUI : MonoBehaviour
{
    public RectTransform bar;
    public RectTransform indicator;

    public TMP_Text emotionPointsText;
    public TMP_Text logicPointsText;

    public float edgePadding = 6f;
    public float smoothing = 12f;

    public float centerOffsetX = -28f;

    private float currentX;

    private void Awake()
    {
        if (indicator != null)
            currentX = indicator.anchoredPosition.x;
    }

    private void LateUpdate()
    {
        if (GameManager.I == null) return;
        if (bar == null || indicator == null) return;

        int eSpent = Mathf.Max(0, GameManager.I.EmotionSpentThisLevel);
        int lSpent = Mathf.Max(0, GameManager.I.LogicSpentThisLevel);
        int total = eSpent + lSpent;

        float bias;
        if (total <= 0) bias = 0f;
        else bias = Mathf.Clamp((lSpent - eSpent) / (float)total, -1f, 1f);

        float halfBar = bar.rect.width * 0.5f;
        float halfIndicator = indicator.rect.width * 0.5f;

        float maxX = Mathf.Max(0f, halfBar - halfIndicator - edgePadding);

        float targetX = centerOffsetX + (bias * maxX);

        currentX = Mathf.Lerp(currentX, targetX, 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));

        Vector2 p = indicator.anchoredPosition;
        p.x = currentX;
        indicator.anchoredPosition = p;

        if (emotionPointsText != null) emotionPointsText.text = GameManager.I.EmotionPoints.ToString();
        if (logicPointsText != null) logicPointsText.text = GameManager.I.LogicPoints.ToString();
    }
}
