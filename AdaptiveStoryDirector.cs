using System;
using UnityEngine;

/// <summary>
/// Adaptive Story Director for KALKI: NEON DHARMA.
/// Dynamically selects narrative outcomes, dialogue lines, and mentor reactions
/// based on the player's performance tier (Mastery, Resilient, or Struggling).
/// Dispatches the chosen narration to KalkiStoryManager and GameUIManager.
///
/// UNITY SETUP INSTRUCTIONS:
/// 1. Create an empty GameObject named 'AdaptiveStoryDirector' in your scene.
/// 2. Attach this AdaptiveStoryDirector script.
/// 3. Assign your DialogueData ScriptableObject asset (created via Right Click -> Create -> Kalki -> Dialogue Data).
/// 4. Connect references to KalkiStoryManager, PlayerPerformanceTracker, and GameUIManager (auto-detected if null).
/// 5. (Optional) Check 'LogTelemetryToDisk' to write CSV research data on every mission run.
///
/// DEPENDENCIES:
/// - DialogueData.cs
/// - PlayerPerformanceTracker.cs
/// - KalkiStoryManager.cs
/// - GameUIManager.cs
/// - TrainingMetrics.cs
///
/// TEST CASES:
/// - Player completes trial rapidly: Story director selects Mastery narration ("Your strength awakens. Destiny calls.").
/// - Player completes after retries: Selects Resilient narration ("A true warrior learns from every failure.").
/// - Player fails or struggles: Selects Endurance narration ("Rise again, Kalki. Your journey has only begun.").
/// </summary>
public class AdaptiveStoryDirector : MonoBehaviour
{
    public static AdaptiveStoryDirector Instance { get; private set; }

    [Header("Story Dialogue Database")]
    [Tooltip("DialogueData asset containing narration lines mapped to performance tiers.")]
    [SerializeField] private DialogueData dialogueDatabase;

    [Header("Telemetry Logging")]
    [Tooltip("Automatically save research telemetry to persistent CSV log file.")]
    [SerializeField] private bool logTelemetryToDisk = true;

    [Header("Component References")]
    [SerializeField] private KalkiStoryManager storyManager;
    [SerializeField] private PlayerPerformanceTracker performanceTracker;
    [SerializeField] private GameUIManager uiManager;
    [SerializeField] private AIStoryEvaluator aiEvaluator;

    // Events
    public event Action<PerformanceTier, DialogueLine> OnNarrationSelected;

    // Internal State
    private PerformanceTier currentTier = PerformanceTier.Resilient;
    private DialogueLine lastSelectedLine;

    public PerformanceTier CurrentTier => currentTier;
    public DialogueLine LastSelectedLine => lastSelectedLine;

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

        // Auto-resolve components
        if (storyManager == null)
        {
            storyManager = FindFirstObjectByType<KalkiStoryManager>();
        }

        if (performanceTracker == null)
        {
            performanceTracker = FindFirstObjectByType<PlayerPerformanceTracker>();
        }

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<GameUIManager>();
        }

        if (aiEvaluator == null)
        {
            aiEvaluator = FindFirstObjectByType<AIStoryEvaluator>();
        }
    }

    private void Start()
    {
        // Listen to performance tracker
        if (performanceTracker != null)
        {
            performanceTracker.OnPerformanceEvaluated += HandlePerformanceEvaluated;
            performanceTracker.OnMetricsRecorded += HandleMetricsRecorded;
        }
    }

    private void OnDestroy()
    {
        if (performanceTracker != null)
        {
            performanceTracker.OnPerformanceEvaluated -= HandlePerformanceEvaluated;
            performanceTracker.OnMetricsRecorded -= HandleMetricsRecorded;
        }
    }

    private void HandlePerformanceEvaluated(PerformanceTier evaluatedTier)
    {
        currentTier = evaluatedTier;

        // If an AI evaluator is present, allow it to refine the tier
        if (aiEvaluator != null && performanceTracker != null)
        {
            currentTier = aiEvaluator.RefinePerformanceTier(currentTier, performanceTracker);
        }

        SelectAndDeliverNarration(currentTier);
    }

    private void HandleMetricsRecorded(TrainingMetrics metrics)
    {
        if (logTelemetryToDisk && metrics != null)
        {
            metrics.SaveToPersistentCsv();
        }
    }

    /// <summary>
    /// Selects narration matching the tier and dispatches subtitles, audio, and UI summary card.
    /// </summary>
    public void SelectAndDeliverNarration(PerformanceTier tier)
    {
        DialogueLine line;
        string tierTitle;

        if (dialogueDatabase != null)
        {
            line = dialogueDatabase.GetLineForTier(tier);
            tierTitle = dialogueDatabase.GetTierTitle(tier);
        }
        else
        {
            // Fallback default lines if DialogueData asset is not yet assigned
            line = GetFallbackLine(tier);
            tierTitle = tier.ToString();
        }

        lastSelectedLine = line;

        // 1. Deliver voice & subtitle via KalkiStoryManager
        if (storyManager != null)
        {
            storyManager.PlayNarration(line.speaker, line.text, line.duration, line.voiceClip);
        }

        // 2. Display summary evaluation card in UI
        if (uiManager != null && performanceTracker != null)
        {
            uiManager.ShowPerformanceSummary(
                tierTitle,
                line.text,
                performanceTracker.LastCompletionTime,
                performanceTracker.TotalAttempts,
                performanceTracker.AccuracyPercentage
            );
        }

        OnNarrationSelected?.Invoke(tier, line);
        Debug.Log($"[AdaptiveStoryDirector] Dispatched narration for {tier}: \"{line.text}\"");
    }

    /// <summary>
    /// Delivers failure commentary when a trial is failed.
    /// </summary>
    public void DeliverFailureNarration()
    {
        DialogueLine line;
        if (dialogueDatabase != null)
        {
            line = dialogueDatabase.GetFailureLine();
        }
        else
        {
            line = new DialogueLine
            {
                speaker = "DHARMA ARCHITECT",
                text = "Rise again, Kalki. Your journey has only begun.",
                duration = 3.5f
            };
        }

        if (storyManager != null)
        {
            storyManager.PlayNarration(line.speaker, line.text, line.duration, line.voiceClip);
        }
    }

    private DialogueLine GetFallbackLine(PerformanceTier tier)
    {
        switch (tier)
        {
            case PerformanceTier.Mastery:
                return new DialogueLine
                {
                    speaker = "DHARMA ARCHITECT",
                    text = "Your strength awakens. Destiny calls.",
                    duration = 4.0f
                };
            case PerformanceTier.Resilient:
                return new DialogueLine
                {
                    speaker = "DHARMA ARCHITECT",
                    text = "A true warrior learns from every failure.",
                    duration = 4.0f
                };
            case PerformanceTier.Struggling:
            default:
                return new DialogueLine
                {
                    speaker = "DHARMA ARCHITECT",
                    text = "Rise again, Kalki. Your journey has only begun.",
                    duration = 4.0f
                };
        }
    }
}
