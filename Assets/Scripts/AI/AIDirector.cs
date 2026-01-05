using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AIDirector for Option 2 (Action-based scoring)
/// - Hazards restart level (handled elsewhere)
/// - AI observes player style via ACTIONS (not time), so puzzle thinking time doesn't bias results.
/// - Per-level style is computed for feedback.
/// - Final ending depends on WHOLE RUN aggregated score.
/// 
/// Integration: call RegisterLogicAction / RegisterEmotionAction from your gameplay scripts.
/// </summary>
public class AIDirector : MonoBehaviour
{
    public static AIDirector Instance { get; private set; }

    // -------------------------
    // Style model
    // -------------------------
    public enum PlayerStyle { LogicDominant, EmotionDominant, Balanced }

    [Header("Style Thresholds")]
    [Tooltip("If one side exceeds this ratio in a level or in the run, it becomes dominant.")]
    [Range(0.51f, 0.90f)]
    [SerializeField] private float dominantRatioThreshold = 0.65f;

    [Tooltip("Minimum number of total actions required before we trust style inference. Below this -> Balanced.")]
    [SerializeField] private int minActionsToInfer = 3;

    [Header("Optional: Weighting")]
    [Tooltip("How much each logic action contributes by default.")]
    [SerializeField] private int defaultLogicWeight = 1;

    [Tooltip("How much each emotion action contributes by default.")]
    [SerializeField] private int defaultEmotionWeight = 1;

    // -------------------------
    // Telemetry (per level)
    // -------------------------
    [Header("Per-Level Telemetry (current level)")]
    [SerializeField] private int levelLogicScore = 0;
    [SerializeField] private int levelEmotionScore = 0;
    [SerializeField] private int levelSwitchCount = 0;

    // -------------------------
    // Whole-run totals
    // -------------------------
    [Header("Whole-Run Totals (across all levels)")]
    [SerializeField] private int runLogicScore = 0;
    [SerializeField] private int runEmotionScore = 0;
    [SerializeField] private int runSwitchCount = 0;

    // -------------------------
    // Per-level history (for reports/UI)
    // -------------------------
    public struct LevelResult
    {
        public int levelIndex;
        public int logicScore;
        public int emotionScore;
        public int switchCount;
        public PlayerStyle style;

        
    }

    private readonly List<LevelResult> levelResults = new List<LevelResult>();

    // -------------------------
    // Unity lifecycle
    // -------------------------
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // -------------------------
    // Run / Level boundaries
    // -------------------------

    /// <summary>
    /// Call when starting a new game/run (e.g., pressing Start Transfer).
    /// </summary>
    public void StartNewRun()
    {
        levelResults.Clear();

        runLogicScore = 0;
        runEmotionScore = 0;
        runSwitchCount = 0;

        ResetLevelTelemetry();
    }

    /// <summary>
    /// Call at the start of each level.
    /// </summary>
    public void StartLevel(int levelIndex)
    {
        // You can store current level index if you want; not required.
        ResetLevelTelemetry();
    }

    /// <summary>
    /// Call when level is completed. Stores per-level result and adds to run totals.
    /// Returns the per-level result for UI.
    /// </summary>
    public LevelResult CompleteLevel(int levelIndex)
    {
        PlayerStyle style = InferStyle(levelLogicScore, levelEmotionScore);

        var result = new LevelResult
        {
            levelIndex = levelIndex,
            logicScore = levelLogicScore,
            emotionScore = levelEmotionScore,
            switchCount = levelSwitchCount,
            style = style
        };

        levelResults.Add(result);

        // accumulate into run totals
        runLogicScore += levelLogicScore;
        runEmotionScore += levelEmotionScore;
        runSwitchCount += levelSwitchCount;

        return result;
    }

    /// <summary>
    /// Reset only current-level telemetry (use on restart).
    /// Important: Restarting a level should NOT add to run totals.
    /// Call this on death/retry. Do NOT call CompleteLevel on retry.
    /// </summary>
    public void ResetLevelTelemetry()
    {
        levelLogicScore = 0;
        levelEmotionScore = 0;
        levelSwitchCount = 0;
    }

    // -------------------------
    // Gameplay hooks (call these from other systems)
    // -------------------------

    public void RegisterSwitch()
    {
        levelSwitchCount++;
       
    }

