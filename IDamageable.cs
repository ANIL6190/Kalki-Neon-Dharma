using UnityEngine;

/// <summary>
/// Interface for any entity in KALKI: NEON DHARMA that can receive damage.
/// Implemented by Health.cs on both Kalki and Maya.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
    void TakeDamage(float amount, Vector3 hitDirection, float knockbackForce);
    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsDead { get; }
}
