using System;
using UnityEngine;

public enum HubScreenId
{
    Battle = 0,
    Deck = 1
}

[Serializable]
public sealed class HubScreenEntry
{
    public HubScreenId id;
    public GameObject root;
    public string footerTabId;
}

/// <summary>
/// Shows exactly one hub screen root at a time under DesignRoot.
/// </summary>
[DisallowMultipleComponent]
public sealed class HubScreenNavigator : MonoBehaviour
{
    [SerializeField] private HubScreenEntry[] screens;
    [SerializeField] private HubScreenId defaultScreen = HubScreenId.Battle;

    private HubScreenId currentScreen;
    private bool initialized;

    public HubScreenId Current => currentScreen;
    public event Action<HubScreenId> ScreenChanged;

    private void Awake()
    {
        Show(defaultScreen, true);
    }

    public bool TryShow(HubScreenId id)
    {
        return Show(id, false);
    }

    public bool TryShowByFooterTabId(string tabId)
    {
        if (string.IsNullOrEmpty(tabId) || screens == null)
            return false;

        for (int i = 0; i < screens.Length; i++)
        {
            HubScreenEntry entry = screens[i];
            if (entry == null || entry.root == null)
                continue;

            if (string.Equals(entry.footerTabId, tabId, StringComparison.OrdinalIgnoreCase))
                return TryShow(entry.id);
        }

        return false;
    }

    public string GetFooterTabId(HubScreenId id)
    {
        if (screens == null)
            return null;

        for (int i = 0; i < screens.Length; i++)
        {
            HubScreenEntry entry = screens[i];
            if (entry != null && entry.id == id)
                return entry.footerTabId;
        }

        return null;
    }

    public GameObject GetScreenRoot(HubScreenId id)
    {
        if (screens == null)
            return null;

        for (int i = 0; i < screens.Length; i++)
        {
            HubScreenEntry entry = screens[i];
            if (entry != null && entry.id == id)
                return entry.root;
        }

        return null;
    }

#if UNITY_EDITOR
    public void EditorSetScreens(HubScreenEntry[] entries)
    {
        screens = entries;
    }
#endif

    private bool Show(HubScreenId id, bool force)
    {
        if (!force && initialized && currentScreen == id)
            return true;

        if (screens == null || screens.Length == 0)
            return false;

        bool found = false;
        for (int i = 0; i < screens.Length; i++)
        {
            HubScreenEntry entry = screens[i];
            if (entry == null || entry.root == null)
                continue;

            bool active = entry.id == id;
            if (entry.root.activeSelf != active)
                entry.root.SetActive(active);

            if (active)
                found = true;
        }

        if (!found)
            return false;

        currentScreen = id;
        initialized = true;
        ScreenChanged?.Invoke(id);
        return true;
    }
}
