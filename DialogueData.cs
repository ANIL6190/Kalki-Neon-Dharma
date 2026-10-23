using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data container and ScriptableObject definitions for KALKI: NEON DHARMA adaptive storytelling.
/// Defines dialogue lines, performance threshold conditions, and dialogue tables categorized by player skill tiers.
///
/// UNITY SETUP INSTRUCTIONS:
/// 1. In your Project window, Right-Click -> Create -> Kalki -> Dialogue Data.
/// 2. Configure the lines and audio clips for each performance tier:
///    - Fast / Mastery Tier: e.g. "Your strength awakens. Destiny calls."
///    - Resilient / Learning Tier: e.g. "A true warrior learns from every failure."
///    - Struggling / Endurance Tier: e.g. "Rise again, Kalki. Your journey has only begun."
/// 3. Assign this asset to AdaptiveStoryDirector in the Inspector.
/// </summary>
public enum PerformanceTier
{
    Mastery,    // Fast completion, high accuracy, low/no damage
    Resilient,  // Completed after multiple attempts or moderate pace
    Struggling  // Slow, high damage taken, repeated failures
}

[System.Serializable]
public class DialogueLine
{
    [Tooltip("Speaker identifier (e.g. 'DHARMA AI', 'ANCIENT SAGE', 'KALKI').")]
    public string speaker = "DHARMA AI";

    [TextArea(2, 4)]
    [Tooltip("Subtitle or narration text content.")]
    public string text;

    [Tooltip("Optional voiceover audio clip.")]
    public AudioClip voiceClip;

    [Tooltip("Display duration on screen in seconds.")]
    public float duration = 4.0f;
}

[System.Serializable]
public class StoryTierBranch
{
    public PerformanceTier tier;
    [TextArea(1, 2)]
    public string tierTitle = "Mastery Awakening";
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
}

[CreateAssetMenu(fileName = "KalkiDialogueData", menuName = "Kalki/Dialogue Data", order = 1)]
public class DialogueData : ScriptableObject
{
    [Header("Fast / Mastery Tier (Quick completion, minimal attempts)")]
    [SerializeField] private StoryTierBranch masteryTier = new StoryTierBranch
    {
        tier = PerformanceTier.Mastery,
        tierTitle = "DIVINE HARMONY: MASTERY",
        dialogueLines = new List<DialogueLine>
        {
            new DialogueLine
            {
                speaker = "DHARMA ARCHITECT",
                text = "Your strength awakens. Destiny calls.",
                duration = 4.0f
            },
            new DialogueLine
            {
                speaker = "DHARMA ARCHITECT",
                text = "The cosmic frequencies resonate with your swift blade. The avatar is ready.",
                duration = 4.5f
            }
        }
    };

    [Header("Resilient / Learning Tier (Multiple attempts, steady progress)")]
    [SerializeField] private StoryTierBranch resilientTier = new StoryTierBranch
    {
        tier = PerformanceTier.Resilient,
        tierTitle = "IRON WILL: RESILIENCE",
        dialogueLines = new List<DialogueLine>
        {
            new DialogueLine
            {
                speaker = "DHARMA ARCHITECT",
                text = "A true warrior learns from every failure.",
                duration = 4.0f
            },
            new DialogueLine
            {
                speaker = "DHARMA ARCHITECT",
                text = "Your strikes refine through persistence. Adaptability is the highest form of dharma.",
                duration = 4.5f
            }
        }
    };

    [Header("Struggling / Endurance Tier (Repeated retries, heavy damage)")]
    [SerializeField] private StoryTierBranch strugglingTier = new StoryTierBranch
    {
        tier = PerformanceTier.Struggling,
        tierTitle = "TRIAL OF ASHES: ENDURANCE",
        dialogueLines = new List<DialogueLine>
        {
            new DialogueLine
            {
                speaker = "DHARMA ARCHITECT",
                text = "Rise again, Kalki. Your journey has only begun.",
                duration = 4.0f
            },
            new DialogueLine
            {
                speaker = "DHARMA ARCHITECT",
                text = "Even a shattered sword can be reforged in the fires of discipline. Center your focus.",
                duration = 4.5f
            }
        }
    };

    [Header("Mission Failure Commentary")]
    [SerializeField] private List<DialogueLine> failureLines = new List<DialogueLine>
    {
        new DialogueLine
        {
            speaker = "DHARMA ARCHITECT",
            text = "The simulation collapsed. Clear your mind and re-engage.",
            duration = 3.5f
        }
    };

    /// <summary>
    /// Retrieves a dialogue line corresponding to the specified performance tier.
    /// </summary>
    public DialogueLine GetLineForTier(PerformanceTier tier)
    {
        StoryTierBranch branch = GetBranch(tier);
        if (branch != null && branch.dialogueLines != null && branch.dialogueLines.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, branch.dialogueLines.Count);
            return branch.dialogueLines[index];
        }

        // Fallback default
        return new DialogueLine
        {
            speaker = "DHARMA ARCHITECT",
            text = "Your journey unfolds, Kalki.",
            duration = 3.5f
        };
    }

    /// <summary>
    /// Retrieves the title of the specified performance tier.
    /// </summary>
    public string GetTierTitle(PerformanceTier tier)
    {
        StoryTierBranch branch = GetBranch(tier);
        return branch != null ? branch.tierTitle : tier.ToString();
    }

    /// <summary>
    /// Retrieves failure dialogue line.
    /// </summary>
    public DialogueLine GetFailureLine()
    {
        if (failureLines != null && failureLines.Count > 0)
        {
            return failureLines[UnityEngine.Random.Range(0, failureLines.Count)];
        }

        return new DialogueLine
        {
            speaker = "DHARMA ARCHITECT",
            text = "Re-align your focus, Kalki.",
            duration = 3.0f
        };
    }

    private StoryTierBranch GetBranch(PerformanceTier tier)
    {
        switch (tier)
        {
            case PerformanceTier.Mastery:
                return masteryTier;
            case PerformanceTier.Resilient:
                return resilientTier;
            case PerformanceTier.Struggling:
                return strugglingTier;
            default:
                return resilientTier;
        }
    }
}
