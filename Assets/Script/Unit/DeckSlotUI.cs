using UnityEngine;
using UnityEngine.EventSystems;

public class DeckSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Unit Data")]
    [SerializeField] private UnitData unitData;

    [Header("Level")]
    [SerializeField] private int currentLevel = 1;

    [Header("UI References")]
    [SerializeField] private UnityEngine.UI.Image iconImage;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform dragLayer;

    [Header("Drop Settings")]
    [SerializeField] private LayerMask boardCellLayer;

    private UnityEngine.UI.Image dragIcon;

    public UnitData UnitData => unitData;
    public int CurrentLevel => currentLevel;

    private void Awake()
    {
        CleanupDragIcon();
        RefreshIcon();
    }

    private void OnDisable()
    {
        CleanupDragIcon();
    }

    private void OnDestroy()
    {
        CleanupDragIcon();
    }

    public void UpgradeLevel()
    {
        currentLevel++;
        RefreshIcon();
    }

    private void RefreshIcon()
    {
        if (iconImage == null || unitData == null)
            return;

        iconImage.sprite = unitData.GetIcon(currentLevel);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        CleanupDragIcon();

        if (!BattleFlowState.IsGameplayActive || unitData == null || dragLayer == null)
            return;

        GameObject prefab = unitData.GetPrefab(currentLevel);

        if (prefab == null)
            return;

        GameObject dragObject = new GameObject("Dragging_" + unitData.unitName);
        dragObject.transform.SetParent(dragLayer, false);

        dragIcon = dragObject.AddComponent<UnityEngine.UI.Image>();
        dragIcon.sprite = unitData.GetIcon(currentLevel);
        dragIcon.raycastTarget = false;
        dragIcon.preserveAspect = true;
        dragIcon.rectTransform.sizeDelta = new Vector2(120, 120);

        MoveDragIcon(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!BattleFlowState.IsGameplayActive)
        {
            CleanupDragIcon();
            return;
        }

        MoveDragIcon(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!BattleFlowState.IsGameplayActive)
        {
            CleanupDragIcon();
            return;
        }

        TryDropTower(eventData);
        CleanupDragIcon();
    }

    private void MoveDragIcon(PointerEventData eventData)
    {
        if (dragIcon == null)
            return;

        Camera uiCamera = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragLayer,
            eventData.position,
            uiCamera,
            out Vector2 localPoint
        );

        dragIcon.rectTransform.localPosition = localPoint;
    }

    private void TryDropTower(PointerEventData eventData)
    {
        if (!BattleFlowState.IsGameplayActive || Camera.main == null || unitData == null)
            return;

        Vector3 worldPosition = CanvasMapSpace.ScreenToGameplayWorld(eventData.position);

        Collider2D hit = Physics2D.OverlapPoint(worldPosition, boardCellLayer);

        if (hit == null)
            return;

        TowerBoardCell cell = hit.GetComponent<TowerBoardCell>();

        if (cell == null)
            return;

        GameObject prefab = unitData.GetPrefab(currentLevel);

        if (prefab == null)
            return;

        cell.PlaceTower(prefab, unitData, currentLevel);
    }

    private void CleanupDragIcon()
    {
        if (dragIcon != null)
            Destroy(dragIcon.gameObject);

        dragIcon = null;
    }
}
