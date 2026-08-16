using UnityEngine;

// Compatibility input for older prefabs/scenes that do not use BoardTowerInputController.
// The current battle scene uses the central controller, which also handles touch input.
public class BoardTowerDrag : MonoBehaviour
{
    [SerializeField] private float cellDropRadius = 1.4f;
    [SerializeField] private float dragStartDistance = 0.15f;
    [SerializeField] private float ghostAlpha = 0.55f;

    private BoardTower boardTower;
    private SpriteRenderer ghostRenderer;
    private Vector3 pointerStartPosition;
    private bool pointerDown;
    private bool dragging;

    private void Awake()
    {
        boardTower = GetComponent<BoardTower>();
    }

    private void OnDisable()
    {
        ClearDragState();
    }

    private void OnMouseDown()
    {
        if (!BattleFlowState.IsGameplayActive || boardTower == null || HasCentralInputController())
            return;

        pointerStartPosition = CanvasMapSpace.ScreenToGameplayWorld(Input.mousePosition);
        pointerDown = true;
        dragging = false;
    }

    private void OnMouseDrag()
    {
        if (!BattleFlowState.IsGameplayActive || !pointerDown || boardTower == null)
            return;

        Vector3 pointerPosition = CanvasMapSpace.ScreenToGameplayWorld(Input.mousePosition);

        if (!dragging && Vector2.Distance(pointerStartPosition, pointerPosition) >= dragStartDistance)
        {
            dragging = true;
            ShowGhost();

            if (TowerInfoUI.Instance != null)
                TowerInfoUI.Instance.Hide();
        }

        if (dragging && ghostRenderer != null)
            ghostRenderer.transform.position = pointerPosition;
    }

    private void OnMouseUp()
    {
        if (!BattleFlowState.IsGameplayActive)
        {
            ClearDragState();
            return;
        }

        if (!pointerDown || boardTower == null)
        {
            ClearDragState();
            return;
        }

        if (!dragging)
        {
            if (TowerInfoUI.Instance != null)
                TowerInfoUI.Instance.Show(boardTower);

            ClearDragState();
            return;
        }

        Vector3 dropPosition = CanvasMapSpace.ScreenToGameplayWorld(Input.mousePosition);
        BoardTower targetTower = FindNearestOccupiedTower(dropPosition);

        if (targetTower != null && MergeManager.Instance != null)
            MergeManager.Instance.TryMerge(boardTower, targetTower);

        ClearDragState();
    }

    private BoardTower FindNearestOccupiedTower(Vector3 worldPosition)
    {
        TowerBoardCell nearestCell = null;
        float nearestDistance = float.MaxValue;
        TowerBoardCell[] cells = UnityEngine.Object.FindObjectsByType<TowerBoardCell>(FindObjectsSortMode.None);

        for (int i = 0; i < cells.Length; i++)
        {
            TowerBoardCell cell = cells[i];

            if (cell == null || cell.CurrentTower == null || cell.CurrentTower == boardTower)
                continue;

            float distance = Vector2.Distance(worldPosition, cell.SpawnPosition);

            if (distance > cellDropRadius || distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearestCell = cell;
        }

        return nearestCell != null ? nearestCell.CurrentTower : null;
    }

    private void ShowGhost()
    {
        if (boardTower.SpriteRenderer == null)
            return;

        if (ghostRenderer == null)
        {
            GameObject ghostObject = new GameObject("TowerDragGhost");
            ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
        }

        ghostRenderer.sprite = boardTower.SpriteRenderer.sprite;
        ghostRenderer.sortingLayerID = boardTower.SpriteRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = boardTower.SpriteRenderer.sortingOrder + 20;
        ghostRenderer.transform.position = boardTower.transform.position;
        ghostRenderer.transform.localScale = boardTower.SpriteRenderer.transform.lossyScale;

        Color color = boardTower.SpriteRenderer.color;
        color.a = ghostAlpha;
        ghostRenderer.color = color;
        ghostRenderer.gameObject.SetActive(true);
    }

    private void ClearDragState()
    {
        pointerDown = false;
        dragging = false;

        if (ghostRenderer != null)
        {
            if (Application.isPlaying)
                Destroy(ghostRenderer.gameObject);
            else
                DestroyImmediate(ghostRenderer.gameObject);

            ghostRenderer = null;
        }
    }

    private static bool HasCentralInputController()
    {
        BoardTowerInputController controller = UnityEngine.Object.FindFirstObjectByType<BoardTowerInputController>();
        return controller != null && controller.isActiveAndEnabled;
    }
}
