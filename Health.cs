using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Health System for KALKI: NEON DHARMA.
/// Attached to both the player (Kalki) and enemies (MAYA).
/// Handles health tracking, damage intake, i-frames (invulnerability frames),
/// feedback events, and death notification to GameManager.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Health : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [Tooltip("Maximum health capacity.")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Starting health value. Usually equals Max Health.")]
    [SerializeField] private float currentHealth;

    [Header("Invulnerability (i-Frames)")]
    [Tooltip("Time in seconds character is immune to repeated damage after being hit.")]
    [SerializeField] private float invulnerabilityDuration = 0.5f;

    [Tooltip("Whether to flash character renderers during i-frames.")]
    [SerializeField] private bool flashOnInvulnerable = true;

    [Header("Knockback Settings")]
    [SerializeField] private bool canReceiveKnockback = true;
    [SerializeField] private float knockbackResistance = 0f; // 0 = full knockback, 1 = immune

    [Header("Animation")]
    [Tooltip("Animator reference. If left empty, will attempt GetComponentInChildren.")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hitTriggerName = "Hit";
    [SerializeField] private string deathTriggerName = "Die";

    [Header("Unity Events for UI & Audio")]
    public UnityEvent<float, float> OnHealthChanged; // (current, max)
    public UnityEvent<float> OnDamageTaken;          // (damageAmount)
    public UnityEvent OnDeath;

    // State flags
    private bool isDead = false;
    private bool isInvulnerable = false;
    private float invulnerabilityTimer = 0f;

    // Cached components
    private Rigidbody rb;
    private Renderer[] renderers;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        // Initial health event push for UI health bars
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        // Manage invulnerability timer
        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0f)
            {
                isInvulnerable = false;
                SetRenderersVisible(true);
            }
        }
    }

    /// <summary>
    /// Applies damage to the entity.
    /// Prevents repeated damage if entity is currently invulnerable or dead.
    /// </summary>
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, Vector3.zero, 0f);
    }

    /// <summary>
    /// Applies damage and directional knockback.
    /// </summary>
    public void TakeDamage(float amount, Vector3 hitDirection, float knockbackForce)
    {
        if (isDead || isInvulnerable)
        {
            return;
        }

        // Apply damage clamped between 0 and maxHealth
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageTaken?.Invoke(amount);

        // Apply knockback if available
        if (canReceiveKnockback && knockbackForce > 0f && rb != null && !rb.isKinematic)
        {
            float effectiveForce = knockbackForce * Mathf.Clamp01(1f - knockbackResistance);
            Vector3 knockDir = new Vector3(hitDirection.x, 0.35f, 0f).normalized; // 2.5D plane knockback
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f); // Reset horizontal momentum for crisp impact
            rb.AddForce(knockDir * effectiveForce, ForceMode.Impulse);
        }

        // Trigger hit animation if alive
        if (currentHealth > 0f)
        {
            if (animator != null && !string.IsNullOrEmpty(hitTriggerName))
            {
                animator.SetTrigger(hitTriggerName);
            }

            // Start i-frames to prevent multiple hits from one single attack swing
            TriggerInvulnerability(invulnerabilityDuration);
        }
        else
        {
            Die();
        }
    }

    /// <summary>
    /// Heals the character by specified amount up to max health.
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Activates temporary invulnerability and optional flicker.
    /// </summary>
    public void TriggerInvulnerability(float duration)
    {
        if (duration <= 0f) return;
        isInvulnerable = true;
        invulnerabilityTimer = duration;

        if (flashOnInvulnerable && gameObject.activeInHierarchy)
        {
            StartCoroutine(FlickerRoutine(duration));
        }
    }

    private IEnumerator FlickerRoutine(float duration)
    {
        float interval = 0.08f;
        float elapsed = 0f;
        bool visible = true;

        while (elapsed < duration && isInvulnerable)
        {
            visible = !visible;
            SetRenderersVisible(visible);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        SetRenderersVisible(true);
    }

    private void SetRenderersVisible(bool visible)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = visible;
            }
        }
    }

    /// <summary>
    /// Handles entity death, disables controls, triggers death animation,
    /// and alerts the GameManager.
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Trigger Death Animation
        if (animator != null && !string.IsNullOrEmpty(deathTriggerName))
        {
            animator.SetTrigger(deathTriggerName);
        }

        // Disable control scripts on death
        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        var combat = GetComponent<PlayerCombat>();
        if (combat != null) combat.enabled = false;

        // Fire local Death Event
        OnDeath?.Invoke();

        // Notify GameManager (Singleton instance or fallback finder)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCharacterDeath(gameObject);
        }
        else
        {
            Debug.Log($"[Health] Character '{gameObject.name}' has fallen! (No active GameManager found in scene)");
        }
    }
}
