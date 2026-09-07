using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class HeroAbilityButtonController : MonoBehaviour
{
    [Header("Option A — Product lock")]
    [Tooltip("DEV ONLY. When true, Ability buttons bind to the selected tower. Product builds always use saved global actives.")]
    [SerializeField] private bool enablePrototypeTowerBinding = false;

    [Header("Ability Slots (Inspector)")]
    [Tooltip("Drag Abilities_1 and Abilities_2 roots here (with Ability_Image / Ability_Level / Unity_Tag_BackGround).")]
    [SerializeField] private DeckCardView[] abilitySlotViews;

    [SerializeField] private string[] existingButtonObjectNames = { "Abilities_1", "Abilities_2" };

    [Header("Shared Visuals (Inspector)")]
    [SerializeField] private DeckCardVisualSettings visualSettings;
    [SerializeField] private Sprite tagFrameSprite;
    [SerializeField, Min(1)] private int defaultAbilityLevel = 1;
    [SerializeField] private string targetingHintText = "TAP";

    [Header("Cooldown Presentation")]
    [SerializeField] private Color unavailableColor = new Color(0.28f, 0.28f, 0.28f, 0.72f);
    [SerializeField] private Color coolingColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color targetingTint = new Color(0.55f, 0.85f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float chargeFillAlpha = 0.82f;
    [SerializeField, Range(0f, 1f)] private float readyGlowAlpha = 0.38f;
    [SerializeField, Min(0f)] private float readyGlowPulseSpeed = 4.5f;
    [SerializeField, Range(1f, 1.3f)] private float readyGlowScale = 1.09f;

    private readonly List<Button> buttons = new List<Button>(2);
    private readonly List<DeckCardView> slots = new List<DeckCardView>(2);
    private readonly List<Image> portraits = new List<Image>(2);
    private readonly List<Image> chargeFills = new List<Image>(2);
    private readonly List<Image> readyGlows = new List<Image>(2);
    private readonly List<TextMeshProUGUI> cooldownLabels = new List<TextMeshProUGUI>(2);
    private readonly List<TowerAbilityBase> bindings = new List<TowerAbilityBase>(2);
    private readonly List<UnityAction> listeners = new List<UnityAction>(2);
    private bool globalCastSubscribed;

    private bool UsePrototypeTowerBinding
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return enablePrototypeTowerBinding;
#else
            return false;
#endif
        }
    }

    private void Awake()
    {
        if (visualSettings == null)
            visualSettings = Resources.Load<DeckCardVisualSettings>("DeckCardVisualSettings");

        if (tagFrameSprite == null && visualSettings != null)
            tagFrameSprite = visualSettings.tagFrameSprite;
    }

    private void OnEnable()
    {
        TowerBoardCell.BoardChanged += RefreshBindings;
        BoardTowerInputController.AbilitySelectionChanged += HandleSelectionChanged;
        GlobalActiveCastTargetingController.TargetingChanged += HandleTargetingChanged;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    private void Start()
    {
        ResolveExistingButtons();
        RefreshBindings();
        SubscribeGlobalCastService();
        if (!UsePrototypeTowerBinding)
            Debug.Log("[Option A] HeroAbilityButtonController: bound to saved global actives.");
    }

    private void Update()
    {
        if (!HasValidUiSlots())
            return;

        if (!UsePrototypeTowerBinding)
        {
            for (int i = 0; i < buttons.Count; i++)
                UpdateSlotVisual(i);
            return;
        }

        BoardTower selectedTower = BoardTowerInputController.SelectedAbilityTower;
        if (selectedTower == null || selectedTower.CurrentCell == null)
        {
            if (HasAnyBinding())
                RefreshBindings();
        }

        for (int i = 0; i < buttons.Count; i++)
            UpdateSlotVisual(i);
    }

    private void OnDisable()
    {
        TowerBoardCell.BoardChanged -= RefreshBindings;
        BoardTowerInputController.AbilitySelectionChanged -= HandleSelectionChanged;
        GlobalActiveCastTargetingController.TargetingChanged -= HandleTargetingChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        UnsubscribeGlobalCastService();
        RemoveButtonListeners();
    }

    private void HandleSelectionChanged(BoardTower selectedTower)
    {
        RefreshBindings();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveExistingButtons();
        RefreshBindings();
        SubscribeGlobalCastService();
    }

    private void HandleSceneUnloaded(Scene scene)
    {
        if (GlobalActiveCastTargetingController.IsTargetingActive)
            GlobalActiveCastTargetingController.Instance.CancelTargeting("Scene unloaded.");

        ClearResolvedButtons();
    }

    private void ClearResolvedButtons()
    {
        RemoveButtonListeners();
        buttons.Clear();
        slots.Clear();
        portraits.Clear();
        chargeFills.Clear();
        readyGlows.Clear();
        cooldownLabels.Clear();
        bindings.Clear();
        listeners.Clear();
    }

    private bool HasValidUiSlots()
    {
        if (buttons.Count == 0 || slots.Count == 0 || portraits.Count == 0)
            return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null || portraits[i] == null || buttons[i] == null)
            {
                ClearResolvedButtons();
                return false;
            }
        }

        return true;
    }

    private void ResolveExistingButtons()
    {
        ClearResolvedButtons();
        ApplySharedVisuals();

        if (abilitySlotViews != null && abilitySlotViews.Length > 0)
        {
            int limit = Mathf.Min(abilitySlotViews.Length, GlobalActiveCastService.MaxSlots);
            for (int i = 0; i < limit; i++)
                RegisterAbilitySlot(abilitySlotViews[i]);
            return;
        }

        Image[] sceneImages = FindObjectsByType<Image>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int maxNames = Mathf.Min(existingButtonObjectNames.Length, GlobalActiveCastService.MaxSlots);
        for (int nameIndex = 0; nameIndex < maxNames; nameIndex++)
        {
            Image frame = FindNamedImage(sceneImages, existingButtonObjectNames[nameIndex]);
            if (frame == null)
                continue;

            DeckCardView slotView = frame.GetComponent<DeckCardView>();
            if (slotView == null)
                slotView = frame.gameObject.AddComponent<DeckCardView>();

            slotView.ApplyVisualSettings(visualSettings);
            slotView.ApplyTagFrameSprite(tagFrameSprite);
            RegisterAbilitySlot(slotView);
        }
    }

    private void RegisterAbilitySlot(DeckCardView slotView)
    {
        if (slotView == null)
            return;

        slotView.ApplyVisualSettings(visualSettings);
        slotView.ApplyTagFrameSprite(tagFrameSprite);

        Image frame = slotView.FrameImage;
        if (frame == null)
            return;

        Button button = frame.GetComponent<Button>();
        if (button == null)
            button = frame.gameObject.AddComponent<Button>();

        button.targetGraphic = frame;
        int slot = buttons.Count;
        UnityAction listener = () => HandlePressed(slot);
        button.onClick.AddListener(listener);

        Image portrait = slotView.PortraitImage;
        if (portrait == null)
            return;

        buttons.Add(button);
        slots.Add(slotView);
        portraits.Add(portrait);
        listeners.Add(listener);
        bindings.Add(null);
        readyGlows.Add(EnsureOverlayImage(portrait.rectTransform, "Ready Glow"));
        chargeFills.Add(EnsureOverlayImage(portrait.rectTransform, "Cooldown Charge"));
        cooldownLabels.Add(EnsureCooldownLabel(portrait.rectTransform));
    }

    private void ApplySharedVisuals()
    {
        if (abilitySlotViews == null)
            return;

        foreach (DeckCardView slotView in abilitySlotViews)
        {
            if (slotView == null)
                continue;

            slotView.ApplyVisualSettings(visualSettings);
            slotView.ApplyTagFrameSprite(tagFrameSprite);
        }
    }

    private void RefreshBindings()
    {
        if (buttons.Count == 0)
            return;

        if (!UsePrototypeTowerBinding)
        {
            for (int slot = 0; slot < buttons.Count; slot++)
            {
                bindings[slot] = null;
                UpdateSlotVisual(slot, true);
            }

            SubscribeGlobalCastService();
            return;
        }

        BoardTower selectedTower = BoardTowerInputController.SelectedAbilityTower;
        TowerAbilityBase[] selectedAbilities = selectedTower != null && selectedTower.CurrentCell != null
            ? selectedTower.GetComponents<TowerAbilityBase>()
            : System.Array.Empty<TowerAbilityBase>();

        int abilityIndex = 0;
        for (int slot = 0; slot < buttons.Count; slot++)
        {
            TowerAbilityBase binding = null;
            while (abilityIndex < selectedAbilities.Length && binding == null)
            {
                TowerAbilityBase candidate = selectedAbilities[abilityIndex++];
                if (candidate != null &&
                    candidate.isActiveAndEnabled &&
                    candidate.SupportsManualActivation &&
                    !candidate.IsRuntimeCopy)
                {
                    binding = candidate;
                }
            }

            bindings[slot] = binding;
            UpdateSlotVisual(slot, true);
        }
    }

    private void UpdateSlotVisual(int index, bool force = false)
    {
        if (index < 0 || index >= buttons.Count || index >= slots.Count || index >= portraits.Count)
            return;

        DeckCardView slotView = slots[index];
        Image portrait = portraits[index];
        Image charge = index < chargeFills.Count ? chargeFills[index] : null;
        Image glow = index < readyGlows.Count ? readyGlows[index] : null;
        TextMeshProUGUI label = index < cooldownLabels.Count ? cooldownLabels[index] : null;
        Button button = buttons[index];
        if (slotView == null || portrait == null || charge == null || glow == null || label == null || button == null)
        {
            ClearResolvedButtons();
            return;
        }

        slotView.ApplyVisualSettings(visualSettings);
        slotView.ApplyTagFrameSprite(tagFrameSprite);

        if (!UsePrototypeTowerBinding)
        {
            UpdateGlobalSlotVisual(index, slotView, portrait, charge, glow, label, button);
            return;
        }

        TowerAbilityBase ability = index < bindings.Count ? bindings[index] : null;
        bool bound = ability != null && ability.Owner != null && ability.Owner.CurrentCell != null;
        bool ready = BattleFlowState.IsGameplayActive && bound && ability.IsReady;
        float progress = bound ? ability.CooldownProgress : 0f;
        Color abilityColor = bound ? ability.AbilityColor : Color.white;
        Sprite displaySprite = bound ? ability.AbilityIcon : null;

        if (bound)
        {
            slotView.BindPortrait(displaySprite, defaultAbilityLevel, visualSettings != null ? visualSettings.abilityCategoryTag : null);
            slotView.ApplyTagFrameSprite(tagFrameSprite);
        }
        else
            slotView.SetEmpty();

        portrait.color = !bound ? unavailableColor : ready ? Color.white : coolingColor;

        ApplyCooldownPresentation(charge, glow, label, displaySprite, abilityColor, bound, ready, false, progress,
            bound ? ability.CooldownRemaining : 0f);

        button.interactable = ready;
    }

    private void UpdateGlobalSlotVisual(
        int index,
        DeckCardView slotView,
        Image portrait,
        Image charge,
        Image glow,
        TextMeshProUGUI label,
        Button button)
    {
        GlobalActiveCastService castService = GlobalActiveCastService.Instance;
        ActiveAbilityDefinition definition = castService != null ? castService.GetActive(index) : null;
        bool bound = definition != null;
        bool implemented = bound && definition.implemented;
        bool awaitingTarget = GlobalActiveCastTargetingController.IsTargetingActive &&
                             GlobalActiveCastTargetingController.Instance.PendingSlot == index;
        bool ready = implemented && castService != null &&
                     BattleFlowState.IsGameplayActive && castService.IsReady(index);
        float progress = bound && castService != null ? castService.GetCooldownProgress(index) : 0f;
        float cooldownRemaining = bound && castService != null ? castService.GetCooldownRemaining(index) : 0f;
        Color abilityColor = awaitingTarget ? targetingTint : Color.white;
        Sprite displaySprite = definition != null ? definition.icon : null;

        if (bound)
        {
            slotView.BindAbility(definition, defaultAbilityLevel);
            slotView.ApplyTagFrameSprite(tagFrameSprite);
        }
        else
            slotView.SetEmpty();

        // Empty / unimplemented slots stay grey; ready and targeting stay bright.
        if (!bound)
            portrait.color = emptyPortraitColor();
        else if (!implemented)
            portrait.color = unavailableColor;
        else if (awaitingTarget || ready)
            portrait.color = Color.white;
        else
            portrait.color = coolingColor;

        ApplyCooldownPresentation(
            charge,
            glow,
            label,
            displaySprite,
            abilityColor,
            bound && implemented,
            ready,
            awaitingTarget,
            progress,
            cooldownRemaining);

        // Keep pending slot clickable so the player can cancel targeting.
        button.interactable = ready || awaitingTarget;
    }

    private static Color emptyPortraitColor()
    {
        return new Color(1f, 1f, 1f, 0.18f);
    }

    private void ApplyCooldownPresentation(
        Image charge,
        Image glow,
        TextMeshProUGUI cooldownLabel,
        Sprite displaySprite,
        Color abilityColor,
        bool bound,
        bool ready,
        bool awaitingTarget,
        float progress,
        float cooldownRemaining)
    {
        charge.sprite = displaySprite;
        charge.type = Image.Type.Filled;
        charge.fillMethod = Image.FillMethod.Radial360;
        charge.fillOrigin = (int)Image.Origin360.Top;
        charge.fillClockwise = true;
        charge.fillAmount = awaitingTarget ? 1f : progress;
        charge.color = WithAlpha(
            Color.Lerp(Color.white, abilityColor, awaitingTarget ? 0.75f : 0.52f),
            bound && !awaitingTarget ? chargeFillAlpha : awaitingTarget ? 0.35f : 0f);
        charge.enabled = bound && displaySprite != null && !awaitingTarget;

        glow.sprite = displaySprite;
        glow.enabled = (ready || awaitingTarget) && displaySprite != null;
        if (glow.enabled)
        {
            float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * readyGlowPulseSpeed) * 0.5f;
            float alpha = awaitingTarget
                ? readyGlowAlpha * Mathf.Lerp(0.75f, 1f, pulse)
                : readyGlowAlpha * Mathf.Lerp(0.55f, 1f, pulse);
            glow.color = WithAlpha(Color.Lerp(Color.white, abilityColor, awaitingTarget ? 0.8f : 0.58f), alpha);
            glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.02f, readyGlowScale, pulse);
        }

        bool showCooldown = bound && !ready && !awaitingTarget;
        bool showTargetHint = awaitingTarget;
        cooldownLabel.gameObject.SetActive(showCooldown || showTargetHint);
        if (showTargetHint)
            cooldownLabel.SetText(string.IsNullOrEmpty(targetingHintText) ? "TAP" : targetingHintText);
        else if (showCooldown)
            cooldownLabel.SetText("{0:0}", Mathf.Ceil(cooldownRemaining));
    }

    private void HandlePressed(int index)
    {
        if (!UsePrototypeTowerBinding)
        {
            GlobalActiveCastService castService = GlobalActiveCastService.Instance;
            if (castService == null)
                return;

            ActiveAbilityDefinition definition = castService.GetActive(index);
            if (definition == null || !definition.implemented)
                return;

            if (!castService.IsReady(index))
                return;

            GlobalActiveCastTargetingController targeting = GlobalActiveCastTargetingController.EnsureExists();

            if (GlobalActiveCastTargeting.RequiresPlayerTarget(definition))
            {
                if (targeting.IsTargeting && targeting.PendingSlot == index)
                {
                    targeting.CancelTargeting("Retoggled from HUD.");
                    UpdateSlotVisual(index, true);
                    return;
                }

                if (targeting.BeginTargeting(index, definition))
                {
                    GameAudioManager.PlayButtonConfirm();
                    UpdateSlotVisual(index, true);
                }

                return;
            }

            if (targeting.IsTargeting)
                targeting.CancelTargeting("Instant cast selected.");

            GlobalActiveCastContext context = GlobalActiveCastTargeting.ResolveCastContext(definition);
            if (!castService.TryCast(index, context))
                return;

            GameAudioManager.PlayButtonConfirm();
            UpdateSlotVisual(index, true);
            return;
        }

        if (index < 0 || index >= bindings.Count)
            return;

        TowerAbilityBase ability = bindings[index];
        if (ability == null || !ability.TryActivateFromButton())
            return;

        GameAudioManager.PlayButtonConfirm();
        UpdateSlotVisual(index, true);
    }

    private void HandleTargetingChanged()
    {
        if (UsePrototypeTowerBinding || !HasValidUiSlots())
            return;

        for (int i = 0; i < buttons.Count; i++)
            UpdateSlotVisual(i, true);
    }

    private bool HasAnyBinding()
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i] != null)
                return true;
        }
        return false;
    }

    private void RemoveButtonListeners()
    {
        for (int i = 0; i < buttons.Count && i < listeners.Count; i++)
        {
            if (buttons[i] != null)
                buttons[i].onClick.RemoveListener(listeners[i]);
        }
    }

    private void SubscribeGlobalCastService()
    {
        if (UsePrototypeTowerBinding)
            return;

        GlobalActiveCastService castService = GlobalActiveCastService.Instance;
        if (castService == null || globalCastSubscribed)
            return;

        castService.SlotStateChanged += HandleGlobalCastStateChanged;
        globalCastSubscribed = true;
    }

    private void UnsubscribeGlobalCastService()
    {
        if (!globalCastSubscribed)
            return;

        GlobalActiveCastService castService = GlobalActiveCastService.Instance;
        if (castService != null)
            castService.SlotStateChanged -= HandleGlobalCastStateChanged;

        globalCastSubscribed = false;
    }

    private void HandleGlobalCastStateChanged()
    {
        if (UsePrototypeTowerBinding || !HasValidUiSlots())
            return;

        for (int i = 0; i < buttons.Count; i++)
            UpdateSlotVisual(i, true);
    }

    private static Image FindNamedImage(Image[] imagesInScene, string objectName)
    {
        for (int i = 0; i < imagesInScene.Length; i++)
        {
            Image image = imagesInScene[i];
            if (image != null && image.gameObject.name == objectName)
                return image;
        }

        return null;
    }

    private static Image EnsureOverlayImage(RectTransform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        Image image;
        if (existing != null)
        {
            image = existing.GetComponent<Image>();
        }
        else
        {
            GameObject overlay = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay.transform.SetParent(parent, false);
            image = overlay.GetComponent<Image>();
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private static TextMeshProUGUI EnsureCooldownLabel(RectTransform parent)
    {
        Transform existing = parent.Find("Cooldown Time");
        TextMeshProUGUI label;
        if (existing != null)
        {
            label = existing.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            GameObject labelObject = new GameObject("Cooldown Time", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
        }

        RectTransform rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.fontSize = 24f;
        label.color = Color.white;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
