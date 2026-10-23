using System;
using UnityEngine;

/// <summary>
/// Telemetry & Player Performance Tracker for KALKI: NEON DHARMA.
/// Records attempts, combat accuracy, completion speed, damage taken, and target eliminations.
/// Analyzes real-time metrics to calculate the player's performance tier for adaptive storytelling.
///
/// UNITY SETUP INSTRUCTIONS:
/// 1. Create an empty GameObject named 'PerformanceTracker' in your scene.
/// 2. Attach this PlayerPerformanceTracker component.
/// 3. Adjust threshold settings (e.g. Mastery Time < 22s, Max Mastery Attempts = 2).
/// 4. PlayerCombat, Health, and TrainingTarget will automatically report their actions to this tracker.
///
/// DEPENDENCIES:
/// - UnityEngine
/// - DialogueData.cs (PerformanceTier enum)
/// - TrainingMetrics.cs (Structured data snapshot)
///
/// TEST CASES:
/// - Completing mission under 20s on 1st try -> Evaluates to Mastery.
/// - Retrying 3 times and completing at 40s -> Evaluates to Resilient.
/// - Failing 3+ times or missing most strikes -> Evaluates to Struggling.
/// </summary>
public class PlayerPerformanceTracker : MonoBehaviour
{
    public static PlayerPerformanceTracker Instance { get; private set; }

    [Header("Evaluation Thresholds")]
    [Tooltip("Target completion time in seconds for Mastery tier.")]
    [SerializeField] private float masteryTimeThreshold = 25f;

    [Tooltip("Completion time above which player is considered struggling.")]
    [SerializeField] private float strugglingTimeThreshold = 55f;

    [Tooltip("Maximum attempts allowed for Mastery tier.")]
    [SerializeField] private int maxMasteryAttempts = 2;

    [Tooltip("Attempt count threshold triggering Struggling tier.")]
    [SerializeField] private int strugglingAttemptsThreshold = 3;

    [Tooltip("Minimum accuracy percentage required for Mastery.")]
    [SerializeField] private float masteryAccuracyThreshold = 65f;

    [Header("Real-Time Telemetry (Read Only)")]
    [SerializeField] private int totalAttempts = 0;
    [SerializeField] private int consecutiveFailures = 0;
    [SerializeField] private int totalSuccesses = 0;
    [SerializeField] private float lastCompletionTime = 0f;

    [SerializeField] private int basicAttacksCount = 0;
    [SerializeField] private int powerStrikesCount = 0;
    [SerializeField] private int hitsLandedCount = 0;
    [SerializeField] private float totalDamageDealt = 0f;
    [SerializeField] private float totalDamageTaken = 0f;
    [SerializeField] private int targetsDestroyedCount = 0;
    [SerializeField] private int totalTargetsCount = 0;

    // Events
    public event Action<PerformanceTier> OnPerformanceEvaluated;
    public event Action<TrainingMetrics> OnMetricsRecorded;

    // Public Getters
    public int TotalAttempts => totalAttempts;
    public int ConsecutiveFailures => consecutiveFailures;
    public int TotalSuccesses => totalSuccesses;
    public float LastCompletionTime => lastCompletionTime;
    public float TotalDamageTaken => totalDamageTaken;
    public int TotalAttacksFired => basicAttacksCount + powerStrikesCount;
    public float AccuracyPercentage
    {
        get
        {
            int total = TotalAttacksFired;
            if (total == 0) return 0f;
            return Mathf.Clamp((float)hitsLandedCount / total * 100f, 0f, 100f);
        }
    }

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
    /// Invoked whenever a training mission begins or restarts.
    /// </summary>
    public void OnMissionStarted(int totalTargets)
    {
        totalAttempts++;
        totalTargetsCount = totalTargets;
        targetsDestroyedCount = 0;
        basicAttacksCount = 0;
        powerStrikesCount = 0;
        hitsLandedCount = 0;
        totalDamageDealt = 0f;
        totalDamageTaken = 0f;

        Debug.Log($"[PlayerPerformanceTracker] Mission Attempt #{totalAttempts} initiated.");
    }

    /// <summary>
    /// Records combat attack swings fired by player.
    /// </summary>
    public void RecordAttackFired(bool isPowerStrike)
    {
        if (isPowerStrike)
            powerStrikesCount++;
        else
            basicAttacksCount++;
    }

