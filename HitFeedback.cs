using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual feedback component attached to Kalki and MAYA.
/// Listens to Health events to flash renderers with a bright neon/damage color,
/// spawn hit particles, and play hit audio.
/// </summary>
[RequireComponent(typeof(Health))]
public class HitFeedback : MonoBehaviour
{
    [Header("Flash Settings")]
    [Tooltip("Color to flash when receiving damage.")]
    [SerializeField] private Color flashColor = new Color(1f, 0.2f, 0.2f, 1f); // Neon red

    [Tooltip("Duration of the hit flash in seconds.")]
    [SerializeField] private float flashDuration = 0.12f;

    [Header("Audio & Effects")]
    [SerializeField] private AudioClip hitHurtSound;
    [SerializeField] private AudioSource audioSource;

    private Health health;
    private Renderer[] renderers;
    private Dictionary<Renderer, Color[]> originalColors = new Dictionary<Renderer, Color[]>();
    private Coroutine flashCoroutine;

    private void Awake()
    {
        health = GetComponent<Health>();
        renderers = GetComponentsInChildren<Renderer>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        CacheOriginalColors();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDamageTaken.AddListener(OnDamageTaken);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamageTaken.RemoveListener(OnDamageTaken);
        }
    }

    private void CacheOriginalColors()
    {
        foreach (var rend in renderers)
        {
            if (rend == null) continue;
            Material[] mats = rend.materials;
            Color[] cols = new Color[mats.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i].HasProperty("_Color"))
                {
                    cols[i] = mats[i].color;
                }
                else if (mats[i].HasProperty("_BaseColor"))
                {
                    cols[i] = mats[i].GetColor("_BaseColor");
                }
            }
            originalColors[rend] = cols;
        }
    }

    private void OnDamageTaken(float damageAmount)
    {
        // Play hurt sound
        if (hitHurtSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitHurtSound);
        }

        // Trigger material flash
        if (gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        ApplyColor(flashColor);
        yield return new WaitForSeconds(flashDuration);
        RestoreColors();
        flashCoroutine = null;
    }

    private void ApplyColor(Color col)
    {
        foreach (var rend in renderers)
        {
            if (rend == null) continue;
            Material[] mats = rend.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i].HasProperty("_Color"))
                {
                    mats[i].color = col;
                }
                else if (mats[i].HasProperty("_BaseColor"))
                {
                    mats[i].SetColor("_BaseColor", col);
                }
            }
        }
    }

    private void RestoreColors()
    {
        foreach (var kvp in originalColors)
        {
            Renderer rend = kvp.Key;
            Color[] cols = kvp.Value;
            if (rend == null) continue;

            Material[] mats = rend.materials;
            for (int i = 0; i < mats.Length && i < cols.Length; i++)
            {
                if (mats[i].HasProperty("_Color"))
                {
                    mats[i].color = cols[i];
                }
                else if (mats[i].HasProperty("_BaseColor"))
                {
                    mats[i].SetColor("_BaseColor", cols[i]);
                }
            }
        }
    }
}
