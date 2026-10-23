using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Health Bar UI Controller for KALKI: NEON DHARMA.
/// Can be bound to Slider or Image fill, smoothly reflecting health changes.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("Target Entity")]
    [Tooltip("Health component to track.")]
    [SerializeField] private Health targetHealth;

    [Header("UI References")]
    [Tooltip("Optional UI Slider.")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("Optional filled UI Image (for neon fill bars).")]
    [SerializeField] private Image healthFillImage;

    [Header("Smooth Transition")]
    [SerializeField] private bool smoothTransition = true;
    [SerializeField] private float smoothSpeed = 8f;

    private float targetFillRatio = 1f;

    private void Start()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged.AddListener(UpdateHealthUI);
            UpdateHealthUI(targetHealth.CurrentHealth, targetHealth.MaxHealth);
        }
    }

    private void Update()
    {
        if (smoothTransition)
        {
            if (healthSlider != null)
            {
                healthSlider.value = Mathf.Lerp(healthSlider.value, targetFillRatio, smoothSpeed * Time.deltaTime);
            }
            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, targetFillRatio, smoothSpeed * Time.deltaTime);
            }
        }
    }

    public void UpdateHealthUI(float current, float max)
    {
        if (max <= 0f) return;
        targetFillRatio = Mathf.Clamp01(current / max);

        if (!smoothTransition)
        {
            if (healthSlider != null) healthSlider.value = targetFillRatio;
            if (healthFillImage != null) healthFillImage.fillAmount = targetFillRatio;
        }
    }
}
