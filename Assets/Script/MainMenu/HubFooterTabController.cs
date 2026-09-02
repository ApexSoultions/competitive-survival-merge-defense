using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Footer tab sprite swap: per-tab Selected/Unselected sprites + draw order.
/// Raises <see cref="TabClicked"/> for hub navigation.
/// </summary>
[DisallowMultipleComponent]
public sealed class HubFooterTabController : MonoBehaviour
{
    [Serializable]
    public sealed class TabEntry
    {
        public string id;
        public RectTransform root;
        public Button button;
        public Image backgroundImage;
        [Tooltip("Optional per-tab selected sprite. Falls back to controller default.")]
        public Sprite selectedSprite;
        [Tooltip("Optional per-tab unselected sprite. Falls back to controller default.")]
        public Sprite unselectedSprite;
    }

    [Header("Sprites (fallback when a tab has no per-tab sprite)")]
    [SerializeField] private Sprite selectedTabSprite;
    [SerializeField] private Sprite unselectedTabSprite;

    [Header("Tabs (Shop, Team, Battle, Clan, Event)")]
    [SerializeField] private TabEntry[] tabs;

    [Header("Defaults")]
    [SerializeField] private string defaultSelectedId = "Battle";
    [SerializeField] private bool autoResolveFromFooter = true;
    [Tooltip("After swapping Selected/Unselected sprites, match Image size to sprite pixels.")]
    [SerializeField] private bool setNativeSizeOnChange = true;
    [Tooltip("Keep equal horizontal slots so changing native size does not create uneven gaps.")]
    [SerializeField] private bool evenHorizontalLayout = true;
    [SerializeField] private float layoutSidePadding = 8f;
    [SerializeField] private float layoutBottomPadding = 0f;

    private readonly System.Collections.Generic.List<(Button button, UnityEngine.Events.UnityAction action)> bound =
        new System.Collections.Generic.List<(Button, UnityEngine.Events.UnityAction)>(8);

    private int selectedIndex = -1;
    private bool wired;
    private bool layoutGroupDisabled;
    private RectTransform layoutParent;
    private float lastAppliedParentWidth = -1f;
    private Coroutine deferredLayoutCoroutine;
    private bool initialLayoutApplied;

    public int SelectedIndex => selectedIndex;
    public event Action<string> TabClicked;

    private void Awake()
    {
        EnsureSpritesFromHubUi();
        if (autoResolveFromFooter)
            TryAutoResolveTabs();

        ResolveMissingRefs();
        ApplyDefaultTabSpritesFromHubUi();
        Wire();
        SelectDefaultSilently();
        ScheduleDeferredLayoutRefresh();
    }

    private void OnEnable()
    {
        if (initialLayoutApplied)
            ScheduleDeferredLayoutRefresh();
    }

    private void LateUpdate()
    {
        if (!evenHorizontalLayout || selectedIndex < 0)
            return;

        EnsureLayoutParentCached();
        if (layoutParent == null)
            return;

        float width = ResolveLayoutParentWidth(layoutParent);
        if (width <= 1f || Mathf.Approximately(width, lastAppliedParentWidth))
            return;

        ReflowEvenHorizontalLayout();
    }

    private void EnsureSpritesFromHubUi()
    {
        if (selectedTabSprite != null && unselectedTabSprite != null)
            return;

        HubUiSprites hub = Resources.Load<HubUiSprites>("HubUiSprites");
        if (hub == null)
            return;

        if (selectedTabSprite == null)
            selectedTabSprite = hub.footerSelectedTab;
        if (unselectedTabSprite == null)
            unselectedTabSprite = hub.footerUnselectedTab;
    }

