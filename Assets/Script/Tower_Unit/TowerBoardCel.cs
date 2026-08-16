using System;
using UnityEngine;

public class TowerBoardCell : MonoBehaviour
{
    public static event Action BoardChanged;

    [SerializeField] private Transform towerSpawnPoint;
    [SerializeField] private bool alignToCanvasMapGrid = false;
    [SerializeField] private bool useCanvasMapGridForSpawn = true;
    [SerializeField] private bool useCanvasCellCenterForSpawn = true;
    [SerializeField] private bool keepPlacedTowerOnCellCenter = true;
    [SerializeField] private float resnapDistance = 0.01f;

    private BoardTower currentTower;
    private Vector3 lastSpawnPosition;
    private bool hasLastSpawnPosition;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private float lastCameraOrthographicSize = -1f;

    private int lastGridRevision = int.MinValue;

    public bool IsOccupied => currentTower != null;
    public BoardTower CurrentTower => currentTower;
    public Vector3 SpawnPosition => GetSpawnPosition();

    private void Awake()
    {
        if (towerSpawnPoint == null)
            towerSpawnPoint = transform;

        CacheSpawnPosition();
    }

    private void Start()
    {
        AlignTransformToCanvasMapGrid();
        ApplyCellColliderSize();
        SnapCurrentTowerToCell();
    }

    private void LateUpdate()
    {
        if (!HasLayoutStateChanged())
            return;

        AlignTransformToCanvasMapGrid();
        ApplyCellColliderSize();

        Vector3 spawnPosition = SpawnPosition;

        CacheSpawnPosition(spawnPosition);

        if (!keepPlacedTowerOnCellCenter || currentTower == null)
            return;

        if (!hasLastSpawnPosition ||
            (currentTower.transform.position - spawnPosition).sqrMagnitude > resnapDistance * resnapDistance)
        {
            SnapCurrentTowerToCell(spawnPosition);
        }
    }

    private void OnValidate()
    {
        if (towerSpawnPoint == null)
            towerSpawnPoint = transform;
    }

    public bool PlaceTower(GameObject towerPrefab, UnitData unitData, int level)
    {
        if (!BattleFlowState.IsGameplayActive || towerPrefab == null || unitData == null || IsOccupied)
            return false;

        if (level < 1 || level > UnitData.MaximumLevel || unitData.GetPrefabExact(level) != towerPrefab)
        {
            UnityEngine.Debug.LogWarning("Tower placement rejected: prefab does not match " + unitData.unitName + " Lv" + level + ".");
            return false;
        }

        Vector3 spawnPosition = SpawnPosition;
        GameObject towerObject = Instantiate(towerPrefab, spawnPosition, Quaternion.identity);

        BoardTower boardTower = towerObject.GetComponent<BoardTower>();

        if (boardTower == null)
            boardTower = towerObject.AddComponent<BoardTower>();

        boardTower.Initialize(unitData, level, this);

        currentTower = boardTower;
        towerObject.transform.position = spawnPosition;
        CacheSpawnPosition(spawnPosition);

        NotifyBoardChanged();
        return true;
    }

    public void ClearCell(BoardTower tower)
    {
        if (currentTower == tower)
        {
            currentTower = null;
            NotifyBoardChanged();
        }
    }

    public bool SetTower(BoardTower tower)
    {
        if (tower == null)
        {
            bool hadTower = currentTower != null;
            currentTower = null;

            if (hadTower)
                NotifyBoardChanged();

            return true;
        }

        if (IsOccupied && currentTower != tower)
            return false;

        currentTower = tower;
        tower.SetCell(this);
        SnapCurrentTowerToCell();

        NotifyBoardChanged();
        return true;
    }

    public void SnapCurrentTowerToCell()
    {
        SnapCurrentTowerToCell(SpawnPosition);
    }

    private void SnapCurrentTowerToCell(Vector3 spawnPosition)
    {
        CacheSpawnPosition(spawnPosition);

        if (currentTower != null)
            currentTower.transform.position = spawnPosition;
    }

    [ContextMenu("Align To Canvas Map Grid Once")]
    private void AlignToCanvasMapGrid()
    {
        if (!alignToCanvasMapGrid)
            return;

        AlignTransformToCanvasMapGrid();
    }

    private void AlignTransformToCanvasMapGrid()
    {
        if (!alignToCanvasMapGrid && !useCanvasMapGridForSpawn)
            return;

        CanvasMapSpace.ConfigureMapCanvasForGameplay();

        if (CanvasMapSpace.TryGetBoardCellWorldPosition(gameObject.name, out Vector3 worldPosition))
            transform.position = worldPosition;
    }

    private void ApplyCellColliderSize()
    {
        BoardGridLayout gridLayout = BoardGridLayout.FindActive();
        if (gridLayout == null || !gridLayout.TryGetCellWorldSize(out Vector2 worldSize))
            return;

        if (!TryGetComponent(out BoxCollider2D boxCollider))
            return;

        boxCollider.size = worldSize;
        boxCollider.offset = Vector2.zero;
    }

    private Vector3 GetSpawnPosition()
    {
        if (useCanvasMapGridForSpawn &&
            CanvasMapSpace.TryGetBoardCellWorldPosition(gameObject.name, out Vector3 gridPosition))
        {
            return gridPosition;
        }

        Transform source = towerSpawnPoint != null ? towerSpawnPoint : transform;

        if (useCanvasCellCenterForSpawn && source.GetComponentInParent<Canvas>() != null)
            return CanvasMapSpace.TransformToGameplayWorld(source);

        return source.position;
    }

    private void CacheSpawnPosition()
    {
        CacheSpawnPosition(SpawnPosition);
    }

    private void CacheSpawnPosition(Vector3 spawnPosition)
    {
        lastSpawnPosition = spawnPosition;
        hasLastSpawnPosition = true;
        CacheLayoutState();
    }

    private bool HasLayoutStateChanged()
    {
        float cameraSize = GetCameraOrthographicSize();

        return Screen.width != lastScreenWidth ||
               Screen.height != lastScreenHeight ||
               lastGridRevision != BoardGridLayout.Revision ||
               !Mathf.Approximately(cameraSize, lastCameraOrthographicSize);
    }

    private void CacheLayoutState()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastCameraOrthographicSize = GetCameraOrthographicSize();
        lastGridRevision = BoardGridLayout.Revision;
    }

    private static float GetCameraOrthographicSize()
    {
        Camera mainCamera = Camera.main;
        return mainCamera != null && mainCamera.orthographic ? mainCamera.orthographicSize : 0f;
    }

    private static void NotifyBoardChanged()
    {
        BoardChanged?.Invoke();
    }
}
