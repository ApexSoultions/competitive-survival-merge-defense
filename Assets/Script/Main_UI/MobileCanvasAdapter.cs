using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasScaler))]
public class MobileCanvasAdapter : MonoBehaviour
{
    [Header("Canvas Scale")]
    public Vector2 referenceResolution = new Vector2(1080f, 1920f);
    public bool adaptCanvasScaler = true;
    public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
    [Range(0f, 1f)] public float matchWidthOrHeight = 0.5f;

    [Header("Safe Area")]
    public bool applySafeArea = true;
    public RectTransform[] safeAreaRoots;

    [Header("Bottom Bleed")]
    [Tooltip("Bottom-anchored footers that stay on the physical screen bottom and grow by the home-indicator inset.")]
    public RectTransform[] bottomBleedRoots;

    [Header("Full Screen Visuals")]
    public bool stretchFullScreenRects = true;
    public RectTransform[] fullScreenRects;

    private CanvasScaler canvasScaler;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private float[] bottomBleedBaseHeights;

    private void Awake()
    {
        Refresh(true);
    }

    private void OnEnable()
    {
        Refresh(true);
    }

    private void OnRectTransformDimensionsChange()
    {
        Refresh(false);
    }

    private void Update()
    {
        Refresh(false);
    }

    public void Refresh(bool force)
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        Rect safeArea = GetValidSafeArea();

        if (!force && screenSize == lastScreenSize && Approximately(safeArea, lastSafeArea))
            return;

        lastScreenSize = screenSize;
        lastSafeArea = safeArea;

