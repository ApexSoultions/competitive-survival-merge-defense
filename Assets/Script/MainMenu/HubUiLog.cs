using UnityEngine;

/// <summary>
/// Hub UI logging — silent in player builds unless HubUiLog.Enabled is set.
/// </summary>
public static class HubUiLog
{
#if UNITY_EDITOR
    public static bool Enabled = true;
#else
    public static bool Enabled = false;
#endif

    public static void Info(string message)
    {
        if (Enabled)
            Debug.Log(message);
    }

    public static void Warn(string message)
    {
        if (Enabled)
            Debug.LogWarning(message);
    }
}
