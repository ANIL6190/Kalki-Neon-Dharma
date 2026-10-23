using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Story & Cinematic Manager for KALKI: NEON DHARMA.
/// Manages opening cinematic cutscenes, subtitle queue playback, story narration,
/// cinematic camera blends, and audio dialogue playback.
///
/// UNITY SETUP INSTRUCTIONS:
/// 1. Create an empty GameObject named 'StoryManager' in the scene and attach this script.
/// 2. Assign optional cinematic camera shot transforms (e.g. IntroWideShot, KalkiCloseUp).
/// 3. Assign an AudioSource for voiceover audio narration.
/// 4. Connect with GameUIManager for displaying subtitle text on screen.
/// 5. Check 'PlayCinematicOnStart' if you want the opening cutscene to automatically trigger on scene load.
///
/// DEPENDENCIES:
/// - UnityEngine
/// - KalkiController.cs (locks/unlocks player during cinematic)
/// - KalkiCameraController.cs (blends camera between cinematic shots and gameplay)
/// - GameUIManager.cs (displays subtitles)
///
/// TEST CASES:
/// - On Scene Start: If playCinematicOnStart is true, camera blends to cinematic shot, player is locked, intro subtitles display in sequence, voiceover plays.
/// - Upon cutscene finish: Player is unlocked, camera transitions smoothly back to third-person follow, mission begins.
/// - Adaptive Narration: Calling PlayNarration(dialogueLine) displays dynamic performance commentary.
/// </summary>
public class KalkiStoryManager : MonoBehaviour
{
    public static KalkiStoryManager Instance { get; private set; }

    [System.Serializable]
    public class CinematicSceneStep
    {
        [Tooltip("Speaker name (e.g. 'ANCIENT DHARMA AI', 'KALKI', 'MAYA').")]
        public string speakerName = "NARRATOR";

        [TextArea(2, 4)]
        [Tooltip("Subtitle text to display.")]
        public string subtitleText;

        [Tooltip("Duration in seconds this subtitle and shot remain active.")]
        public float duration = 4.0f;

        [Tooltip("Optional camera vantage point during this line.")]
        public Transform cameraShot;

        [Tooltip("Optional voiceover audio clip.")]
        public AudioClip voiceClip;
    }

    [Header("Cinematic Sequence Configuration")]
    [Tooltip("Play the intro cutscene automatically when the scene begins.")]
    [SerializeField] private bool playCinematicOnStart = true;

    [Tooltip("Allow pressing Space or Enter to skip the active cinematic.")]
    [SerializeField] private bool allowCinematicSkip = true;

    [Tooltip("List of cinematic story beats for the opening.")]
    [SerializeField] private List<CinematicSceneStep> introSequence = new List<CinematicSceneStep>();

    [Header("Audio")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("External Component References")]
    [SerializeField] private KalkiController playerController;
    [SerializeField] private KalkiCameraController cameraController;
    [SerializeField] private GameUIManager uiManager;

    // Events
    public event Action OnCinematicStarted;
    public event Action OnCinematicFinished;
    public event Action<string, string> OnSubtitleShown;

    // Internal State
    private Coroutine activeCinematicRoutine;
    private Coroutine activeNarrationRoutine;
    private bool isCinematicPlaying = false;

    public bool IsCinematicPlaying => isCinematicPlaying;

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

