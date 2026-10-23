using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Structured Telemetry Data Container for KALKI: NEON DHARMA.
/// Encapsulates player combat performance metrics for research evaluation,
/// AI model feature input, and persistent CSV/JSON logging.
///
/// UNITY SETUP INSTRUCTIONS:
/// - This is a pure data class and utility container (does not need to be attached to a GameObject).
/// - PlayerPerformanceTracker, AdaptiveStoryDirector, and AIStoryEvaluator use this for data transfer.
/// - Metrics can be exported to Application.persistentDataPath for research logging.
/// </summary>
[System.Serializable]
public class TrainingMetrics
{
    public string timestampUtc;
    public int attemptNumber;
    public int consecutiveFailures;
    public bool missionSuccess;
    public float completionTimeSeconds;
    public int totalAttacks;
    public int hitsLanded;
    public float accuracyPercent;
    public float damageDealt;
    public float damageTaken;
    public int targetsEliminated;
    public int totalTargets;
    public PerformanceTier evaluatedTier;
    public string outcomeNotes;

    /// <summary>
    /// Serializes metrics object to JSON string.
    /// </summary>
    public string ToJson(bool prettyPrint = true)
    {
        return JsonUtility.ToJson(this, prettyPrint);
    }

    /// <summary>
    /// Deserializes metrics from JSON string.
    /// </summary>
    public static TrainingMetrics FromJson(string json)
    {
        return JsonUtility.FromJson<TrainingMetrics>(json);
    }

    /// <summary>
    /// Returns standardized CSV header string for research data logging.
    /// </summary>
    public static string ToCsvHeader()
    {
        return "TimestampUtc,AttemptNumber,ConsecutiveFailures,MissionSuccess,CompletionTimeSeconds,TotalAttacks,HitsLanded,AccuracyPercent,DamageDealt,DamageTaken,TargetsEliminated,TotalTargets,EvaluatedTier,OutcomeNotes";
    }

    /// <summary>
    /// Formats current metrics as a single CSV row.
    /// </summary>
    public string ToCsvRow()
    {
        return $"{timestampUtc},{attemptNumber},{consecutiveFailures},{missionSuccess},{completionTimeSeconds:F2},{totalAttacks},{hitsLanded},{accuracyPercent:F1},{damageDealt:F1},{damageTaken:F1},{targetsEliminated},{totalTargets},{evaluatedTier},\"{outcomeNotes}\"";
    }

    /// <summary>
    /// Appends the metrics row to a persistent CSV log file for research dataset recording.
    /// </summary>
    public void SaveToPersistentCsv(string fileName = "kalki_performance_log.csv")
    {
        try
        {
            string folderPath = Application.persistentDataPath;
            string filePath = Path.Combine(folderPath, fileName);

            bool fileExists = File.Exists(filePath);
            using (StreamWriter sw = new StreamWriter(filePath, append: true))
            {
                if (!fileExists)
                {
                    sw.WriteLine(ToCsvHeader());
                }
                sw.WriteLine(ToCsvRow());
            }

            Debug.Log($"[TrainingMetrics] Research telemetry saved to: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrainingMetrics] Failed to write CSV telemetry: {ex.Message}");
        }
    }
}
