using UnityEngine;

/// <summary>
/// Inspector knobs for the 5x5 board. Put this on Arena_Grid.
/// Do not move Cell_1–Cell_25 by hand — they snap from this layout.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class BoardGridLayout : MonoBehaviour
{
    public const int GridSize = 5;

    public static int Revision { get; private set; }

    [Header("Fit cells inside painted squares")]
    [Tooltip("Changing this copies the same value into Left/Right/Top/Bottom.")]
    [Range(0f, 0.4f)]
    [SerializeField] private float insetAll = 0.10f;

    [Range(0f, 0.4f)]
    [SerializeField] private float insetLeft = 0.10f;

    [Range(0f, 0.4f)]
    [SerializeField] private float insetRight = 0.10f;

    [Range(0f, 0.4f)]
    [SerializeField] private float insetTop = 0.10f;

    [Range(0f, 0.4f)]
    [SerializeField] private float insetBottom = 0.10f;

    [Header("Center spawn in each box (fraction of one cell)")]
    [Tooltip("0 = geometric center. +X moves right, +Y moves up. 0.15 = 15% of a cell.")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float centerOffsetX = 0.15f;

    [Range(-0.5f, 0.5f)]
    [SerializeField] private float centerOffsetY = 0.15f;

    [Header("Tap / merge")]
    [Range(0.5f, 1f)]
    [SerializeField] private float colliderFill = 0.85f;

    private RectTransform rectTransform;
    [SerializeField, HideInInspector] private float lastInsetAll = 0.10f;
    private int lastAppliedHash;

    private static BoardGridLayout cached;

    public float ColliderFill => colliderFill;

    private void OnEnable()
    {
        cached = this;
        rectTransform = transform as RectTransform;
        lastInsetAll = insetAll;
        BumpRevision();
    }

    private void OnDisable()
    {
        if (cached == this)
            cached = null;
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        // Avoid treating domain-reload defaults as an Inset All edit when entering Play Mode.
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            lastInsetAll = insetAll;
            BumpRevision();
            return;
        }
#endif

        if (!Mathf.Approximately(insetAll, lastInsetAll))
        {
            insetLeft = insetRight = insetTop = insetBottom = insetAll;
            lastInsetAll = insetAll;
        }

        BumpRevision();
    }

    private void LateUpdate()
    {
        int hash = ComputeStateHash();
        if (hash == lastAppliedHash)
            return;

        lastAppliedHash = hash;
        BumpRevision();
    }

    public static BoardGridLayout FindActive()
    {
        if (cached != null)
            return cached;

        GameObject gridObject = GameObject.Find("Arena_Grid");
        cached = gridObject != null ? gridObject.GetComponent<BoardGridLayout>() : null;
        return cached;
    }

    public bool TryGetCellWorldPosition(int row, int column, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (row < 0 || row >= GridSize || column < 0 || column >= GridSize)
            return false;

        float innerWidth = 1f - insetLeft - insetRight;
        float innerHeight = 1f - insetTop - insetBottom;

        if (innerWidth <= 0.01f || innerHeight <= 0.01f)
            return false;

        float cellWidth = innerWidth / GridSize;
        float cellHeight = innerHeight / GridSize;

        float u = insetLeft + (column + 0.5f) * cellWidth + centerOffsetX * cellWidth;
        float vFromBottom = insetBottom + (GridSize - 1 - row + 0.5f) * cellHeight + centerOffsetY * cellHeight;

        return CanvasMapSpace.TryGetRectNormalizedWorldPosition(GetRect(), u, 1f - vFromBottom, out worldPosition);
    }

    public bool TryGetCellWorldSize(out Vector2 worldSize)
    {
        worldSize = Vector2.zero;

        if (!TryGetCellWorldPosition(0, 0, out Vector3 topLeft) ||
            !TryGetCellWorldPosition(0, 1, out Vector3 topNext) ||
            !TryGetCellWorldPosition(1, 0, out Vector3 nextRow))
        {
            return false;
        }

        worldSize = new Vector2(
            Vector3.Distance(topLeft, topNext),
            Vector3.Distance(topLeft, nextRow)
        ) * colliderFill;

        return worldSize.x > 0.01f && worldSize.y > 0.01f;
    }

    private RectTransform GetRect()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        return rectTransform;
    }

    private int ComputeStateHash()
    {
        RectTransform rect = GetRect();
        Vector3 pos = rect != null ? rect.position : Vector3.zero;
        Vector2 size = rect != null ? rect.rect.size : Vector2.zero;

        return pos.x.GetHashCode()
            ^ (pos.y.GetHashCode() << 2)
            ^ (size.x.GetHashCode() << 4)
            ^ (size.y.GetHashCode() << 6)
            ^ insetLeft.GetHashCode()
            ^ (insetRight.GetHashCode() << 1)
            ^ (insetTop.GetHashCode() << 3)
            ^ (insetBottom.GetHashCode() << 5)
            ^ centerOffsetX.GetHashCode()
            ^ (centerOffsetY.GetHashCode() << 7)
            ^ colliderFill.GetHashCode();
    }

    private static void BumpRevision()
    {
        Revision++;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);

        for (int row = 0; row < GridSize; row++)
        {
            for (int column = 0; column < GridSize; column++)
            {
                if (!TryGetCellWorldPosition(row, column, out Vector3 worldPosition))
                    continue;

                Gizmos.DrawWireSphere(worldPosition, 0.06f);
            }
        }
    }
#endif
}