    private void ApplyDefaultTabSpritesFromHubUi()
    {
        if (tabs == null || tabs.Length == 0)
            return;

        HubUiSprites hub = Resources.Load<HubUiSprites>("HubUiSprites");
        if (hub == null)
            return;

        for (int i = 0; i < tabs.Length; i++)
        {
            TabEntry tab = tabs[i];
            if (tab == null)
                continue;

            if (tab.selectedSprite == null)
                tab.selectedSprite = hub.footerSelectedTab;
            if (tab.unselectedSprite == null)
                tab.unselectedSprite = hub.GetFooterUnselectedForTab(tab.id);
        }
    }

    private Sprite ResolveSprite(TabEntry tab, bool selected)
    {
        if (tab != null)
        {
            Sprite perTab = selected ? tab.selectedSprite : tab.unselectedSprite;
            if (perTab != null)
                return perTab;
        }

        return selected ? selectedTabSprite : unselectedTabSprite;
    }

    private void OnDestroy()
    {
        if (deferredLayoutCoroutine != null)
        {
            StopCoroutine(deferredLayoutCoroutine);
            deferredLayoutCoroutine = null;
        }

        Unwire();
    }

    public void SelectTab(int index)
    {
        if (!SelectTabSilently(index))
            return;

        RaiseTabClicked(index);
    }

    public bool SelectTabSilently(int index)
    {
        if (tabs == null || tabs.Length == 0)
            return false;
        if (index < 0 || index >= tabs.Length)
            return false;

        selectedIndex = index;
        ApplySpritesAndOrder();
        return true;
    }

    public void SelectTabById(string id)
    {
        SelectTabSilentlyById(id);
    }

    public bool SelectTabSilentlyById(string id)
    {
        if (tabs == null || string.IsNullOrEmpty(id))
            return false;

        for (int i = 0; i < tabs.Length; i++)
        {
            TabEntry tab = tabs[i];
            if (tab == null)
                continue;
            if (string.Equals(tab.id, id, StringComparison.OrdinalIgnoreCase) ||
                (tab.root != null && string.Equals(tab.root.name, id, StringComparison.OrdinalIgnoreCase)))
            {
                return SelectTabSilently(i);
            }
        }

        return false;
    }

    private void RaiseTabClicked(int index)
    {
        if (tabs == null || index < 0 || index >= tabs.Length)
            return;

        TabEntry tab = tabs[index];
        if (tab == null)
            return;

        string id = string.IsNullOrEmpty(tab.id)
            ? (tab.root != null ? tab.root.name : null)
            : tab.id;

        if (!string.IsNullOrEmpty(id))
            TabClicked?.Invoke(id);
    }