    /// <summary>
    /// Records successful hit landed on target.
    /// </summary>
    public void RecordTargetHit(float damage)
    {
        hitsLandedCount++;
        totalDamageDealt += damage;
    }

    /// <summary>
    /// Records damage received by player.
    /// </summary>
    public void RecordDamageTaken(float damage)
    {
        totalDamageTaken += damage;
    }

    /// <summary>
    /// Records target elimination.
    /// </summary>
    public void RecordTargetDestroyed(TrainingTarget target)
    {
        targetsDestroyedCount++;
    }

    /// <summary>
    /// Invoked when mission completes successfully.
    /// </summary>
    public PerformanceTier OnMissionCompleted(float completionTime)
    {
        lastCompletionTime = completionTime;
        totalSuccesses++;
        consecutiveFailures = 0;

        PerformanceTier tier = EvaluatePerformance(completionTime, totalAttempts, totalDamageTaken, AccuracyPercentage);

        // Generate research metrics snapshot
        TrainingMetrics metrics = GenerateMetricsSnapshot(tier, true, "Objective Completed");
        OnMetricsRecorded?.Invoke(metrics);
        OnPerformanceEvaluated?.Invoke(tier);

        Debug.Log($"[PlayerPerformanceTracker] Evaluated Tier: {tier} (Time: {completionTime:F2}s, Attempts: {totalAttempts}, Accuracy: {AccuracyPercentage:F1}%)");
        return tier;
    }

    /// <summary>
    /// Invoked when mission fails.
    /// </summary>
    public PerformanceTier OnMissionFailed(string reason)
    {
        consecutiveFailures++;
        PerformanceTier tier = PerformanceTier.Struggling;

        TrainingMetrics metrics = GenerateMetricsSnapshot(tier, false, reason);
        OnMetricsRecorded?.Invoke(metrics);
        OnPerformanceEvaluated?.Invoke(tier);

        Debug.LogWarning($"[PlayerPerformanceTracker] Mission Failed (Attempt #{totalAttempts}): {reason}");
        return tier;
    }

    /// <summary>
    /// Rule-based adaptive tier evaluation algorithm.
    /// </summary>
    public PerformanceTier EvaluatePerformance(float time, int attempts, float damageTaken, float accuracy)
    {
        // 1. Mastery: Fast speed, few attempts, accurate strikes
        bool isFast = time <= masteryTimeThreshold;
        bool isFewAttempts = attempts <= maxMasteryAttempts;
        bool isAccurate = accuracy >= masteryAccuracyThreshold;
        bool lowDamage = damageTaken <= 20f;

        if (isFast && isFewAttempts && (isAccurate || lowDamage))
        {
            return PerformanceTier.Mastery;
        }

        // 2. Struggling: Multiple retries, slow completion, or excessive damage
        bool isSlow = time >= strugglingTimeThreshold;
        bool isManyRetries = attempts >= strugglingAttemptsThreshold || consecutiveFailures >= 2;
        bool lowAccuracy = accuracy < 35f;

        if (isSlow || isManyRetries || (damageTaken > 70f && lowAccuracy))
        {
            return PerformanceTier.Struggling;
        }

        // 3. Resilient: Steady progress, balanced performance
        return PerformanceTier.Resilient;
    }

    /// <summary>
    /// Generates structured telemetry snapshot for analysis and export.
    /// </summary>
    public TrainingMetrics GenerateMetricsSnapshot(PerformanceTier tier, bool success, string outcomeNotes)
    {
        TrainingMetrics m = new TrainingMetrics();
        m.timestampUtc = DateTime.UtcNow.ToString("o");
        m.attemptNumber = totalAttempts;
        m.consecutiveFailures = consecutiveFailures;
        m.missionSuccess = success;
        m.completionTimeSeconds = lastCompletionTime;
        m.totalAttacks = TotalAttacksFired;
        m.hitsLanded = hitsLandedCount;
        m.accuracyPercent = AccuracyPercentage;
        m.damageDealt = totalDamageDealt;
        m.damageTaken = totalDamageTaken;
        m.targetsEliminated = targetsDestroyedCount;
        m.totalTargets = totalTargetsCount;
        m.evaluatedTier = tier;
        m.outcomeNotes = outcomeNotes;

        return m;
    }
}
