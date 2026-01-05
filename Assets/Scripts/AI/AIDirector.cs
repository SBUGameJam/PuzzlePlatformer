using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AIDirector (Narrative-only AI)
/// - Observes player style via ACTIONS (not time): logic vs emotion actions + switches.
/// - Computes per-level and whole-run style (LogicDominant / EmotionDominant / Balanced).
/// - DOES NOT punish any style. Endings are always "win", but narrative text changes.
/// 
/// New Design Integration (your abilities):
/// Logic:
///   - Default (environment interaction): usually NOT scored (mandatory puzzle steps).
///   - Special (Scan for safe paths): RegisterLogicAction(weight)
/// Emotion:
///   - Default (stomp enemies): RegisterEmotionAction(weight)
///   - Special (parkour dash/double jump/gravity): RegisterEmotionAction(weight)
/// Logic special (Swap places with Emotion): usually RegisterLogicAction(weight) (system intervention),
///   OR register as "switch" only—choose ONE consistent rule in your team.
///
/// IMPORTANT RULE:
/// - Do NOT score mandatory actions that are required to clear the level.
///   Score only "style signals" (optional / expressive / meaningful choices).
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

    [Header("Default Weights (optional)")]
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

        // Optional: store narrative key used for that level report
        public NarrativeKey narrativeKey;
    }

    private readonly List<LevelResult> levelResults = new List<LevelResult>();

    // -------------------------
    // Narrative (no punishment)
    // -------------------------
    public enum NarrativeKey
    {
        LogicReport,
        EmotionReport,
        BalancedReport
    }

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
    /// Call when starting a new game/run.
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
        // (Optional) store current level index if you want; not required.
        ResetLevelTelemetry();
    }

    /// <summary>
    /// Call when level is completed. Stores per-level result and adds to run totals.
    /// Returns the per-level result for UI.
    /// </summary>
    public LevelResult CompleteLevel(int levelIndex)
    {
        PlayerStyle style = InferStyle(levelLogicScore, levelEmotionScore);
        NarrativeKey key = GetNarrativeKey(style);

        var result = new LevelResult
        {
            levelIndex = levelIndex,
            logicScore = levelLogicScore,
            emotionScore = levelEmotionScore,
            switchCount = levelSwitchCount,
            style = style,
            narrativeKey = key
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
    // Gameplay hooks
    // -------------------------

    public void RegisterSwitch()
    {
        levelSwitchCount++;
    }

    /// <summary>
    /// Register a logic-relevant signal (e.g., Logic special "Scan safe path",
    /// Logic choosing a safe/systemic approach).
    /// </summary>
    public void RegisterLogicAction(int weight = -1)
    {
        if (weight < 0) weight = defaultLogicWeight;
        weight = Mathf.Max(1, weight);
        levelLogicScore += weight;
    }

    /// <summary>
    /// Register an emotion-relevant signal (e.g., Emotion stomp, parkour dash/double-jump/gravity use in danger).
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

    public PlayerStyle GetCurrentLevelStyle() => InferStyle(levelLogicScore, levelEmotionScore);
    public PlayerStyle GetRunStyle() => InferStyle(runLogicScore, runEmotionScore);

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
    // Narrative outcome (NO punishment)
    // -------------------------

    /// <summary>
    /// Always returns "win", but with a narrative key depending on inferred run style.
    /// </summary>
    public NarrativeKey GetRunNarrativeKey()
    {
        return GetNarrativeKey(GetRunStyle());
    }

    private NarrativeKey GetNarrativeKey(PlayerStyle style)
    {
        switch (style)
        {
            case PlayerStyle.LogicDominant: return NarrativeKey.LogicReport;
            case PlayerStyle.EmotionDominant: return NarrativeKey.EmotionReport;
            default: return NarrativeKey.BalancedReport;
        }
    }

    /// <summary>
    /// Returns a short narrative text you can show in UI at the end of a LEVEL.
    /// (You can replace these strings with your own writing later.)
    /// </summary>
    public string GetLevelNarrativeText(PlayerStyle style)
    {
        switch (style)
        {
            case PlayerStyle.LogicDominant:
                return "SYSTEM NOTE: Your choices favored certainty. Safe paths, clean solutions. The network approves.";
            case PlayerStyle.EmotionDominant:
                return "SYSTEM NOTE: You kept moving when it hurt. Risk over comfort. The network cannot predict you.";
            default:
                return "SYSTEM NOTE: Balance detected. Control and instinct in equal measure. An unstable equilibrium.";
        }
    }

    /// <summary>
    /// Returns a longer narrative text you can show for the FINAL screen (whole run).
    /// </summary>
    public string GetRunNarrativeText()
    {
        var style = GetRunStyle();
        switch (style)
        {
            case PlayerStyle.LogicDominant:
                return "TRANSFER LOG: Consciousness stabilized through rational dominance.\nOutcome: Controlled survival.\nThe system will remember your compliance.";
            case PlayerStyle.EmotionDominant:
                return "TRANSFER LOG: Consciousness persisted through emotional dominance.\nOutcome: Unlicensed humanity.\nThe system cannot fully erase you.";
            default:
                return "TRANSFER LOG: Consciousness stabilized by balance.\nOutcome: Dual-core integrity.\nLogic and emotion coexist—for now.";
        }
    }

    // -------------------------
    // Reporting (UI text)
    // -------------------------

    public string GetCurrentLevelReport(int levelIndex)
    {
        var style = GetCurrentLevelStyle();
        return
            "INFERENCE REPORT\n" +
            $"LEVEL: {levelIndex}\n" +
            $"STYLE: {style.ToString().ToUpperInvariant()}\n" +
            $"LOGIC SCORE: {levelLogicScore}\n" +
            $"EMOTION SCORE: {levelEmotionScore}\n" +
            $"SWITCHES: {levelSwitchCount}\n" +
            $"LOGIC: {(GetCurrentLevelLogicRatio() * 100f):0}%\n" +
            $"EMOTION: {(GetCurrentLevelEmotionRatio() * 100f):0}%\n" +
            $"TEXT: {GetLevelNarrativeText(style)}\n";
    }

    public string GetRunReport()
    {
        var style = GetRunStyle();
        return
            "RUN SUMMARY\n" +
            $"STYLE: {style.ToString().ToUpperInvariant()}\n" +
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
        Debug.Log(GetRunNarrativeText());
    }
}
