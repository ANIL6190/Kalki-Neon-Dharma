using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Training Target Dummy for KALKI: NEON DHARMA.
/// Implements IDamageable to receive damage from PlayerCombat attacks.
/// Provides procedural hit reaction (neon flash, hit scale punch, sound, particles),
/// and notifies TrainingMissionManager and PlayerPerformanceTracker upon destruction.
///
/// UNITY SETUP INSTRUCTIONS:
/// 1. Create a 3D Dummy/Target GameObject (e.g. cylinder, mannequin, or bot mesh).
/// 2. Tag the GameObject as "Enemy" (so PlayerCombat's hit detection registers it).
/// 3. Add a Collider (CapsuleCollider or BoxCollider).
/// 4. Add this TrainingTarget component.
/// 5. (Optional) Assign Hit Particles, Destruction VFX, Audio Clips, and Mesh Renderer.
///
/// DEPENDENCIES:
/// - IDamageable.cs
/// - PlayerCombat.cs (inflicts damage)
/// - TrainingMissionManager.cs (receives destruction event)
/// - PlayerPerformanceTracker.cs (records target hit metrics)
///
/// TEST CASES:
/// - Basic Attack (J / Left Click): Target takes damage, flashes neon-cyan/red, shakes momentarily.
/// - Power Strike (K): Target takes heavy damage, knocks back slightly, triggers larger hit feedback.
/// - Health reaches 0: Target spawns destruction VFX, plays break sound, hides, and notifies Mission Manager.
/// - Reset: Target can be restored during mission restarts via ResetTarget().
/// </summary>
[RequireComponent(typeof(Collider))]
public class TrainingTarget : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [Tooltip("Maximum hit points of the training dummy.")]
    [SerializeField] private float maxHealth = 50f;

    [Tooltip("Current hit points.")]
    [SerializeField] private float currentHealth;

    [Header("Visual Feedback & Mesh")]
    [Tooltip("Target mesh renderer to flash when struck.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("Neon flash color upon taking damage.")]
    [SerializeField] private Color flashColor = new Color(0.1f, 1f, 1f); // Neon Cyan

    [Tooltip("Duration of hit flash in seconds.")]
    [SerializeField] private float flashDuration = 0.12f;

    [Tooltip("Scale punch intensity when struck.")]
    [SerializeField] private float punchScaleFactor = 1.15f;

    [Header("Audio & VFX")]
    [Tooltip("Particle effect spawned on hit impact.")]
    [SerializeField] private GameObject hitVfxPrefab;

    [Tooltip("Particle effect or explosion spawned when destroyed.")]
    [SerializeField] private GameObject destroyVfxPrefab;

    [Tooltip("Audio clip played when struck.")]
    [SerializeField] private AudioClip hitSound;

    [Tooltip("Audio clip played upon target destruction.")]
    [SerializeField] private AudioClip destroySound;

    [Header("Respawn / Persistence")]
    [Tooltip("Disable GameObject on destruction instead of Destroy() to allow mission restart.")]
    [SerializeField] private bool disableOnDeath = true;

    // Events
    public event Action<TrainingTarget> OnDestroyedEvent;
    public event Action<float> OnDamagedEvent;

    // IDamageable Properties
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    // Internal State
    private bool isDead = false;
    private Vector3 originalScale;
    private Material targetMaterial;
    private Color originalMaterialColor;
    private AudioSource audioSource;
    private Collider targetCollider;

    private void Awake()
    {
        currentHealth = maxHealth;
        originalScale = transform.localScale;
        targetCollider = GetComponent<Collider>();

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (targetRenderer != null)
        {
            targetMaterial = targetRenderer.material;
            if (targetMaterial.HasProperty("_Color"))
            {
                originalMaterialColor = targetMaterial.color;
            }
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, Vector3.zero, 0f);
    }

    public void TakeDamage(float amount, Vector3 hitDirection, float knockbackForce)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnDamagedEvent?.Invoke(amount);

        // Notify performance tracker
        if (PlayerPerformanceTracker.Instance != null)
        {
            PlayerPerformanceTracker.Instance.RecordTargetHit(amount);
        }

        // Hit Audio
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // Hit VFX
        if (hitVfxPrefab != null)
        {
            Instantiate(hitVfxPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
        }

        // Procedural Flash & Shake Animation
        StartCoroutine(HitFeedbackRoutine());

        // Check Death
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private IEnumerator HitFeedbackRoutine()
    {
        // 1. Material Flash
        if (targetMaterial != null && targetMaterial.HasProperty("_Color"))
        {
            targetMaterial.color = flashColor;
        }

        // 2. Punch Scale
        transform.localScale = originalScale * punchScaleFactor;

        float timer = 0f;
        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            float t = timer / flashDuration;
            transform.localScale = Vector3.Lerp(originalScale * punchScaleFactor, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;

        if (targetMaterial != null && targetMaterial.HasProperty("_Color"))
        {
            targetMaterial.color = originalMaterialColor;
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[TrainingTarget] Target '{gameObject.name}' destroyed!");

        // Death VFX
        if (destroyVfxPrefab != null)
        {
            Instantiate(destroyVfxPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
        }

        // Death Sound
        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, transform.position);
        }

        OnDestroyedEvent?.Invoke(this);

        if (disableOnDeath)
        {
            if (targetCollider != null) targetCollider.enabled = false;
            if (targetRenderer != null) targetRenderer.enabled = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Resets the target to full health and active visibility for mission restarts.
    /// </summary>
    public void ResetTarget()
    {
        isDead = false;
        currentHealth = maxHealth;
        transform.localScale = originalScale;

        if (targetCollider != null) targetCollider.enabled = true;
        if (targetRenderer != null) targetRenderer.enabled = true;

        if (targetMaterial != null && targetMaterial.HasProperty("_Color"))
        {
            targetMaterial.color = originalMaterialColor;
        }
    }
}
