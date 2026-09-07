#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// QA helpers for Phase 5 account-level L10/L20 gating.
/// Menu: Tools → Account → …
/// </summary>
public static class AccountProgressEditorMenu
{
    [MenuItem("Tools/Account/Log Current Level")]
    public static void LogCurrentLevel()
    {
        LogUnlockPreview(AccountProgressService.GetAccountLevel(), showDialog: true);
    }

    [MenuItem("Tools/Account/Set Level 1 (L10/L20 off)")]
    public static void SetLevel1()
    {
        ApplyLevel(1);
    }

    [MenuItem("Tools/Account/Set Level 10 (L10 on, L20 off)")]
    public static void SetLevel10()
    {
        ApplyLevel(10);
    }

    [MenuItem("Tools/Account/Set Level 20 (L10 + L20 on)")]
    public static void SetLevel20()
    {
        ApplyLevel(20);
    }

    [MenuItem("Tools/Account/Set Custom Level…")]
    public static void SetCustomLevel()
    {
        AccountLevelInputWizard.Open();
    }

    private static void ApplyLevel(int level)
    {
        AccountProgressService.SetAccountLevel(level);
        int saved = AccountProgressService.GetAccountLevel();
        LogUnlockPreview(saved, showDialog: true);
        Debug.Log("[Account] Saved ACCOUNT_LEVEL=" + saved +
                  ". Re-summon towers (or restart Play) so UnitAbilityRuntime rebinds.");
    }

    private static void LogUnlockPreview(int accountLevel, bool showDialog)
    {
        GameBalanceConfig balance = LoadBalance();
        bool force = balance != null && balance.debugForceAllAbilityTiers;
        int needL10 = balance != null ? balance.l10UnlockAccountLevel : 10;
        int needL20 = balance != null ? balance.l20UnlockAccountLevel : 20;

        bool l10 = force || accountLevel >= needL10;
        bool l20 = force || accountLevel >= needL20;

        string msg =
            "level=" + accountLevel +
            "  saved=" + AccountProgressService.HasSavedAccountLevel() +
            "  forceAll=" + force +
            "\n→ L10=" + (l10 ? "ON" : "OFF") +
            "  L20=" + (l20 ? "ON" : "OFF") +
            "  (need " + needL10 + " / " + needL20 + ")";

        Debug.Log("[Account] " + msg.Replace("\n", " "));
        if (showDialog)
            EditorUtility.DisplayDialog("Account Level", msg, "OK");
    }

    private static GameBalanceConfig LoadBalance()
    {
        GameConfigRegistry registry = AssetDatabase.LoadAssetAtPath<GameConfigRegistry>(
            "Assets/Content/Resources/GameConfigRegistry.asset");
        if (registry != null && registry.GameBalance != null)
            return registry.GameBalance;

        return AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(
            "Assets/Content/Balance/GameBalanceConfig.asset");
    }

    private sealed class AccountLevelInputWizard : ScriptableWizard
    {
        [SerializeField, Min(1)] private int accountLevel = 1;

        public static void Open()
        {
            AccountLevelInputWizard wizard = DisplayWizard<AccountLevelInputWizard>(
                "Set Account Level", "Apply");
            wizard.accountLevel = Mathf.Max(1, AccountProgressService.GetAccountLevel());
        }

        private void OnWizardCreate()
        {
            ApplyLevel(accountLevel);
        }
    }
}
#endif
