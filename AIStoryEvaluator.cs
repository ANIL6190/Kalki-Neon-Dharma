using System;
using UnityEngine;

/// <summary>
/// AI & Research Story Evaluator for KALKI: NEON DHARMA.
/// Performs multi-feature heuristic and machine-learning ready scoring on player telemetry.
/// Computes composite performance weights to recommend narrative outcomes, pacing adjustments,
/// and comparative research evaluation (Fixed Baseline vs. Adaptive Narration).
///
/// UNITY SETUP INSTRUCTIONS:
/// 1. Create an empty GameObject or attach to 'AdaptiveStoryDirector'.
/// 2. Configure feature weights (Time Weight, Accuracy Weight, Damage Penalty, Attempt Penalty).
/// 3. Toggle 'EnableResearchLogging' to analyze player persistence and completion differences.
///
/// DEPENDENCIES:
/// - DialogueData.cs (PerformanceTier)
/// - PlayerPerformanceTracker.cs
/// - TrainingMetrics.cs
/// </summary>
public class AIStoryEvaluator : MonoBehaviour
{
    public static AIStoryEvaluator Instance { get; private set; }

    [Header("Evaluation Mode")]
    [Tooltip("Enable adaptive narration vs fixed linear narration for research experiments.")]
    [SerializeField] private bool useAdaptiveNarration = true;

    [Tooltip("Log comparative research analysis.")]
    [SerializeField] private bool enableResearchLogging = true;

    [Header("Scoring Feature Weights (Normalized sum ~ 1.0)")]
    [Tooltip("Weight given to fast completion speed.")]
    [Range(0f, 1f)]
    [SerializeField] private float timeWeight = 0.35f;

    [Tooltip("Weight given to attack hit accuracy.")]
    [Range(0f, 1f)]
    [SerializeField] private float accuracyWeight = 0.30f;

    [Tooltip("Weight penalty for damage taken.")]
    [Range(0f, 1f)]
    [SerializeField] private float damagePenaltyWeight = 0.20f;

    [Tooltip("Weight penalty for multiple retries/attempts.")]
    [Range(0f, 1f)]
    [SerializeField] private float attemptPenaltyWeight = 0.15f;

    [Header("Tier Score Thresholds")]
    [Range(0f, 1f)]
    [SerializeField] private float masteryScoreThreshold = 0.72f;

    [Range(0f, 1f)]
    [SerializeField] private float strugglingScoreThreshold = 0.38f;

    // Research tracking
    private int sessionTotalEvaluations = 0;
    private float averageCompositeScore = 0f;

    public bool UseAdaptiveNarration => useAdaptiveNarration;
    public float LastComputedCompositeScore { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Computes a normalized composite score [0.0 - 1.0] from performance tracker features.
    /// </summary>
    public float CalculateCompositeScore(PlayerPerformanceTracker tracker)
    {
        if (tracker == null) return 0.5f;

        // 1. Time Score (assumes 15s is perfect, 60s is 0)
        float normalizedTime = Mathf.Clamp01(1f - ((tracker.LastCompletionTime - 15f) / 45f));

        // 2. Accuracy Score [0 - 1]
        float normalizedAccuracy = Mathf.Clamp01(tracker.AccuracyPercentage / 100f);

        // 3. Health / Damage Penalty [0 - 1] (0 damage taken = 1.0, 100 damage = 0.0)
        float normalizedHealth = Mathf.Clamp01(1f - (tracker.TotalDamageTaken / 100f));

        // 4. Attempt Penalty (1 attempt = 1.0, 5 attempts = 0.0)
        float normalizedAttempts = Mathf.Clamp01(1f - ((tracker.TotalAttempts - 1) / 4f));

        // Weighted Linear Combination
        float composite = (normalizedTime * timeWeight) +
                          (normalizedAccuracy * accuracyWeight) +
                          (normalizedHealth * damagePenaltyWeight) +
                          (normalizedAttempts * attemptPenaltyWeight);

        LastComputedCompositeScore = Mathf.Clamp01(composite);

        // Update research stats
        sessionTotalEvaluations++;
        averageCompositeScore = ((averageCompositeScore * (sessionTotalEvaluations - 1)) + LastComputedCompositeScore) / sessionTotalEvaluations;

        if (enableResearchLogging)
        {
            Debug.Log($"[AIStoryEvaluator] Composite Score: {LastComputedCompositeScore:F3} | Avg: {averageCompositeScore:F3} (Time: {normalizedTime:F2}, Acc: {normalizedAccuracy:F2}, HP: {normalizedHealth:F2}, Att: {normalizedAttempts:F2})");
        }

        return LastComputedCompositeScore;
    }

    /// <summary>
    /// Refines the recommended PerformanceTier using AI composite scoring.
    /// </summary>
    public PerformanceTier RefinePerformanceTier(PerformanceTier baseTier, PlayerPerformanceTracker tracker)
    {
        if (!useAdaptiveNarration)
        {
            // Control group in research: always return default Resilient tier
            return PerformanceTier.Resilient;
        }

        float score = CalculateCompositeScore(tracker);

        if (score >= masteryScoreThreshold)
        {
            return PerformanceTier.Mastery;
        }
        else if (score <= strugglingScoreThreshold)
        {
            return PerformanceTier.Struggling;
        }
        else
        {
            return PerformanceTier.Resilient;
        }
    }
}
