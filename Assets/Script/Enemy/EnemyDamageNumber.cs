using UnityEngine;

/// <summary>
/// Compatibility bridge for older feedback callers. New code uses <see cref="FloatingDamagePool"/> directly.
/// </summary>
public static class EnemyDamageNumber
{
    public static void Spawn(Vector3 position, float damage, EnemyDamageType damageType, EnemyCombatFeedbackTheme theme, bool isBoss)
    {
        if (FloatingDamagePool.Instance != null)
            FloatingDamagePool.Instance.Show(position, damage, damageType, theme, isBoss);
    }
}