        // Auto-resolve references if missing
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<KalkiController>();
        }

        if (cameraController == null && Camera.main != null)
        {
            cameraController = Camera.main.GetComponent<KalkiCameraController>();
        }

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<GameUIManager>();
        }

        if (voiceAudioSource == null)
        {
            voiceAudioSource = GetComponent<AudioSource>();
            if (voiceAudioSource == null)
            {
                voiceAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        PopulateDefaultIntroIfEmpty();
    }

    private void Start()
    {
        if (playCinematicOnStart)
        {
            StartOpeningCinematic();
        }
    }

    private void Update()
    {
        if (isCinematicPlaying && allowCinematicSkip)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Escape))
            {
                SkipCinematic();
            }
        }
    }

    private void PopulateDefaultIntroIfEmpty()
    {
        if (introSequence == null || introSequence.Count == 0)
        {
            introSequence = new List<CinematicSceneStep>
            {
                new CinematicSceneStep
                {
                    speakerName = "DHARMA PROTOCOL AI",
                    subtitleText = "The Kali Yuga has reached terminal collapse. Neon corruption consumes the cosmos.",
                    duration = 3.5f
                },
                new CinematicSceneStep
                {
                    speakerName = "DHARMA PROTOCOL AI",
                    subtitleText = "Kalki, Tenth Avatar... awaken your bio-mechanical blade. Calibrate your martial neural matrix.",
                    duration = 4.0f
                },
                new CinematicSceneStep
                {
                    speakerName = "KALKI",
                    subtitleText = "The cycle resets through action. Initiate the trial targets.",
                    duration = 3.0f
                }
            };
        }
    }

    /// <summary>
    /// Begins the opening cinematic cutscene.
    /// </summary>
    public void StartOpeningCinematic()
    {
        if (isCinematicPlaying) return;

        if (activeCinematicRoutine != null)
        {
            StopCoroutine(activeCinematicRoutine);
        }

        activeCinematicRoutine = StartCoroutine(PlayCinematicRoutine());
    }

    private IEnumerator PlayCinematicRoutine()
    {
        isCinematicPlaying = true;
        OnCinematicStarted?.Invoke();

        // Lock player controls
        if (playerController != null)
        {
            playerController.SetMovementLocked(true);
        }

        for (int i = 0; i < introSequence.Count; i++)
        {
            CinematicSceneStep step = introSequence[i];

            // Blend camera to shot if provided
            if (cameraController != null && step.cameraShot != null)
            {
                cameraController.EnterCinematicShot(step.cameraShot, 2.5f);
            }

            // Play voice clip if available
            if (voiceAudioSource != null && step.voiceClip != null)
            {
                voiceAudioSource.clip = step.voiceClip;
                voiceAudioSource.Play();
            }

            // Display subtitle in UI
            if (uiManager != null)
            {
                uiManager.ShowSubtitle(step.speakerName, step.subtitleText, step.duration);
            }
            OnSubtitleShown?.Invoke(step.speakerName, step.subtitleText);

            yield return new WaitForSeconds(step.duration);
        }

        FinishCinematic();
    }

    /// <summary>
    /// Skips the ongoing cinematic directly to gameplay.
    /// </summary>
    public void SkipCinematic()
    {
        if (!isCinematicPlaying) return;

        if (activeCinematicRoutine != null)
        {
            StopCoroutine(activeCinematicRoutine);
            activeCinematicRoutine = null;
        }

        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
        {
            voiceAudioSource.Stop();
        }

        FinishCinematic();
    }

    private void FinishCinematic()
    {
        isCinematicPlaying = false;

        // Reset camera to player
        if (cameraController != null)
        {
            cameraController.ExitCinematicShot();
        }

        // Restore player movement
        if (playerController != null)
        {
            playerController.SetMovementLocked(false);
        }

        // Clear UI subtitles
        if (uiManager != null)
        {
            uiManager.ClearSubtitle();
        }

        OnCinematicFinished?.Invoke();

        // Notify GameFlowManager if active
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OnCinematicCompleted();
        }
    }

    /// <summary>
    /// Plays an adaptive narration line with voice and subtitle display.
    /// </summary>
    public void PlayNarration(string speaker, string text, float duration = 4.0f, AudioClip audioClip = null)
    {
        if (activeNarrationRoutine != null)
        {
            StopCoroutine(activeNarrationRoutine);
        }
        activeNarrationRoutine = StartCoroutine(NarrationRoutine(speaker, text, duration, audioClip));
    }

    private IEnumerator NarrationRoutine(string speaker, string text, float duration, AudioClip audioClip)
    {
        if (voiceAudioSource != null && audioClip != null)
        {
            voiceAudioSource.clip = audioClip;
            voiceAudioSource.Play();
        }

        if (uiManager != null)
        {
            uiManager.ShowSubtitle(speaker, text, duration);
        }

        OnSubtitleShown?.Invoke(speaker, text);
        yield return new WaitForSeconds(duration);
    }
}