    private void SelectDefaultSilently()
    {
        if (!string.IsNullOrEmpty(defaultSelectedId))
        {
            if (SelectTabSilentlyById(defaultSelectedId))
                return;
        }

        if (tabs == null)
            return;

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] != null && tabs[i].root != null)
            {
                SelectTabSilently(i);
                return;
            }
        }
    }

    private void ApplySpritesAndOrder()
    {
        if (tabs == null)
            return;

        for (int i = 0; i < tabs.Length; i++)
        {
            TabEntry tab = tabs[i];
            if (tab == null)
                continue;

            Image bg = tab.backgroundImage;
            if (bg == null && tab.root != null)
                bg = tab.root.GetComponent<Image>();

            if (bg != null)
            {
                bool selected = i == selectedIndex;
                Sprite sprite = ResolveSprite(tab, selected);
                if (sprite != null)
                {
                    bg.sprite = sprite;
                    bg.type = Image.Type.Simple;
                    bg.preserveAspect = true;
                    if (setNativeSizeOnChange)
                        bg.SetNativeSize();
                }

                bg.enabled = true;
                bg.color = Color.white;
                tab.backgroundImage = bg;
            }
        }

        if (evenHorizontalLayout)
        {
            if (!ReflowEvenHorizontalLayout())
                ScheduleDeferredLayoutRefresh();
        }

        if (selectedIndex >= 0 && selectedIndex < tabs.Length)
        {
            TabEntry selected = tabs[selectedIndex];
            if (selected != null && selected.root != null)
                selected.root.SetAsLastSibling();
        }
    }

    /// <summary>
    /// Places each tab in an equal-width slot so native-size changes never open uneven gaps.
    /// Tabs are bottom-aligned so the taller Selected_Tab grows upward.
    /// </summary>
    private bool ReflowEvenHorizontalLayout()
    {
        RectTransform parent = null;
        int count = 0;
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null || tabs[i].root == null)
                continue;
            count++;
            if (parent == null)
                parent = tabs[i].root.parent as RectTransform;
        }

        if (parent == null || count <= 0)
            return false;

        layoutParent = parent;
        DisableConflictingLayout(parent);

        float parentWidth = ResolveLayoutParentWidth(parent);
        if (parentWidth <= 1f)
            return false;

        float usable = Mathf.Max(1f, parentWidth - layoutSidePadding * 2f);
        float slot = usable / count;
        float originX = -parentWidth * 0.5f + layoutSidePadding + slot * 0.5f;

        // Parent may use center or bottom pivot; convert slot centers into local X under parent's pivot.
        float parentPivotOffsetX = (0.5f - parent.pivot.x) * parentWidth;

        int slotIndex = 0;
        for (int i = 0; i < tabs.Length; i++)
        {
            TabEntry tab = tabs[i];
            if (tab == null || tab.root == null)
                continue;

            RectTransform root = tab.root;
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);

            float x = originX + slot * slotIndex + parentPivotOffsetX;
            root.anchoredPosition = new Vector2(x, layoutBottomPadding);
            slotIndex++;
        }

        lastAppliedParentWidth = parentWidth;
        initialLayoutApplied = true;
        return true;
    }

    private void EnsureLayoutParentCached()
    {
        if (layoutParent != null || tabs == null)
            return;

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null || tabs[i].root == null)
                continue;
            layoutParent = tabs[i].root.parent as RectTransform;
            return;
        }
    }

    private static float ResolveLayoutParentWidth(RectTransform parent)
    {
        if (parent == null)
            return 0f;

        Canvas.ForceUpdateCanvases();

        RectTransform current = parent;
        while (current != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(current);
            float width = current.rect.width;
            if (width > 1f)
                return width;

            current = current.parent as RectTransform;
        }

        return 0f;
    }

    private void ScheduleDeferredLayoutRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (deferredLayoutCoroutine != null)
            StopCoroutine(deferredLayoutCoroutine);

        deferredLayoutCoroutine = StartCoroutine(DeferredLayoutRefresh());
    }

    private IEnumerator DeferredLayoutRefresh()
    {
        // CanvasScaler, safe area, and bottom-bleed run after Awake on many resolutions.
        const int maxFrames = 4;
        for (int frame = 0; frame < maxFrames; frame++)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            if (selectedIndex < 0)
                continue;

            if (evenHorizontalLayout && ReflowEvenHorizontalLayout())
                break;
        }

        deferredLayoutCoroutine = null;
    }

    private void DisableConflictingLayout(RectTransform parent)
    {
        if (layoutGroupDisabled || parent == null)
            return;

        HorizontalLayoutGroup hlg = parent.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null && hlg.enabled)
            hlg.enabled = false;

        VerticalLayoutGroup vlg = parent.GetComponent<VerticalLayoutGroup>();
        if (vlg != null && vlg.enabled)
            vlg.enabled = false;

        GridLayoutGroup glg = parent.GetComponent<GridLayoutGroup>();
        if (glg != null && glg.enabled)
            glg.enabled = false;

        ContentSizeFitter fitter = parent.GetComponent<ContentSizeFitter>();
        if (fitter != null && fitter.enabled)
            fitter.enabled = false;

        layoutGroupDisabled = true;
    }

    private void Wire()
    {
        if (wired || tabs == null)
            return;
        wired = true;

        for (int i = 0; i < tabs.Length; i++)
        {
            TabEntry tab = tabs[i];
            if (tab == null)
                continue;

            Button button = tab.button;
            if (button == null && tab.root != null)
                button = tab.root.GetComponent<Button>();
            if (button == null && tab.root != null)
                button = tab.root.gameObject.AddComponent<Button>();

            tab.button = button;
            if (button == null)
                continue;

            if (tab.backgroundImage == null && tab.root != null)
                tab.backgroundImage = tab.root.GetComponent<Image>();

            if (button.targetGraphic == null && tab.backgroundImage != null)
                button.targetGraphic = tab.backgroundImage;

            int index = i;
            UnityEngine.Events.UnityAction action = () => SelectTab(index);
            button.onClick.AddListener(action);
            bound.Add((button, action));
        }
    }

    private void Unwire()
    {
        if (!wired)
            return;
        wired = false;

        for (int i = 0; i < bound.Count; i++)
        {
            (Button button, UnityEngine.Events.UnityAction action) = bound[i];
            if (button != null)
                button.onClick.RemoveListener(action);
        }

        bound.Clear();
    }

    private void ResolveMissingRefs()
    {
        if (tabs == null)
            return;

        for (int i = 0; i < tabs.Length; i++)
        {
            TabEntry tab = tabs[i];
            if (tab == null)
                continue;

            if (tab.root != null)
            {
                if (string.IsNullOrEmpty(tab.id))
                    tab.id = tab.root.name;
                if (tab.button == null)
                    tab.button = tab.root.GetComponent<Button>();
                if (tab.backgroundImage == null)
                    tab.backgroundImage = tab.root.GetComponent<Image>();
            }
        }
    }

    /// <summary>
    /// Finds Footer under Canvas_MainMenu and binds Shop/Team/Battle/Clan/Event by name.
    /// </summary>
    public void TryAutoResolveTabs()
    {
        if (tabs != null && tabs.Length > 0)
        {
            bool any = false;
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] != null && tabs[i].root != null)
                {
                    any = true;
                    break;
                }
            }

            if (any)
                return;
        }

        Transform footer = FindFooterTransform();
        if (footer == null)
            return;

        Transform container = footer.Find("BackGround/Tabs");
        if (container == null)
            container = footer.Find("Tabs");
        if (container == null)
            container = footer.Find("BackGround");
        if (container == null)
            container = footer;

        string[] order = { "Shop", "Team", "Battle", "Clan", "Event" };
        var list = new System.Collections.Generic.List<TabEntry>(order.Length);
        for (int i = 0; i < order.Length; i++)
        {
            Transform tabTransform = FindChildRecursive(container, order[i]);
            if (tabTransform == null)
                continue;

            list.Add(new TabEntry
            {
                id = order[i],
                root = tabTransform as RectTransform,
                button = tabTransform.GetComponent<Button>(),
                backgroundImage = tabTransform.GetComponent<Image>()
            });
        }

        if (list.Count > 0)
            tabs = list.ToArray();
    }

    private static Transform FindFooterTransform()
    {
        // Prefer Canvas_MainMenu hierarchy
        GameObject canvas = GameObject.Find("Canvas_MainMenu");
        if (canvas != null)
        {
            Transform footer = FindChildRecursive(canvas.transform, "Footer");
            if (footer != null)
                return footer;
        }

        return FindChildRecursive(null, "Footer");
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null)
        {
            // Search loaded scenes
            for (int s = 0; s < UnityEngine.SceneManagement.SceneManager.sceneCount; s++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(s);
                if (!scene.isLoaded)
                    continue;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    Transform found = FindChildRecursive(roots[i].transform, name);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

#if UNITY_EDITOR
    public void EditorAssignSprites(Sprite selected, Sprite unselected)
    {
        selectedTabSprite = selected;
        unselectedTabSprite = unselected;
    }

    public void EditorSetTabs(TabEntry[] entries)
    {
        tabs = entries;
    }
#endif
}
