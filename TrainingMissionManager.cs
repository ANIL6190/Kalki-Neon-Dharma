using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Training Mission Manager for KALKI: NEON DHARMA.
/// Orchestrates the combat calibration training trial.
/// Tracks objective progress, target destruction, mission time limit, and triggers
/// completion or failure events for UI, performance tracking, and adaptive storytelling.
///
/// UNITY SETUP INSTRUCTIONS:
/// 1. Create an empty GameObject named 'TrainingMissionManager' and attach this script.
/// 2. Drag scene TrainingTarget instances into the 'Targets' list (or leave empty to auto-find in scene).
/// 3. Configure 'HasTimeLimit' and 'TimeLimitSeconds' (e.g. 60 seconds).
/// 4. Connect references to GameUIManager and PlayerPerformanceTracker (auto-detected if null).
/// 5. Check 'AutoStartMission' if you want it to trigger immediately (or let GameFlowManager start it).
///
/// DEPENDENCIES:
/// - UnityEngine
/// - TrainingTarget.cs
/// - PlayerPerformanceTracker.cs
/// - GameUIManager.cs
/// - GameFlowManager.cs
///
/// TEST CASES:
/// - On Start: Objective displays "Destroy All Training Dummies (0/N)".
/// - Strike Target: Count updates dynamically on HUD.
/// - Destroy All Targets: Mission completes, banner displays, performance metrics are captured, adaptive story triggers.
/// - Timer Expired: If time limit is enabled, triggers Mission Failed with option to retry.
/// </summary>
public class TrainingMissionManager : MonoBehaviour
{
    public static TrainingMissionManager Instance { get; private set; }

    public enum MissionState
    {
        NotStarted,
        InProgress,
        Completed,
        Failed
    }

    [Header("Mission Configuration")]
    [Tooltip("Mission objective title displayed on UI.")]
    [SerializeField] private string missionTitle = "CALIBRATION TRIAL: TARGET ELIMINATION";

    [Tooltip("Mission objective instruction description.")]
    [SerializeField] private string objectiveDescription = "Strike and destroy all holographic combat dummies.";

    [Tooltip("Whether this mission has a strict countdown time limit.")]
    [SerializeField] private bool hasTimeLimit = true;

    [Tooltip("Countdown duration in seconds.")]
    [SerializeField] private float timeLimitSeconds = 60f;

    [Tooltip("Start mission automatically in Start() if not managed by GameFlowManager.")]
    [SerializeField] private bool autoStartMission = false;

    [Header("Mission Targets")]
    [Tooltip("Targets required for mission. Auto-populated from scene if empty.")]
    [SerializeField] private List<TrainingTarget> targets = new List<TrainingTarget>();

    [Header("Component References")]
    [SerializeField] private GameUIManager uiManager;
    [SerializeField] private PlayerPerformanceTracker performanceTracker;

    // Events
    public event Action OnMissionStarted;
    public event Action<int, int> OnObjectiveProgress; // (current, total)
    public event Action<float> OnMissionCompleted;     // (completionTime)
    public event Action<string> OnMissionFailed;       // (reason)

    // Internal State
    private MissionState currentState = MissionState.NotStarted;
    private int targetsDestroyed = 0;
    private int totalTargets = 0;
    private float elapsedTime = 0f;
    private float remainingTime = 0f;

