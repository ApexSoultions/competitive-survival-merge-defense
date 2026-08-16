using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BoardTowerInputController : MonoBehaviour
{
    public static event Action<BoardTower> AbilitySelectionChanged;
    public static BoardTower SelectedAbilityTower { get; private set; }

    [Header("Layers")]
    [SerializeField] private LayerMask towerLayer;
    [SerializeField] private LayerMask boardCellLayer;

    [Header("Ghost")]
    [SerializeField] private SpriteRenderer ghostRenderer;
    [SerializeField] private float ghostAlpha = 0.55f;
    [SerializeField] private int ghostSortingOrder = 200;

    [Header("Drag Settings")]
    [SerializeField] private float dragStartDistance = 0.15f;
    [SerializeField] private float towerPickRadius = 0.45f;
    [SerializeField] private float boardCellDetectRadius = 0.85f;

    private Camera mainCamera;

    private BoardTower selectedTower;
    private Vector2 pointerStartScreenPosition;
    private bool pointerDown;
    private bool isDragging;
    private bool touchWasPressed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSelection()
    {
        SelectedAbilityTower = null;
        AbilitySelectionChanged = null;
    }

    private void Awake()
    {
        mainCamera = Camera.main;

        if (towerLayer.value == 0)
            towerLayer = LayerMask.GetMask("Tower");

        if (boardCellLayer.value == 0)
            boardCellLayer = LayerMask.GetMask("BoardCell");

        HideGhost();
    }

    private void OnEnable()
    {
        ClearState();
    }

    private void OnDisable()
    {
        ClearState();
        SetAbilitySelection(null);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            ClearState();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            ClearState();
    }

    private void Update()
    {
        if (!BattleFlowState.IsGameplayActive)
        {
            if (pointerDown || isDragging || selectedTower != null || SelectedAbilityTower != null)
            {
                ClearState();
                SetAbilitySelection(null);
            }

            return;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        if (!HandleTouchInput())
            HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null)
            return;

        Vector2 screenPosition = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
            PointerDown(screenPosition);

        if (Mouse.current.leftButton.isPressed)
            PointerMove(screenPosition);

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            PointerUp(screenPosition);
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
        {
            touchWasPressed = true;
            PointerDown(screenPosition);
        }

        if (touch.press.isPressed)
            PointerMove(screenPosition);

        if (touch.press.wasReleasedThisFrame)
        {
            touchWasPressed = false;
            PointerUp(screenPosition);
        }

        return true;
    }

    private void PointerDown(Vector2 screenPosition)
    {
        if (!BattleFlowState.IsGameplayActive)
            return;

        if (IsPointerOverUI(screenPosition))
            return;

        BoardTower tower = GetTowerAtScreenPosition(screenPosition);

        if (tower == null)
        {
            SetAbilitySelection(null);
            if (TowerInfoUI.Instance != null)
                TowerInfoUI.Instance.Hide();

            return;
        }

        if (TowerInfoUI.Instance != null)
            TowerInfoUI.Instance.Hide();

        tower.TriggerPulseEffect();

        selectedTower = tower;
        pointerStartScreenPosition = screenPosition;
        pointerDown = true;
        isDragging = false;
    }

    private void PointerMove(Vector2 screenPosition)
    {
        if (!pointerDown || selectedTower == null)
            return;

        float worldDistance = Vector2.Distance(
            ScreenToWorld(pointerStartScreenPosition),
            ScreenToWorld(screenPosition)
        );

        if (!isDragging && worldDistance >= dragStartDistance)
        {
            isDragging = true;
            ShowGhost(selectedTower);

            if (TowerInfoUI.Instance != null)
                TowerInfoUI.Instance.Hide();
        }

        if (!isDragging)
            return;

        MoveGhost(ScreenToWorld(screenPosition));
    }

    private void PointerUp(Vector2 screenPosition)
    {
        if (!pointerDown || selectedTower == null)
        {
            ClearState();
            return;
        }

        if (!isDragging)
        {
            SetAbilitySelection(selectedTower);
            if (TowerInfoUI.Instance != null)
                TowerInfoUI.Instance.Show(selectedTower);

            ClearState();
            return;
        }

        BoardTower targetTower = GetTowerAtScreenPosition(screenPosition, selectedTower);

        if (targetTower != null)
        {
            if (MergeManager.Instance != null)
                MergeManager.Instance.TryMerge(selectedTower, targetTower);

            ClearState();
            return;
        }

        ClearState();
    }

    private BoardTower GetTowerAtScreenPosition(Vector2 screenPosition, BoardTower ignoredTower = null)
    {
        Vector3 world = ScreenToWorld(screenPosition);
        Collider2D[] hits = Physics2D.OverlapCircleAll(world, towerPickRadius, towerLayer);

        BoardTower nearest = null;
        float nearestDistance = float.MaxValue;
        HashSet<BoardTower> checkedTowers = new HashSet<BoardTower>();

        foreach (Collider2D hit in hits)
        {
            BoardTower tower = hit.GetComponentInParent<BoardTower>();

            if (tower == null || tower == ignoredTower || !checkedTowers.Add(tower))
                continue;

            // Adjacent unit colliders can overlap. ClosestPoint returns the pointer
            // itself for every overlapping collider, which makes the selected unit
            // depend on Unity's non-deterministic physics result order. The board
            // position uniquely identifies the unit the player actually tapped.
            Vector3 towerPosition = tower.CurrentCell != null
                ? tower.CurrentCell.SpawnPosition
                : tower.transform.position;
            float distance = Vector2.SqrMagnitude((Vector2)(world - towerPosition));

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = tower;
            }
        }

        if (nearest != null)
            return nearest;

        TowerBoardCell occupiedCell = FindNearestCell(world);

        if (occupiedCell == null ||
            occupiedCell.CurrentTower == null ||
            occupiedCell.CurrentTower == ignoredTower)
        {
            return null;
        }

        return occupiedCell.CurrentTower;
    }

    private TowerBoardCell FindNearestCell(Vector3 worldPosition)
    {
        TowerBoardCell nearest = null;
        float nearestDistance = float.MaxValue;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            worldPosition,
            boardCellDetectRadius,
            boardCellLayer
        );

        foreach (Collider2D hit in hits)
        {
            TowerBoardCell cell = hit.GetComponent<TowerBoardCell>();

            if (cell == null)
                continue;

            float distance = Vector2.Distance(worldPosition, cell.SpawnPosition);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = cell;
            }
        }

        if (nearest != null)
            return nearest;

        float fallbackDetectRadius = Mathf.Max(boardCellDetectRadius * 2.5f, 2.1f);
        TowerBoardCell[] cells = UnityEngine.Object.FindObjectsByType<TowerBoardCell>(FindObjectsSortMode.None);

        foreach (TowerBoardCell cell in cells)
        {
            if (cell == null)
                continue;

            float distance = Vector2.Distance(worldPosition, cell.SpawnPosition);

            if (distance > fallbackDetectRadius || distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearest = cell;
        }

        return nearest;
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        return CanvasMapSpace.ScreenToGameplayWorld(screenPosition);
    }

    private static bool IsPointerOverUI(Vector2 screenPosition)
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

    private void ShowGhost(BoardTower tower)
    {
        if (tower == null || tower.SpriteRenderer == null)
            return;

        EnsureGhostRenderer();

        if (ghostRenderer == null)
            return;

        ghostRenderer.sprite = tower.SpriteRenderer.sprite;
        ghostRenderer.transform.position = tower.transform.position;
        MatchGhostWorldScale(tower.SpriteRenderer.transform);
        ghostRenderer.sortingLayerID = tower.SpriteRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = Mathf.Max(ghostSortingOrder, tower.SpriteRenderer.sortingOrder + 20);
        ghostRenderer.gameObject.SetActive(true);

        Color color = tower.SpriteRenderer.color;
        color.a = ghostAlpha;
        ghostRenderer.color = color;
    }

    private void EnsureGhostRenderer()
    {
        if (ghostRenderer != null)
            return;

        GameObject ghostObject = new GameObject("TowerDragGhost");
        ghostObject.transform.SetParent(transform);
        ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
    }

    private void MoveGhost(Vector3 position)
    {
        if (ghostRenderer == null)
            return;

        ghostRenderer.transform.position = position;
    }

    private void MatchGhostWorldScale(Transform sourceVisual)
    {
        if (ghostRenderer == null || sourceVisual == null)
            return;

        Vector3 sourceScale = sourceVisual.lossyScale;
        Transform ghostParent = ghostRenderer.transform.parent;
        Vector3 parentScale = ghostParent != null ? ghostParent.lossyScale : Vector3.one;

        ghostRenderer.transform.localScale = new Vector3(
            SafeScaleRatio(sourceScale.x, parentScale.x),
            SafeScaleRatio(sourceScale.y, parentScale.y),
            SafeScaleRatio(sourceScale.z, parentScale.z)
        );
    }

    private static float SafeScaleRatio(float worldScale, float parentScale)
    {
        return Mathf.Approximately(parentScale, 0f) ? worldScale : worldScale / parentScale;
    }

    private void HideGhost()
    {
        if (ghostRenderer == null)
            return;

        ghostRenderer.sprite = null;
        ghostRenderer.gameObject.SetActive(false);
    }

    private void ClearState()
    {
        HideGhost();

        selectedTower = null;
        pointerDown = false;
        isDragging = false;
        touchWasPressed = false;
    }

    public static void SetAbilitySelection(BoardTower tower)
    {
        if (SelectedAbilityTower == tower)
            return;

        SelectedAbilityTower = tower;
        AbilitySelectionChanged?.Invoke(tower);
    }
}
