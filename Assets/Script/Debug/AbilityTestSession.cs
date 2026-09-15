using Game.Core.Save;
using UnityEngine;

/// <summary>
/// Runtime overrides for client ability QA (DEV panel). Does not edit the balance asset permanently.
/// </summary>
public static class AbilityTestSession
{
    /// <summary>
    /// Effective force-all: session override if set, otherwise <see cref="GameBalanceConfig.debugForceAllAbilityTiers"/>.
    /// </summary>
    public static bool GetForceAllAbilityTiers(GameBalanceConfig balance)
    {
        ISaveService save = ResolveSave();
        if (save.HasKey(SaveKeys.AbilityTestForceAllOverride))
            return save.LoadInt(SaveKeys.AbilityTestForceAllOverride, 0) != 0;

        return balance != null && balance.debugForceAllAbilityTiers;
    }

    public static bool HasForceAllOverride()
    {
        return ResolveSave().HasKey(SaveKeys.AbilityTestForceAllOverride);
    }

    public static void SetForceAllAbilityTiers(bool forceAll)
    {
        ISaveService save = ResolveSave();
        save.SaveInt(SaveKeys.AbilityTestForceAllOverride, forceAll ? 1 : 0);
        save.Save();
    }

    public static void ClearForceAllOverride()
    {
        ISaveService save = ResolveSave();
        if (!save.HasKey(SaveKeys.AbilityTestForceAllOverride))
            return;

        save.DeleteKey(SaveKeys.AbilityTestForceAllOverride);
        save.Save();
    }

    private static ISaveService ResolveSave()
    {
        if (GameServices.Instance != null)
            return GameServices.Instance.Save;
        return new PlayerPrefsSaveService();
    }
}