    /// <summary>
    /// Register a logic-relevant action (hack terminal, disable hazard, solve logic gate...).
    /// </summary>
    public void RegisterLogicAction(int weight = -1)
    {
        if (weight < 0) weight = defaultLogicWeight;
        weight = Mathf.Max(1, weight);
        levelLogicScore += weight;
    }

    /// <summary>
    /// Register an emotion-relevant action (collect memory node, emotion gate, emotional leap...).
    /// </summary>
    public void RegisterEmotionAction(int weight = -1)
    {
        if (weight < 0) weight = defaultEmotionWeight;
        weight = Mathf.Max(1, weight);
        levelEmotionScore += weight;
    }

    // -------------------------
    // Inference (per-level and run)
    // -------------------------

    public PlayerStyle GetCurrentLevelStyle()
    {
        return InferStyle(levelLogicScore, levelEmotionScore);
    }

    public PlayerStyle GetRunStyle()
    {
        return InferStyle(runLogicScore, runEmotionScore);
    }

    public float GetCurrentLevelLogicRatio()
    {
        int total = levelLogicScore + levelEmotionScore;
        return total >= 1 ? (float)levelLogicScore / total : 0.5f;
    }

    public float GetCurrentLevelEmotionRatio()
    {
        int total = levelLogicScore + levelEmotionScore;
        return total >= 1 ? (float)levelEmotionScore / total : 0.5f;
    }

    public float GetRunLogicRatio()
    {
        int total = runLogicScore + runEmotionScore;
        return total >= 1 ? (float)runLogicScore / total : 0.5f;
    }

    public float GetRunEmotionRatio()
    {
        int total = runLogicScore + runEmotionScore;
        return total >= 1 ? (float)runEmotionScore / total : 0.5f;
    }

    private PlayerStyle InferStyle(int logicScore, int emotionScore)
    {
        int total = logicScore + emotionScore;
        if (total < minActionsToInfer) return PlayerStyle.Balanced;

        float logicRatio = (float)logicScore / total;
        float emotionRatio = (float)emotionScore / total;

        if (logicRatio >= dominantRatioThreshold) return PlayerStyle.LogicDominant;
        if (emotionRatio >= dominantRatioThreshold) return PlayerStyle.EmotionDominant;
        return PlayerStyle.Balanced;
    }

    // -------------------------
    // Final ending decision (Option 2)
    // -------------------------

    /// <summary>
    /// True -> Good ending, False -> Bad ending.
    /// By default: EmotionDominant across the whole run is required.
    /// You can loosen this later if you want Balanced -> neutral ending.
    /// </summary>
    /// 

    public enum EndingType { Good, Neutral, Bad }

    public EndingType GetEndingType()
    {
        var style = GetRunStyle();
        if (style == PlayerStyle.EmotionDominant) return EndingType.Good;
        if (style == PlayerStyle.Balanced) return EndingType.Neutral;
        return EndingType.Bad;
    }


    // -------------------------
    // Reporting (UI text)
    // -------------------------

    public string GetCurrentLevelReport(int levelIndex)
    {
        var style = GetCurrentLevelStyle().ToString().ToUpperInvariant();
        return
            "INFERENCE REPORT\n" +
            $"LEVEL: {levelIndex}\n" +
            $"STYLE: {style}\n" +
            $"LOGIC SCORE: {levelLogicScore}\n" +
            $"EMOTION SCORE: {levelEmotionScore}\n" +
            $"SWITCHES: {levelSwitchCount}\n";
    }

    public string GetRunReport()
    {
        var style = GetRunStyle().ToString().ToUpperInvariant();
        return
            "RUN SUMMARY\n" +
            $"STYLE: {style}\n" +
            $"LOGIC TOTAL: {runLogicScore}\n" +
            $"EMOTION TOTAL: {runEmotionScore}\n" +
            $"RUN SWITCHES: {runSwitchCount}\n" +
            $"LOGIC: {(GetRunLogicRatio() * 100f):0}%\n" +
            $"EMOTION: {(GetRunEmotionRatio() * 100f):0}%\n";
    }

    public IReadOnlyList<LevelResult> GetLevelResults() => levelResults;



    // -------------------------
    // Debug
    // -------------------------
    public void DebugPrintRunState()

    {
        Debug.Log(GetRunReport());
    }

}

