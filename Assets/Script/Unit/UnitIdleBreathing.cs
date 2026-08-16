using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class UnitIdleBreathing : MonoBehaviour
{
    private const string VisualObjectName = "Visual";
    private const string ArtObjectName = "VisualArt";
    private const string BreathingPivotName = "IdleBreathingPivot";

    [Header("Idle Breathing")]
    [SerializeField, Min(0.5f)] private float cycleDuration = 2.35f;
    [SerializeField, Range(0f, 0.08f)] private float verticalExpansion = 0.022f;
    [SerializeField, Range(0f, 0.05f)] private float horizontalCompression = 0.008f;
    [SerializeField, Range(0f, 0.08f)] private float liftAmount = 0.014f;
    [SerializeField, Range(0f, 2f)] private float swayDegrees = 0.22f;

    private Transform breathingPivot;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 baseLocalScale;
    private float phaseOffset;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (initialized)
            return;

        Transform visual = transform.Find(VisualObjectName);
        if (visual == null)
        {
            SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>(true);
            visual = renderer != null ? renderer.transform : null;
        }

        if (visual == null)
        {
            enabled = false;
            return;
        }

        breathingPivot = FindOrCreateBreathingPivot(visual);
        baseLocalPosition = breathingPivot.localPosition;
        baseLocalRotation = breathingPivot.localRotation;
        baseLocalScale = breathingPivot.localScale;

        int positiveId = GetInstanceID() & 0x7fffffff;
        phaseOffset = (positiveId % 1000) * (Mathf.PI * 2f / 1000f);
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized || breathingPivot == null)
            return;

        float phase = Time.time * (Mathf.PI * 2f / Mathf.Max(0.5f, cycleDuration)) + phaseOffset;
        float breath = Mathf.Sin(phase);
        float sway = Mathf.Sin(phase * 0.5f + phaseOffset * 0.37f);

        breathingPivot.localPosition = baseLocalPosition + Vector3.up * (breath * liftAmount);
        breathingPivot.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, sway * swayDegrees);
        breathingPivot.localScale = Vector3.Scale(
            baseLocalScale,
            new Vector3(
                1f - breath * horizontalCompression,
                1f + breath * verticalExpansion,
                1f));
    }

    private void OnDisable()
    {
        RestoreBasePose();
    }

    private void OnDestroy()
    {
        RestoreBasePose();
    }

    private void RestoreBasePose()
    {
        if (!initialized || breathingPivot == null)
            return;

        breathingPivot.localPosition = baseLocalPosition;
        breathingPivot.localRotation = baseLocalRotation;
        breathingPivot.localScale = baseLocalScale;
    }

    private Transform FindOrCreateBreathingPivot(Transform visual)
    {
        Transform existingPivot = visual.Find(BreathingPivotName);
        if (visual.name == VisualObjectName && existingPivot != null)
            return existingPivot;

        Transform originalParent = visual.parent;
        Vector3 originalPosition = visual.localPosition;
        Quaternion originalRotation = visual.localRotation;
        Vector3 originalScale = visual.localScale;

        GameObject visualContainerObject = new GameObject(VisualObjectName);
        Transform visualContainer = visualContainerObject.transform;
        visualContainer.SetParent(originalParent, false);
        visualContainer.localPosition = originalPosition;
        visualContainer.localRotation = originalRotation;
        visualContainer.localScale = originalScale;

        GameObject pivotObject = new GameObject(BreathingPivotName);
        Transform pivot = pivotObject.transform;
        pivot.SetParent(visualContainer, false);

        visual.name = ArtObjectName;
        visual.SetParent(pivot, false);
        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one;

        return pivot;
    }
}
