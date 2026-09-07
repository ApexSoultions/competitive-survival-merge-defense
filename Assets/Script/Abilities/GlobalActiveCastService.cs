using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Battle runtime for the two saved global actives from the hub loadout (Option A).
/// Initialized by <see cref="BattleLoadoutBootstrap"/>; consumed by HUD ability buttons.
/// </summary>
public sealed class GlobalActiveCastService : MonoBehaviour
{
    public const int MaxSlots = 2;

    private static readonly Dictionary<string, IGlobalActiveCastHandler> Handlers = BuildHandlers();

    public static GlobalActiveCastService Instance { get; private set; }

    public event Action SlotStateChanged;

    private readonly ActiveAbilityDefinition[] actives = new ActiveAbilityDefinition[MaxSlots];
    private readonly float[] cooldownEndTimes = new float[MaxSlots];
    private int activeSlotCount;

    public int ActiveSlotCount => activeSlotCount;

    public static GlobalActiveCastService EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GlobalActiveCastService existing = FindFirstObjectByType<GlobalActiveCastService>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject host = new GameObject(nameof(GlobalActiveCastService));
        return host.AddComponent<GlobalActiveCastService>();
    }

    public static void RegisterHandler(IGlobalActiveCastHandler handler)
    {
        if (handler == null || string.IsNullOrWhiteSpace(handler.AbilityId))
            return;

        Handlers[handler.AbilityId] = handler;
    }

    private static Dictionary<string, IGlobalActiveCastHandler> BuildHandlers()
    {
        var map = new Dictionary<string, IGlobalActiveCastHandler>(StringComparer.Ordinal);
        RegisterInto(map, new ManaSurgeCastHandler());
        RegisterInto(map, new MeteorStrikeCastHandler());
        RegisterInto(map, new FrostNovaCastHandler());
        RegisterInto(map, new ExecutionSigilCastHandler());
        RegisterInto(map, new RadiantCleanseCastHandler());
        RegisterInto(map, new ArcaneOverclockCastHandler());
        return map;
    }

    private static void RegisterInto(Dictionary<string, IGlobalActiveCastHandler> map, IGlobalActiveCastHandler handler)
    {
        if (handler == null || string.IsNullOrWhiteSpace(handler.AbilityId))
            return;

        map[handler.AbilityId] = handler;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Loads the saved global actives for this match. Cooldowns start ready.
    /// </summary>
    public void Initialize(ActiveAbilityDefinition[] loadoutActives, int slotCount = MaxSlots)
    {
        ResetRuntimeState();

        activeSlotCount = Mathf.Clamp(slotCount, 0, MaxSlots);
        for (int i = 0; i < activeSlotCount; i++)
        {
            actives[i] = loadoutActives != null && i < loadoutActives.Length
                ? loadoutActives[i]
                : null;
            cooldownEndTimes[i] = 0f;
        }

        LogInitializedActives();
        RaiseSlotStateChanged();
    }

    public void ResetRuntimeState()
    {
        activeSlotCount = 0;
        for (int i = 0; i < MaxSlots; i++)
        {
            actives[i] = null;
            cooldownEndTimes[i] = 0f;
        }
    }

    public ActiveAbilityDefinition GetActive(int slot)
    {
        if (!IsValidSlot(slot))
            return null;
        return actives[slot];
    }

    public float GetCooldownRemaining(int slot)
    {
        if (!IsValidSlot(slot))
            return 0f;

        float remaining = cooldownEndTimes[slot] - Time.time;
        return remaining > 0f ? remaining : 0f;
    }

    public float GetCooldownProgress(int slot)
    {
        if (!IsValidSlot(slot))
            return 0f;

        ActiveAbilityDefinition definition = actives[slot];
        if (definition == null || definition.cooldownSeconds <= 0f)
            return 0f;

        float remaining = GetCooldownRemaining(slot);
        if (remaining <= 0f)
            return 1f;

        return 1f - Mathf.Clamp01(remaining / definition.cooldownSeconds);
    }

    public bool IsReady(int slot)
    {
        if (!IsValidSlot(slot))
            return false;

        ActiveAbilityDefinition definition = actives[slot];
        if (definition == null || !definition.implemented)
            return false;

        return GetCooldownRemaining(slot) <= 0f;
    }

    public bool TryCast(int slot, GlobalActiveCastContext context)
    {
        if (!BattleFlowState.IsGameplayActive)
        {
            Debug.Log("[GlobalActiveCastService] Cast blocked — gameplay not active.");
            return false;
        }

        if (!IsValidSlot(slot))
            return false;

        ActiveAbilityDefinition definition = actives[slot];
        if (definition == null)
        {
            Debug.LogWarning("[GlobalActiveCastService] Slot " + slot + " has no active ability.");
            return false;
        }

        if (!IsReady(slot))
            return false;

        if (!definition.implemented)
        {
            Debug.Log("[GlobalActiveCastService] '" + definition.displayName + "' is not implemented yet.");
            return false;
        }

        GlobalActiveCastContext resolved = GlobalActiveCastTargeting.ResolveCastContext(definition, context);
        if (!GlobalActiveCastTargeting.HasRequiredTarget(definition, resolved))
        {
            Debug.Log("[GlobalActiveCastService] No valid target for '" + definition.displayName + "'.");
            return false;
        }

        if (!Handlers.TryGetValue(definition.id, out IGlobalActiveCastHandler handler))
        {
            Debug.LogWarning("[GlobalActiveCastService] No cast handler registered for '" + definition.id + "'.");
            return false;
        }

        if (!handler.TryExecute(definition, resolved))
            return false;

        BeginCooldown(slot);
        PlayCastFeedback(definition);
        Debug.Log("[GlobalActiveCastService] Cast '" + definition.displayName + "' from slot " + slot + ".");
        return true;
    }

    public void BeginCooldown(int slot)
    {
        if (!IsValidSlot(slot))
            return;

        ActiveAbilityDefinition definition = actives[slot];
        if (definition == null)
            return;

        cooldownEndTimes[slot] = Time.time + Mathf.Max(0.1f, definition.cooldownSeconds);
        RaiseSlotStateChanged();
    }

    private static void PlayCastFeedback(ActiveAbilityDefinition definition)
    {
        if (definition == null || definition.castSfx == null)
            return;

        GameAudioManager.PlayAbilityClip(definition.castSfx);
    }

    private bool IsValidSlot(int slot)
    {
        return slot >= 0 && slot < activeSlotCount && slot < MaxSlots;
    }

    private void LogInitializedActives()
    {
        for (int i = 0; i < activeSlotCount; i++)
        {
            ActiveAbilityDefinition definition = actives[i];
            if (definition == null)
            {
                Debug.LogWarning("[GlobalActiveCastService] Slot " + i + " is empty.");
                continue;
            }

            Debug.Log(
                "[GlobalActiveCastService] Slot " + i + ": " +
                definition.displayName + " (" + definition.id + "), " +
                "cooldown=" + definition.cooldownSeconds + "s, " +
                "implemented=" + definition.implemented);
        }
    }

    private void RaiseSlotStateChanged()
    {
        SlotStateChanged?.Invoke();
    }
}
