using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class HeroAbilityButtonController : MonoBehaviour
{
    [Header("Option A — Product lock")]
    [Tooltip("NON-PRODUCT. When false (default), Ability/Ability_2 buttons do not bind to the selected tower. Milestone 2 will bind global actives from loadout.")]
    [SerializeField] private bool enablePrototypeTowerBinding = false;

    [SerializeField] private string[] existingButtonObjectNames = { "Ability", "Ability_2" };

    [Header("Cooldown Presentation")]
    [SerializeField] private Color unavailableColor = new Color(0.28f, 0.28f, 0.28f, 0.72f);
    [SerializeField] private Color coolingColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField, Range(0f, 1f)] private float chargeFillAlpha = 0.82f;
    [SerializeField, Range(0f, 1f)] private float readyGlowAlpha = 0.38f;
    [SerializeField, Min(0f)] private float readyGlowPulseSpeed = 4.5f;
    [SerializeField, Range(1f, 1.3f)] private float readyGlowScale = 1.09f;

    private readonly List<Button> buttons = new List<Button>(2);
    private readonly List<Image> images = new List<Image>(2);
    private readonly List<Image> chargeFills = new List<Image>(2);
    private readonly List<Image> readyGlows = new List<Image>(2);
    private readonly List<TextMeshProUGUI> cooldownLabels = new List<TextMeshProUGUI>(2);
    private readonly List<Sprite> originalSprites = new List<Sprite>(2);
    private readonly List<TowerAbilityBase> bindings = new List<TowerAbilityBase>(2);
    private readonly List<UnityAction> listeners = new List<UnityAction>(2);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // Keep a controller present so buttons stay in a safe non-product state until M2 global cast.
        if (FindFirstObjectByType<HeroAbilityButtonController>() == null)
            new GameObject("Hero Ability Button Controller", typeof(HeroAbilityButtonController));
    }

    private void OnEnable()
    {
        TowerBoardCell.BoardChanged += RefreshBindings;
        BoardTowerInputController.AbilitySelectionChanged += HandleSelectionChanged;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    private void Start()
    {
        ResolveExistingButtons();
        RefreshBindings();
        if (!enablePrototypeTowerBinding)
            Debug.Log("[Option A] HeroAbilityButtonController: prototype tower-binding DISABLED. HUD actives await global loadout (M2).");
    }

    private void Update()
    {
        if (!HasValidUiSlots())
            return;

        if (!enablePrototypeTowerBinding)
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
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
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
    }

    private void HandleSceneUnloaded(Scene scene)
    {
        // Battle/Hub UI Images are destroyed with their scene — drop stale refs immediately.
        ClearResolvedButtons();
    }

    private void ClearResolvedButtons()
    {
        RemoveButtonListeners();
        buttons.Clear();
        images.Clear();
        chargeFills.Clear();
        readyGlows.Clear();
        cooldownLabels.Clear();
        originalSprites.Clear();
        bindings.Clear();
        listeners.Clear();
    }

    private bool HasValidUiSlots()
    {
        if (buttons.Count == 0 || images.Count == 0)
            return false;

        // Unity fake-null: destroyed UI after additive unload.
        for (int i = 0; i < images.Count; i++)
        {
            if (images[i] == null || buttons[i] == null)
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

        Image[] sceneImages = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int nameIndex = 0; nameIndex < existingButtonObjectNames.Length; nameIndex++)
        {
            Image found = FindNamedImage(sceneImages, existingButtonObjectNames[nameIndex]);
            if (found == null)
                continue;

            Button button = found.GetComponent<Button>();
            if (button == null)
                button = found.gameObject.AddComponent<Button>();

            button.targetGraphic = found;
            int slot = buttons.Count;
            UnityAction listener = () => HandlePressed(slot);
            button.onClick.AddListener(listener);

            buttons.Add(button);
            images.Add(found);
            originalSprites.Add(found.sprite);
            listeners.Add(listener);
            bindings.Add(null);
            readyGlows.Add(EnsureOverlayImage(found.rectTransform, "Ready Glow"));
            chargeFills.Add(EnsureOverlayImage(found.rectTransform, "Cooldown Charge"));
            cooldownLabels.Add(EnsureCooldownLabel(found.rectTransform));
        }
    }

    private void RefreshBindings()
    {
        if (buttons.Count == 0)
            return;

        if (!enablePrototypeTowerBinding)
        {
            for (int slot = 0; slot < buttons.Count; slot++)
            {
                bindings[slot] = null;
                UpdateSlotVisual(slot, true);
            }
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
        if (index < 0 || index >= buttons.Count || index >= images.Count)
            return;

        Image baseImage = images[index];
        Image charge = index < chargeFills.Count ? chargeFills[index] : null;
        Image glow = index < readyGlows.Count ? readyGlows[index] : null;
        TextMeshProUGUI label = index < cooldownLabels.Count ? cooldownLabels[index] : null;
        Button button = buttons[index];
        if (baseImage == null || charge == null || glow == null || label == null || button == null)
        {
            ClearResolvedButtons();
            return;
        }

        TowerAbilityBase ability = index < bindings.Count ? bindings[index] : null;
        bool bound = ability != null && ability.Owner != null && ability.Owner.CurrentCell != null;
        bool ready = BattleFlowState.IsGameplayActive && bound && ability.IsReady;
        float progress = bound ? ability.CooldownProgress : 0f;
        Color abilityColor = bound ? ability.AbilityColor : Color.white;
        Sprite displaySprite = originalSprites[index] != null
            ? originalSprites[index]
            : bound ? ability.AbilityIcon : null;

        baseImage.sprite = displaySprite;
        baseImage.color = !bound ? unavailableColor : ready ? Color.white : coolingColor;

        charge.sprite = displaySprite;
        charge.type = Image.Type.Filled;
        charge.fillMethod = Image.FillMethod.Radial360;
        charge.fillOrigin = (int)Image.Origin360.Top;
        charge.fillClockwise = true;
        charge.fillAmount = progress;
        charge.color = WithAlpha(Color.Lerp(Color.white, abilityColor, 0.52f), bound ? chargeFillAlpha : 0f);
        charge.enabled = bound && displaySprite != null;

        glow.sprite = displaySprite;
        glow.enabled = ready && displaySprite != null;
        if (glow.enabled)
        {
            float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * readyGlowPulseSpeed) * 0.5f;
            glow.color = WithAlpha(Color.Lerp(Color.white, abilityColor, 0.58f), readyGlowAlpha * Mathf.Lerp(0.55f, 1f, pulse));
            glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.02f, readyGlowScale, pulse);
        }

        label.gameObject.SetActive(bound && !ready);
        if (bound && !ready)
            label.SetText("{0:0}", Mathf.Ceil(ability.CooldownRemaining));

        button.interactable = ready;
        button.gameObject.name = existingButtonObjectNames[Mathf.Min(index, existingButtonObjectNames.Length - 1)];
    }

    private void HandlePressed(int index)
    {
        if (!enablePrototypeTowerBinding)
            return;

        if (index < 0 || index >= bindings.Count)
            return;

        TowerAbilityBase ability = bindings[index];
        if (ability == null || !ability.TryActivateFromButton())
            return;

        GameAudioManager.PlayButtonConfirm();
        UpdateSlotVisual(index, true);
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
        image.preserveAspect = false;
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
        label.fontSize = 38f;
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