        ConfigureCanvasScaler();
        ApplySafeArea(safeArea);
        ApplyBottomBleed(safeArea);
        StretchFullScreenRects(safeArea);
    }

    private void ConfigureCanvasScaler()
    {
        if (!adaptCanvasScaler)
            return;

        if (canvasScaler == null)
            canvasScaler = GetComponent<CanvasScaler>();

        if (canvasScaler == null)
            return;

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = screenMatchMode;
        canvasScaler.matchWidthOrHeight = matchWidthOrHeight;
    }

    private struct RootLayoutInfo
    {
        public RectTransform rectTransform;
        public Vector2 originalAnchorMin;
        public Vector2 originalAnchorMax;
        public bool isInitialized;
    }

    private RootLayoutInfo[] layoutInfos;

    private void ApplySafeArea(Rect safeArea)
    {
        if (!applySafeArea || safeAreaRoots == null)
            return;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        if (layoutInfos == null || layoutInfos.Length != safeAreaRoots.Length)
        {
            layoutInfos = new RootLayoutInfo[safeAreaRoots.Length];
            for (int i = 0; i < safeAreaRoots.Length; i++)
            {
                if (safeAreaRoots[i] != null)
                {
                    layoutInfos[i] = new RootLayoutInfo
                    {
                        rectTransform = safeAreaRoots[i],
                        originalAnchorMin = safeAreaRoots[i].anchorMin,
                        originalAnchorMax = safeAreaRoots[i].anchorMax,
                        isInitialized = true
                    };
                }
            }
        }

        for (int i = 0; i < safeAreaRoots.Length; i++)
        {
            RectTransform root = safeAreaRoots[i];
            if (root == null)
                continue;

            Vector2 origMin = Vector2.zero;
            Vector2 origMax = Vector2.one;
            bool found = false;

            if (layoutInfos != null && i < layoutInfos.Length && layoutInfos[i].rectTransform == root && layoutInfos[i].isInitialized)
            {
                origMin = layoutInfos[i].originalAnchorMin;
                origMax = layoutInfos[i].originalAnchorMax;
                found = true;
            }

            if (!found)
            {
                origMin = root.anchorMin;
                origMax = root.anchorMax;
            }

            root.anchorMin = new Vector2(
                Mathf.Lerp(anchorMin.x, anchorMax.x, origMin.x),
                Mathf.Lerp(anchorMin.y, anchorMax.y, origMin.y)
            );
            root.anchorMax = new Vector2(
                Mathf.Lerp(anchorMin.x, anchorMax.x, origMax.x),
                Mathf.Lerp(anchorMin.y, anchorMax.y, origMax.y)
            );
        }
    }

    private void ApplyBottomBleed(Rect safeArea)
    {
        if (bottomBleedRoots == null || bottomBleedRoots.Length == 0)
            return;

        CaptureBottomBleedBaseHeights();

        float scaleFactor = 1f;
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.scaleFactor > 0.0001f)
            scaleFactor = canvas.scaleFactor;

        float bottomInsetUnits = Mathf.Max(0f, safeArea.yMin) / scaleFactor;

        for (int i = 0; i < bottomBleedRoots.Length; i++)
        {
            RectTransform root = bottomBleedRoots[i];
            if (root == null)
                continue;

            float baseHeight = bottomBleedBaseHeights != null && i < bottomBleedBaseHeights.Length
                ? bottomBleedBaseHeights[i]
                : root.sizeDelta.y;

            Vector2 sizeDelta = root.sizeDelta;
            sizeDelta.y = baseHeight + bottomInsetUnits;
            root.sizeDelta = sizeDelta;
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(root.anchoredPosition.x, 0f);
        }
    }

    private void CaptureBottomBleedBaseHeights()
    {
        if (bottomBleedRoots == null)
            return;

        if (bottomBleedBaseHeights != null && bottomBleedBaseHeights.Length == bottomBleedRoots.Length)
            return;

        bottomBleedBaseHeights = new float[bottomBleedRoots.Length];
        for (int i = 0; i < bottomBleedRoots.Length; i++)
        {
            RectTransform root = bottomBleedRoots[i];
            bottomBleedBaseHeights[i] = root != null ? root.sizeDelta.y : 0f;
        }
    }

    private void StretchFullScreenRects(Rect safeArea)
    {
        if (!stretchFullScreenRects || fullScreenRects == null)
            return;

        for (int i = 0; i < fullScreenRects.Length; i++)
        {
            RectTransform rect = fullScreenRects[i];
            if (rect == null)
                continue;

            Vector2 anchorMin = Vector2.zero;
            Vector2 anchorMax = Vector2.one;

            if (IsInsideSafeAreaRoot(rect))
                GetBleedAnchors(safeArea, out anchorMin, out anchorMax);

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
        }
    }

    private static Rect GetValidSafeArea()
    {
        Rect safeArea = Screen.safeArea;
        if (safeArea.width <= 0f || safeArea.height <= 0f)
            return new Rect(0f, 0f, Screen.width, Screen.height);

        return safeArea;
    }

    private bool IsInsideSafeAreaRoot(RectTransform rect)
    {
        if (safeAreaRoots == null)
            return false;

        Transform current = rect.parent;
        while (current != null)
        {
            for (int i = 0; i < safeAreaRoots.Length; i++)
            {
                if (current == safeAreaRoots[i])
                    return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static void GetBleedAnchors(Rect safeArea, out Vector2 anchorMin, out Vector2 anchorMax)
    {
        float safeMinX = safeArea.xMin / Screen.width;
        float safeMinY = safeArea.yMin / Screen.height;
        float safeWidth = safeArea.width / Screen.width;
        float safeHeight = safeArea.height / Screen.height;

        if (safeWidth <= 0f || safeHeight <= 0f)
        {
            anchorMin = Vector2.zero;
            anchorMax = Vector2.one;
            return;
        }

        anchorMin = new Vector2(-safeMinX / safeWidth, -safeMinY / safeHeight);
        anchorMax = new Vector2((1f - safeMinX) / safeWidth, (1f - safeMinY) / safeHeight);
    }

    private static bool Approximately(Rect a, Rect b)
    {
        return Mathf.Approximately(a.x, b.x)
            && Mathf.Approximately(a.y, b.y)
            && Mathf.Approximately(a.width, b.width)
            && Mathf.Approximately(a.height, b.height);
    }
}
