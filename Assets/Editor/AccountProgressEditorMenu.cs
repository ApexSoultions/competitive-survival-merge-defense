#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Legacy account-level QA (ability unlocks are now per-unit — use Tools → Units → Set Unit Level…).
/// Menu: Tools → Account → …
/// </summary>
public static class AccountProgressEditorMenu
{
    [MenuItem("Tools/Account/Log Current Level (legacy)")]
    public static void LogCurrentLevel()
    {
        LogUnlockPreview(AccountProgressService.GetAccountLevel(), showDialog: true);
    }

    [MenuItem("Tools/Account/Set Level 1 (legacy — gating is per-unit now)")]
    public static void SetLevel1()
    {
        ApplyLevel(1);
    }

    [MenuItem("Tools/Account/Set Level 10 (legacy — gating is per-unit now)")]
    public static void SetLevel10()
    {
        ApplyLevel(10);
    }

    [MenuItem("Tools/Account/Set Level 20 (legacy — gating is per-unit now)")]
    public static void SetLevel20()
    {
        ApplyLevel(20);
    }

    [MenuItem("Tools/Account/Set Custom Level… (legacy)")]
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
                  ". NOTE: UnitAbilityRuntime now gates by UnitProgressService (Tools → Units → Set Unit Level…).");
    }

    private static void LogUnlockPreview(int accountLevel, bool showDialog)
    {
        string msg =
            "LEGACY account level=" + accountLevel +
            "\nAbility unlocks are per-unit now." +
            "\nUse Tools → Units → Set Unit Level… / Set All Units Level 20.";

        Debug.Log("[Account] " + msg.Replace("\n", " "));
        if (showDialog)
            EditorUtility.DisplayDialog("Account Level (Legacy)", msg, "OK");
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
