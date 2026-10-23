using System.Collections;
using UnityEngine;

/// <summary>
/// Camera Shake feedback component for KALKI: NEON DHARMA.
/// Provides impactful visual punch during hits and Power Strikes.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 originalLocalPos;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        originalLocalPos = transform.localPosition;
    }

    /// <summary>
    /// Triggers screen shake with customizable duration and intensity.
    /// Can be called statically from anywhere: CameraShake.TriggerShake(0.2f, 0.3f);
    /// </summary>
    public static void TriggerShake(float duration = 0.2f, float magnitude = 0.25f)
    {
        if (Instance != null)
        {
            Instance.StartShake(duration, magnitude);
        }
    }

    public void StartShake(float duration, float magnitude)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            transform.localPosition = originalLocalPos;
        }

        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Shake within 2.5D X/Y camera plane
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalLocalPos + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalLocalPos;
        shakeCoroutine = null;
    }
}
