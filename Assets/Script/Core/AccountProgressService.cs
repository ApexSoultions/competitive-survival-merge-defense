using Game.Core.Save;
using UnityEngine;

/// <summary>
/// Persisted account level for Phase 5 L10 / L20 ability gating.
/// Thresholds live on <see cref="GameBalanceConfig"/>; this service only stores the level.
/// </summary>
public static class AccountProgressService
{
    public const int DefaultAccountLevel = 1;
    public const int MinAccountLevel = 1;

    /// <summary>Current account level (clamped, defaults to 1 when unset).</summary>
    public static int GetAccountLevel()
    {
        ISaveService save = ResolveSave();
        int level = save.LoadInt(SaveKeys.AccountLevel, DefaultAccountLevel);
        return Mathf.Max(MinAccountLevel, level);
    }

    /// <summary>Persists account level (minimum 1) and flushes save.</summary>
    public static void SetAccountLevel(int level)
    {
        ISaveService save = ResolveSave();
        int clamped = Mathf.Max(MinAccountLevel, level);
        save.SaveInt(SaveKeys.AccountLevel, clamped);
        save.Save();
    }

    public static bool HasSavedAccountLevel()
    {
        return ResolveSave().HasKey(SaveKeys.AccountLevel);
    }

    private static ISaveService ResolveSave()
    {
        if (GameServices.Instance != null)
            return GameServices.Instance.Save;

        // Editor / early boot without Bootstrap — still use the same PlayerPrefs backend.
        return new PlayerPrefsSaveService();
    }
}
