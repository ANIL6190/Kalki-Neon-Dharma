using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central HUD & Game UI Manager for KALKI: NEON DHARMA.
/// Controls HUD objective display, live countdown timer, subtitles,
/// animated mission announcement banners, and adaptive story evaluation cards.
///
/// UNITY SETUP INSTRUCTIONS:
/// 1. Create a Canvas GameObject in your scene named 'HUD_Canvas'.
/// 2. Attach this GameUIManager component to the Canvas (or a manager object).
/// 3. Link UI Text and Panel references in the Inspector:
///    - Subtitle Panel, Speaker Text, Subtitle Text
///    - Objective Text
///    - Timer Text
///    - Banner Panel, Banner Title Text, Banner Subtitle Text, Banner Image
///    - (Optional) Performance Summary Panel, Story Reaction Text, Stats Text
/// 4. If any UI references are left unassigned, fallback GUI or console logs will prevent null reference errors.
///
/// DEPENDENCIES:
/// - UnityEngine.UI (Text, Image, CanvasGroup)
/// - KalkiStoryManager.cs
/// - TrainingMissionManager.cs
///
/// TEST CASES:
/// - Subtitle Display: Calling ShowSubtitle("KALKI", "I am ready.", 3f) displays styled text that automatically fades after 3 seconds.
/// - Objective Update: Displays current targets destroyed in real-time.
/// - Banner Alert: Pops banner in center screen with neon color theme.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Subtitles UI")]
    [Tooltip("Panel containing subtitle text components.")]
    [SerializeField] private GameObject subtitlePanel;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text subtitleText;

    [Header("Mission Objective & Progress")]
    [Tooltip("Text displaying active mission instructions.")]
    [SerializeField] private Text objectiveText;

    [Tooltip("Text displaying elapsed time or countdown.")]
    [SerializeField] private Text timerText;

    [Header("Announcement Banner")]
    [Tooltip("Center banner panel for mission start/completion/failure.")]
    [SerializeField] private GameObject bannerPanel;
    [SerializeField] private Text bannerTitleText;
    [SerializeField] private Text bannerSubtitleText;
    [SerializeField] private Image bannerBackground;

    [Header("Adaptive Performance Summary Card")]
    [Tooltip("Summary card displayed upon completing training.")]
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private Text summaryTitleText;
    [SerializeField] private Text summaryStoryText;
    [SerializeField] private Text summaryStatsText;
    [SerializeField] private Text summaryPromptText;

    // Coroutine tracking
    private Coroutine subtitleCoroutine;
    private Coroutine bannerCoroutine;

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

        // Initialize panel states
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        if (bannerPanel != null) bannerPanel.SetActive(false);
        if (summaryPanel != null) summaryPanel.SetActive(false);
    }

    #region Subtitles
    /// <summary>
    /// Displays a subtitle dialogue with speaker name for a set duration.
    /// </summary>
    public void ShowSubtitle(string speaker, string message, float duration = 3.5f)
    {
        if (subtitleCoroutine != null)
        {
            StopCoroutine(subtitleCoroutine);
        }

        subtitleCoroutine = StartCoroutine(SubtitleRoutine(speaker, message, duration));
    }

    private IEnumerator SubtitleRoutine(string speaker, string message, float duration)
    {
        if (subtitlePanel != null) subtitlePanel.SetActive(true);

        if (speakerText != null)
        {
            speakerText.text = speaker;
        }

        if (subtitleText != null)
        {
            subtitleText.text = message;
        }

        yield return new WaitForSeconds(duration);

        ClearSubtitle();
    }

    public void ClearSubtitle()
    {
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        if (speakerText != null) speakerText.text = string.Empty;
        if (subtitleText != null) subtitleText.text = string.Empty;
    }
    #endregion

    #region Objective & Timer
    /// <summary>
    /// Updates the main objective instructions text.
    /// </summary>
    public void UpdateObjective(string text)
    {
        if (objectiveText != null)
        {
            objectiveText.text = text;
        }
    }

    /// <summary>
    /// Updates live timer display.
    /// </summary>
    public void UpdateTimer(float seconds, bool isCountdown)
    {
        if (timerText == null) return;

        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        int fraction = Mathf.FloorToInt((seconds * 10f) % 10f);

        string prefix = isCountdown ? "⏳ REMAINING: " : "⏱️ TIME: ";
        timerText.text = $"{prefix}{mins:00}:{secs:00}.{fraction}";

        // Warning color if time is critically low
        if (isCountdown && seconds < 10f)
        {
            timerText.color = Color.red;
        }
        else
        {
            timerText.color = Color.white;
        }
    }
    #endregion

    #region Mission Banners
    /// <summary>
    /// Displays an animated announcement banner on screen.
    /// </summary>
    public void ShowMissionBanner(string title, string subtitle, Color bannerColor, float duration = 3.0f)
    {
        if (bannerCoroutine != null)
        {
            StopCoroutine(bannerCoroutine);
        }

        bannerCoroutine = StartCoroutine(BannerRoutine(title, subtitle, bannerColor, duration));
    }

    private IEnumerator BannerRoutine(string title, string subtitle, Color bannerColor, float duration)
    {
        if (bannerPanel != null) bannerPanel.SetActive(true);

        if (bannerTitleText != null)
        {
            bannerTitleText.text = title;
            bannerTitleText.color = bannerColor;
        }

        if (bannerSubtitleText != null)
        {
            bannerSubtitleText.text = subtitle;
        }

        if (bannerBackground != null)
        {
            Color bg = bannerColor;
            bg.a = 0.25f; // Neon tint overlay
            bannerBackground.color = bg;
        }

        yield return new WaitForSeconds(duration);

        if (bannerPanel != null) bannerPanel.SetActive(false);
    }
    #endregion

    #region Performance Summary Card
    /// <summary>
    /// Displays the adaptive evaluation summary card with story commentary and statistics.
    /// </summary>
    public void ShowPerformanceSummary(string outcomeTitle, string storyText, float completionTime, int attempts, float accuracy)
    {
        if (summaryPanel != null) summaryPanel.SetActive(true);

        if (summaryTitleText != null)
        {
            summaryTitleText.text = outcomeTitle;
        }

        if (summaryStoryText != null)
        {
            summaryStoryText.text = $"\"{storyText}\"";
        }

        if (summaryStatsText != null)
        {
            summaryStatsText.text = $"⏱️ Time: {completionTime:F2}s   |   🔄 Attempts: {attempts}   |   🎯 Accuracy: {accuracy:F0}%";
        }

        if (summaryPromptText != null)
        {
            summaryPromptText.text = "Press [ENTER] to Proceed  •  Press [R] to Retry Trial";
        }
    }

    public void HidePerformanceSummary()
    {
        if (summaryPanel != null) summaryPanel.SetActive(false);
    }
    #endregion
}
