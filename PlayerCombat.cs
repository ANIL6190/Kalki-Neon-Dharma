using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player Combat System for KALKI: NEON DHARMA.
/// Handles:
/// - Light/Basic Attack (J key or Left Mouse Button) with cooldown & combo chaining.
/// - Special Power Strike (K key) with high damage, knockback, and dedicated cooldown.
/// - Hit detection against MAYA (and enemies) using OverlapSphere with visual Gizmos.
/// - Animation triggers and combat feedback (audio, particle spawn, screen shake hooks).
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Basic Attack Settings (J / LMB)")]
    [Tooltip("Damage dealt per basic attack hit.")]
    [SerializeField] private float basicAttackDamage = 15f;

    [Tooltip("Minimum delay between basic attacks (seconds).")]
    [SerializeField] private float basicAttackCooldown = 0.35f;

    [Tooltip("Radius of the basic attack hitbox.")]
    [SerializeField] private float attackRange = 1.2f;

    [Tooltip("Knockback impulse applied to target.")]
    [SerializeField] private float basicAttackKnockback = 3f;

    [Header("Special Power Strike (K key)")]
    [Tooltip("Damage dealt by the Power Strike.")]
    [SerializeField] private float powerStrikeDamage = 40f;

    [Tooltip("Cooldown in seconds for Power Strike.")]
    [SerializeField] private float powerStrikeCooldown = 3.0f;

    [Tooltip("Larger radius of the Power Strike explosion/slash.")]
    [SerializeField] private float powerStrikeRange = 2.0f;

    [Tooltip("Higher knockback impulse applied to target.")]
    [SerializeField] private float powerStrikeKnockback = 8f;

    [Header("Hit Detection & Targeting")]
    [Tooltip("Origin point for melee hit detection. If empty, uses character position + forward offset.")]
    [SerializeField] private Transform attackPoint;

    [Tooltip("Layers representing damageable targets (e.g., Maya, Enemies).")]
    [SerializeField] private LayerMask enemyLayers;

    [Tooltip("Tags treated as valid enemy targets (e.g., 'MAYA', 'Enemy', 'TrainingTarget').")]
    [SerializeField] private string[] targetTags = new string[] { "MAYA", "Enemy", "TrainingTarget" };

    [Header("Combat Mechanics & Movement")]
    [Tooltip("Whether player movement pauses briefly during attack swings.")]
    [SerializeField] private bool lockMovementDuringAttack = true;

    [Tooltip("Duration of movement pause during basic attack.")]
    [SerializeField] private float attackMovementLockDuration = 0.25f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string basicAttackTrigger = "Attack";
    [SerializeField] private string powerStrikeTrigger = "PowerStrike";

    [Header("Visual & Audio Feedback")]
    [Tooltip("Particle effect spawned on successful melee hit.")]
    [SerializeField] private GameObject hitImpactVfxPrefab;

    [Tooltip("Particle effect spawned when unleashing Power Strike.")]
    [SerializeField] private GameObject powerStrikeVfxPrefab;

    [Tooltip("Audio clip for sword slash swing.")]
    [SerializeField] private AudioClip slashSound;

    [Tooltip("Audio clip for Power Strike impact.")]
    [SerializeField] private AudioClip powerStrikeSound;

    [SerializeField] private AudioSource audioSource;

    // Component references
    private PlayerMovement movement;

    // Timers
    private float nextBasicAttackTime = 0f;
    private float nextPowerStrikeTime = 0f;
    private bool isAttacking = false;

    // Public properties for UI displays (e.g., skill cooldowns)
    public float PowerStrikeCooldownRemaining => Mathf.Max(0f, nextPowerStrikeTime - Time.time);
    public float PowerStrikeCooldownMax => powerStrikeCooldown;
    public bool IsPowerStrikeReady => Time.time >= nextPowerStrikeTime;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void Update()
    {
        HandleCombatInput();
    }

    /// <summary>
    /// Checks for attack keys:
    /// Basic Attack: 'J' or Left Mouse Button (0)
    /// Power Strike: 'K'
    /// </summary>
    private void HandleCombatInput()
    {
        // 1. Basic Attack Input (J or Left Click)
        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
        {
            if (Time.time >= nextBasicAttackTime)
            {
                PerformBasicAttack();
            }
        }

        // 2. Power Strike Input (K)
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (Time.time >= nextPowerStrikeTime)
            {
                PerformPowerStrike();
            }
        }
    }

    /// <summary>
    /// Executes light sword strike.
    /// </summary>
    private void PerformBasicAttack()
    {
        nextBasicAttackTime = Time.time + basicAttackCooldown;

        // Record telemetry
        if (PlayerPerformanceTracker.Instance != null)
        {
            PlayerPerformanceTracker.Instance.RecordAttackFired(false);
        }

        // Trigger animation
        if (animator != null && !string.IsNullOrEmpty(basicAttackTrigger))
        {
            animator.SetTrigger(basicAttackTrigger);
        }

        // Play sound
        PlaySound(slashSound);

        // Optional momentary movement pause
        if (lockMovementDuringAttack && movement != null)
        {
            StartCoroutine(TemporaryMovementLock(attackMovementLockDuration));
        }

        // Perform hit detection
        Vector3 hitPoint = GetAttackPointPosition();
        DetectAndDamageEnemies(hitPoint, attackRange, basicAttackDamage, basicAttackKnockback, false);
    }

    /// <summary>
    /// Executes heavy Power Strike.
    /// </summary>
    private void PerformPowerStrike()
    {
        nextPowerStrikeTime = Time.time + powerStrikeCooldown;

        // Record telemetry
        if (PlayerPerformanceTracker.Instance != null)
        {
            PlayerPerformanceTracker.Instance.RecordAttackFired(true);
        }

        // Trigger animation
        if (animator != null && !string.IsNullOrEmpty(powerStrikeTrigger))
        {
            animator.SetTrigger(powerStrikeTrigger);
        }

        // Play sound
        PlaySound(powerStrikeSound != null ? powerStrikeSound : slashSound);

        // Spawn VFX
        Vector3 hitPoint = GetAttackPointPosition();
        if (powerStrikeVfxPrefab != null)
        {
            Instantiate(powerStrikeVfxPrefab, hitPoint, Quaternion.identity);
        }

        // Optional movement lock (slightly longer for heavy attack)
        if (lockMovementDuringAttack && movement != null)
        {
            StartCoroutine(TemporaryMovementLock(attackMovementLockDuration * 1.5f));
        }

        // Perform hit detection with higher damage and wider radius
        DetectAndDamageEnemies(hitPoint, powerStrikeRange, powerStrikeDamage, powerStrikeKnockback, true);

        // Screen Shake feedback if available
        CameraShake.TriggerShake(0.2f, 0.35f);
    }

    /// <summary>
    /// Detects targets in attack radius, validates tags/layers, applies damage and knockback.
    /// </summary>
    private void DetectAndDamageEnemies(Vector3 center, float radius, float damage, float knockback, bool isPowerStrike)
    {
        // 2.5D hit detection using OverlapSphere
        Collider[] hits;
        if (enemyLayers.value != 0)
        {
            hits = Physics.OverlapSphere(center, radius, enemyLayers);
        }
        else
        {
            hits = Physics.OverlapSphere(center, radius);
        }

        // Avoid damaging the same entity multiple times in a single swing
        HashSet<IDamageable> damagedEntities = new HashSet<IDamageable>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];

            // Don't hit self
            if (col.gameObject == gameObject || col.transform.IsChildOf(transform))
            {
                continue;
            }

            // Check if tagged properly (MAYA, Enemy)
            bool isTargetTag = false;
            for (int t = 0; t < targetTags.Length; t++)
            {
                if (col.CompareTag(targetTags[t]))
                {
                    isTargetTag = true;
                    break;
                }
            }

            // Look for IDamageable or Health component on the target or its parent
            IDamageable target = col.GetComponent<IDamageable>();
            if (target == null)
            {
                target = col.GetComponentInParent<IDamageable>();
            }

            // Apply damage if target is valid
            if (target != null && (isTargetTag || enemyLayers.value != 0) && !damagedEntities.Contains(target))
            {
                damagedEntities.Add(target);

                // Knockback direction away from player along the 2.5D X axis
                float directionX = (col.bounds.center.x - transform.position.x) >= 0 ? 1f : -1f;
                Vector3 hitDirection = new Vector3(directionX, 0.2f, 0f).normalized;

                target.TakeDamage(damage, hitDirection, knockback);

                // Spawn Hit Impact VFX
                if (hitImpactVfxPrefab != null)
                {
                    Instantiate(hitImpactVfxPrefab, col.bounds.center, Quaternion.identity);
                }

                Debug.Log($"[PlayerCombat] Hit {(col.gameObject.name)} for {damage} dmg! (PowerStrike: {isPowerStrike})");
            }
        }
    }

    private Vector3 GetAttackPointPosition()
    {
        if (attackPoint != null)
        {
            return attackPoint.position;
        }

        // If no attack point assigned, calculate dynamic point in front of facing direction
        float facingDirX = movement != null && movement.IsFacingRight ? 1f : -1f;
        return transform.position + new Vector3(facingDirX * 1.0f, 0.9f, 0f);
    }

    private IEnumerator TemporaryMovementLock(float duration)
    {
        movement.SetMovementEnabled(false);
        yield return new WaitForSeconds(duration);
        movement.SetMovementEnabled(true);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize Attack hitboxes in the Unity Scene View
        Vector3 center = GetAttackPointPosition();

        // Basic Attack Range (Yellow wireframe)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, attackRange);

        // Power Strike Range (Red wireframe)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(center, powerStrikeRange);
    }
}
