using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Master Game Flow Controller for KALKI: NEON DHARMA.
/// Orchestrates the high-level state machine connecting:
/// Opening Cinematic -> Training Mission -> Performance Evaluation -> Adaptive Story Reaction -> Next Phase / Retry.
///
/// Ensures strict single-ownership: neither StoryManager nor MissionManager starts cutscenes independently.
///
/// UNITY SETUP INSTRUCTIONS:
/// 1. Create an empty GameObject named 'GameFlowManager' in your scene.
/// 2. Attach this GameFlowManager component.
/// 3. Link references to KalkiStoryManager, TrainingMissionManager, and AdaptiveStoryDirector.
/// 4. Choose whether to play the intro cutscene on start ('PlayIntroOnStart').
///
/// DEPENDENCIES:
/// - KalkiStoryManager.cs
/// - TrainingMissionManager.cs
/// - AdaptiveStoryDirector.cs
/// - GameUIManager.cs
///
/// TEST CASES:
/// - On Scene Launch: Enters OpeningCinematic state and triggers StoryManager.
/// - Cutscene Finish: State transitions to TrainingPhase, controls activate, dummy targets spawn/activate.
/// - Target Trial Complete: State transitions to StoryEvaluation, adaptive narration plays, summary card appears.
/// - Press Enter: Proceeds to next phase/scene.
/// - Press R: Restarts current training trial cleanly without reloading entire scene if preferred.
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    public enum FlowState
    {
        Initialization,
        OpeningCinematic,
        TrainingPhase,
        StoryEvaluation,
        AwaitingNextPhase,
        MissionFailed
    }

    [Header("Flow Configuration")]
    [Tooltip("Play intro cinematic cutscene when scene launches.")]
    [SerializeField] private bool playIntroOnStart = true;

    [Tooltip("Name of the next scene to load upon advancing (optional).")]
    [SerializeField] private string nextSceneName = "";

    [Header("Current State (Read Only)")]
    [SerializeField] private FlowState currentState = FlowState.Initialization;

    [Header("Component References")]
    [SerializeField] private KalkiStoryManager storyManager;
    [SerializeField] private TrainingMissionManager missionManager;
    [SerializeField] private AdaptiveStoryDirector adaptiveDirector;
    [SerializeField] private GameUIManager uiManager;

    // Events
    public event Action<FlowState> OnStateChanged;

    public FlowState State => currentState;

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
        if (storyManager == null) storyManager = FindFirstObjectByType<KalkiStoryManager>();
        if (missionManager == null) missionManager = FindFirstObjectByType<TrainingMissionManager>();
        if (adaptiveDirector == null) adaptiveDirector = FindFirstObjectByType<AdaptiveStoryDirector>();
        if (uiManager == null) uiManager = FindFirstObjectByType<GameUIManager>();
    }

    private void Start()
    {
        if (playIntroOnStart && storyManager != null)
        {
            ChangeState(FlowState.OpeningCinematic);
            storyManager.StartOpeningCinematic();
        }
        else
        {
            StartTrainingMission();
        }
    }

    private void Update()
    {
        // Handle input for progressing or retrying after mission ends
        if (currentState == FlowState.AwaitingNextPhase)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                AdvanceToNextPhase();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                RetryTrainingMission();
            }
        }
        else if (currentState == FlowState.MissionFailed)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RetryTrainingMission();
            }
        }
    }

    private void ChangeState(FlowState newState)
    {
        currentState = newState;
        Debug.Log($"[GameFlowManager] State changed to: {newState}");
        OnStateChanged?.Invoke(newState);
    }

    /// <summary>
    /// Callback from KalkiStoryManager when the opening cinematic cutscene finishes.
    /// </summary>
    public void OnCinematicCompleted()
    {
        if (currentState == FlowState.OpeningCinematic)
        {
            StartTrainingMission();
        }
    }

    /// <summary>
    /// Begins the playable training mission.
    /// </summary>
    public void StartTrainingMission()
    {
        ChangeState(FlowState.TrainingPhase);

        if (uiManager != null)
        {
            uiManager.HidePerformanceSummary();
        }

        if (missionManager != null)
        {
            missionManager.StartMission();
        }
        else
        {
            Debug.LogWarning("[GameFlowManager] TrainingMissionManager reference missing!");
        }
    }

    /// <summary>
    /// Callback from TrainingMissionManager upon destroying all targets.
    /// </summary>
    public void OnMissionCompleted(float completionTime)
    {
        ChangeState(FlowState.StoryEvaluation);

        // AdaptiveStoryDirector reacts automatically via PerformanceTracker events
        // Transition to awaiting next phase after brief presentation delay
        ChangeState(FlowState.AwaitingNextPhase);
    }

    /// <summary>
    /// Callback from TrainingMissionManager upon mission failure.
    /// </summary>
    public void OnMissionFailed(string reason)
    {
        ChangeState(FlowState.MissionFailed);

        if (adaptiveDirector != null)
        {
            adaptiveDirector.DeliverFailureNarration();
        }
    }

    /// <summary>
    /// Retries the training mission immediately.
    /// </summary>
    public void RetryTrainingMission()
    {
        if (uiManager != null)
        {
            uiManager.HidePerformanceSummary();
        }

        if (missionManager != null)
        {
            missionManager.RestartMission();
            ChangeState(FlowState.TrainingPhase);
        }
        else
        {
            // Fallback scene reload
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    /// <summary>
    /// Advances to the next phase or stage of the game.
    /// </summary>
    public void AdvanceToNextPhase()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log($"[GameFlowManager] Advancing to scene: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.Log("[GameFlowManager] Calibration complete. Preparing next battle encounter...");
            if (uiManager != null)
            {
                uiManager.ShowMissionBanner("SYSTEM CALIBRATED", "Entering Kali Yuga Neon Wastelands...", Color.cyan, 4.0f);
            }
        }
    }
}
