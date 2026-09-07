using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Captures a battlefield tap for Enemy / Point / BoardCell global actives.
/// None and Global cast immediately without entering this mode.
/// </summary>
public sealed class GlobalActiveCastTargetingController : MonoBehaviour
{
    public static GlobalActiveCastTargetingController Instance { get; private set; }

    public static event Action TargetingChanged;

    [SerializeField, Min(0.1f)] private float enemyPickRadius = 1.15f;
    [SerializeField, Min(0.1f)] private float boardCellPickRadius = 1.25f;

    private int pendingSlot = -1;
    private ActiveAbilityDefinition pendingDefinition;
    private bool touchWasPressed;

    public bool IsTargeting => pendingSlot >= 0 && pendingDefinition != null;
    public int PendingSlot => pendingSlot;
    public ActiveAbilityDefinition PendingDefinition => pendingDefinition;

    public static bool IsTargetingActive => Instance != null && Instance.IsTargeting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static GlobalActiveCastTargetingController EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GlobalActiveCastTargetingController existing =
            FindFirstObjectByType<GlobalActiveCastTargetingController>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject host = new GameObject(nameof(GlobalActiveCastTargetingController));
        return host.AddComponent<GlobalActiveCastTargetingController>();
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

        if (IsTargeting)
            ClearPending(raiseEvent: false);
    }

    private void Update()
    {
        if (!IsTargeting)
            return;

        if (!BattleFlowState.IsGameplayActive)
        {
            CancelTargeting("Gameplay inactive.");
            return;
        }

        GlobalActiveCastService castService = GlobalActiveCastService.Instance;
        if (castService == null || !castService.IsReady(pendingSlot))
        {
            CancelTargeting("Ability no longer ready.");
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelTargeting("Cancelled.");
            return;
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelTargeting("Cancelled.");
            return;
        }

        if (!HandleTouchInput())
            HandleMouseInput();
    }

    public bool BeginTargeting(int slot, ActiveAbilityDefinition definition)
    {
        if (definition == null || !GlobalActiveCastTargeting.RequiresPlayerTarget(definition))
            return false;

        if (!BattleFlowState.IsGameplayActive)
            return false;

        GlobalActiveCastService castService = GlobalActiveCastService.Instance;
        if (castService == null || !castService.IsReady(slot))
            return false;

        if (IsTargeting && pendingSlot == slot)
        {
            CancelTargeting("Retoggled.");
            return false;
        }

        pendingSlot = slot;
        pendingDefinition = definition;
        Debug.Log("[GlobalActiveCastTargeting] Waiting for " + definition.targeting + " target (" + definition.displayName + ").");
        RaiseTargetingChanged();
        return true;
    }

    public void CancelTargeting(string reason = null)
    {
        if (!IsTargeting)
            return;

        if (!string.IsNullOrEmpty(reason))
            Debug.Log("[GlobalActiveCastTargeting] " + reason);

        ClearPending(raiseEvent: true);
    }

    private void ClearPending(bool raiseEvent)
    {
        pendingSlot = -1;
        pendingDefinition = null;
        touchWasPressed = false;

        if (raiseEvent)
            RaiseTargetingChanged();
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            TryResolvePointer(Mouse.current.position.ReadValue());
    }

    private bool HandleTouchInput()
    {
        if (Touchscreen.current == null)
            return false;

        var touch = Touchscreen.current.primaryTouch;
        if (!touch.press.isPressed && !touchWasPressed)
            return false;

        Vector2 screenPosition = touch.position.ReadValue();

        if (touch.press.wasPressedThisFrame)
            touchWasPressed = true;

        if (touch.press.wasReleasedThisFrame)
        {
            touchWasPressed = false;
            TryResolvePointer(screenPosition);
        }

        return true;
    }

    private void TryResolvePointer(Vector2 screenPosition)
    {
        if (!IsTargeting)
            return;

        if (IsPointerOverUi(screenPosition))
            return;

        ActiveAbilityDefinition definition = pendingDefinition;
        int slot = pendingSlot;
        GlobalActiveCastContext context = BuildContextFromPointer(definition, screenPosition);

        if (!GlobalActiveCastTargeting.HasRequiredTarget(definition, context))
        {
            Debug.Log("[GlobalActiveCastTargeting] Invalid target for '" + definition.displayName + "'.");
            return;
        }

        GlobalActiveCastService castService = GlobalActiveCastService.Instance;
        if (castService == null)
        {
            CancelTargeting("Cast service missing.");
            return;
        }

        ClearPending(raiseEvent: true);

        if (!castService.TryCast(slot, context))
        {
            Debug.Log("[GlobalActiveCastTargeting] Cast failed for slot " + slot + ".");
            return;
        }

        GameAudioManager.PlayButtonConfirm();
    }

    private GlobalActiveCastContext BuildContextFromPointer(ActiveAbilityDefinition definition, Vector2 screenPosition)
    {
        Vector3 worldPoint = CanvasMapSpace.ScreenToGameplayWorld(screenPosition);

        switch (definition.targeting)
        {
            case ActiveAbilityTargeting.Enemy:
            {
                Enemy enemy = FindNearestEnemy(worldPoint, enemyPickRadius);
                return GlobalActiveCastContext.FromEnemy(enemy);
            }

            case ActiveAbilityTargeting.BoardCell:
            {
                TowerBoardCell cell = FindNearestBoardCell(worldPoint, boardCellPickRadius);
                if (cell == null)
                    return default;

                return GlobalActiveCastContext.FromBoardCell(cell);
            }

            case ActiveAbilityTargeting.Point:
            default:
                return GlobalActiveCastContext.FromPoint(worldPoint);
        }
    }

    private static Enemy FindNearestEnemy(Vector3 worldPoint, float radius)
    {
        IReadOnlyList<Enemy> enemies = Enemy.ActiveEnemies;
        Enemy nearest = null;
        float nearestDistance = radius;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsTargetable)
                continue;

            float distance = Vector2.Distance(worldPoint, enemy.transform.position);
            if (distance > nearestDistance)
                continue;

            nearestDistance = distance;
            nearest = enemy;
        }

        return nearest;
    }

    private static TowerBoardCell FindNearestBoardCell(Vector3 worldPoint, float radius)
    {
        TowerBoardCell[] cells = FindObjectsByType<TowerBoardCell>(FindObjectsSortMode.None);
        TowerBoardCell nearest = null;
        float nearestDistance = radius;

        for (int i = 0; i < cells.Length; i++)
        {
            TowerBoardCell cell = cells[i];
            if (cell == null)
                continue;

            float distance = Vector2.Distance(worldPoint, cell.SpawnPosition);
            if (distance > nearestDistance)
                continue;

            nearestDistance = distance;
            nearest = cell;
        }

        return nearest;
    }

    private static bool IsPointerOverUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>(4);
        EventSystem.current.RaycastAll(eventData, results);
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject != null && results[i].gameObject.GetComponentInParent<Selectable>() != null)
                return true;
        }

        return false;
    }

    private static void RaiseTargetingChanged()
    {
        TargetingChanged?.Invoke();
    }
}