    public MissionState State => currentState;
    public bool IsInProgress => currentState == MissionState.InProgress;
    public float ElapsedTime => elapsedTime;
    public float RemainingTime => remainingTime;
    public int TargetsDestroyed => targetsDestroyed;
    public int TotalTargets => totalTargets;

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

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<GameUIManager>();
        }

        if (performanceTracker == null)
        {
            performanceTracker = FindFirstObjectByType<PlayerPerformanceTracker>();
        }
    }

    private void Start()
    {
        InitializeTargets();

        if (autoStartMission)
        {
            StartMission();
        }
    }

    private void Update()
    {
        if (currentState != MissionState.InProgress) return;

        elapsedTime += Time.deltaTime;

        if (hasTimeLimit)
        {
            remainingTime -= Time.deltaTime;
            if (uiManager != null)
            {
                uiManager.UpdateTimer(remainingTime, true);
            }

            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                FailMission("Time Limit Expired! Matrix overload.");
            }
        }
        else
        {
            if (uiManager != null)
            {
                uiManager.UpdateTimer(elapsedTime, false);
            }
        }
    }

    private void InitializeTargets()
    {
        if (targets == null || targets.Count == 0)
        {
            targets = new List<TrainingTarget>(FindObjectsByType<TrainingTarget>(FindObjectsSortMode.None));
        }

        totalTargets = targets.Count;
        targetsDestroyed = 0;

        // Register listener on each target
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null)
            {
                targets[i].OnDestroyedEvent += HandleTargetDestroyed;
            }
        }
    }

    /// <summary>
    /// Begins the training mission and initiates timer/tracking.
    /// </summary>
    public void StartMission()
    {
        if (currentState == MissionState.InProgress) return;

        currentState = MissionState.InProgress;
        targetsDestroyed = 0;
        elapsedTime = 0f;
        remainingTime = timeLimitSeconds;

        if (uiManager != null)
        {
            uiManager.ShowMissionBanner("TRIAL INITIATED", missionTitle, new Color(0.2f, 0.9f, 1f));
            UpdateObjectiveUI();
        }

        if (performanceTracker != null)
        {
            performanceTracker.OnMissionStarted(totalTargets);
        }

        OnMissionStarted?.Invoke();
        Debug.Log($"[TrainingMissionManager] Mission started. Total targets: {totalTargets}");
    }

    private void HandleTargetDestroyed(TrainingTarget target)
    {
        if (currentState != MissionState.InProgress) return;

        targetsDestroyed++;
        UpdateObjectiveUI();

        if (performanceTracker != null)
        {
            performanceTracker.RecordTargetDestroyed(target);
        }

        OnObjectiveProgress?.Invoke(targetsDestroyed, totalTargets);

        // Check completion condition
        if (targetsDestroyed >= totalTargets && totalTargets > 0)
        {
            CompleteMission();
        }
    }

    private void UpdateObjectiveUI()
    {
        if (uiManager != null)
        {
            string status = $"🎯 {objectiveDescription} ({targetsDestroyed}/{totalTargets})";
            uiManager.UpdateObjective(status);
        }
    }

    /// <summary>
    /// Successfully completes the training mission.
    /// </summary>
    public void CompleteMission()
    {
        if (currentState != MissionState.InProgress) return;

        currentState = MissionState.Completed;
        Debug.Log($"[TrainingMissionManager] Mission Completed in {elapsedTime:F2}s!");

        if (uiManager != null)
        {
            uiManager.ShowMissionBanner("TRIAL COMPLETED", "All targets eliminated! Calibrating neural matrix...", Color.green);
        }

        if (performanceTracker != null)
        {
            performanceTracker.OnMissionCompleted(elapsedTime);
        }

        OnMissionCompleted?.Invoke(elapsedTime);

        // Notify GameFlowManager
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OnMissionCompleted(elapsedTime);
        }
    }

    /// <summary>
    /// Fails the mission (e.g. time ran out or player defeated).
    /// </summary>
    public void FailMission(string reason)
    {
        if (currentState != MissionState.InProgress) return;

        currentState = MissionState.Failed;
        Debug.LogWarning($"[TrainingMissionManager] Mission Failed: {reason}");

        if (uiManager != null)
        {
            uiManager.ShowMissionBanner("TRIAL FAILED", reason, Color.red);
        }

        if (performanceTracker != null)
        {
            performanceTracker.OnMissionFailed(reason);
        }

        OnMissionFailed?.Invoke(reason);

        // Notify GameFlowManager
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OnMissionFailed(reason);
        }
    }

    /// <summary>
    /// Restarts the mission and respawns any destroyed targets.
    /// </summary>
    public void RestartMission()
    {
        currentState = MissionState.NotStarted;
        targetsDestroyed = 0;
        elapsedTime = 0f;
        remainingTime = timeLimitSeconds;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null)
            {
                targets[i].ResetTarget();
            }
        }

        StartMission();
    }
}
