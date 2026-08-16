using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public sealed class AoERadiusRingVisual : MonoBehaviour
{
    private const int SegmentCount = 56;

    [SerializeField] private LineRenderer lineRenderer;

    public LineRenderer Renderer => lineRenderer;

    private void Awake()
    {
        EnsureConfigured();
    }

    public void EnsureConfigured()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = SegmentCount;
        lineRenderer.numCornerVertices = 2;
        lineRenderer.numCapVertices = 2;
        lineRenderer.widthMultiplier = 0.075f;
        lineRenderer.sortingLayerName = "Tower";
        lineRenderer.sortingOrder = 69;

        for (int i = 0; i < SegmentCount; i++)
        {
            float angle = i * Mathf.PI * 2f / SegmentCount;
            lineRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
        }
    }
}
