using UnityEngine;

/// <summary>
/// Phase 4 ally hit-shield state. Applied by Shield Priestess; consumable via <see cref="TryConsumeHit"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class TowerShieldRuntime : MonoBehaviour
{
    private int hitsRemaining;
    private float refreshAt;
    private int sourceId;
    private Tower ownerTower;

    public bool HasShield => hitsRemaining > 0;
    public int HitsRemaining => Mathf.Max(0, hitsRemaining);
    public float RefreshAt => refreshAt;
    public int SourceId => sourceId;

    private void Awake()
    {
        ownerTower = GetComponent<Tower>();
    }

    public void ApplyOrRefresh(int hits, float refreshAtTime, int shieldSourceId)
    {
        hits = Mathf.Max(0, hits);
        if (hitsRemaining <= 0 || refreshAt <= Time.time)
        {
            hitsRemaining = hits;
            refreshAt = refreshAtTime;
        }

        sourceId = shieldSourceId;
    }

    public void ForceSet(int hits, float refreshAtTime, int shieldSourceId)
    {
        hitsRemaining = Mathf.Max(0, hits);
        refreshAt = refreshAtTime;
        sourceId = shieldSourceId;
    }

    /// <summary>Consumes one shield hit. Returns true if a hit was absorbed.</summary>
    public bool TryConsumeHit()
    {
        if (hitsRemaining <= 0)
            return false;

        hitsRemaining--;
        if (hitsRemaining <= 0 && ownerTower != null)
            ownerTower.RemoveDamageBuff(sourceId);

        return true;
    }

    public void Clear()
    {
        if (hitsRemaining > 0 && ownerTower != null)
            ownerTower.RemoveDamageBuff(sourceId);

        hitsRemaining = 0;
        refreshAt = 0f;
        sourceId = 0;
    }

    public static TowerShieldRuntime Ensure(Tower tower)
    {
        if (tower == null)
            return null;

        TowerShieldRuntime shield = tower.GetComponent<TowerShieldRuntime>();
        if (shield == null)
            shield = tower.gameObject.AddComponent<TowerShieldRuntime>();
        return shield;
    }
}
