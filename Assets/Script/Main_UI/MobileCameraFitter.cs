using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class MobileCameraFitter : MonoBehaviour
{
    public Vector2 referenceResolution = new Vector2(1080f, 1920f);
    public float referenceOrthographicSize = 9.3929405f;
    public float maxOrthographicSize = 13.5f;

    private Camera targetCamera;
    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        Refresh(true);
    }

    private void OnEnable()
    {
        Refresh(true);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    public void Refresh(bool force)
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        if (!force && Screen.width == lastWidth && Screen.height == lastHeight)
            return;

        lastWidth = Screen.width;
        lastHeight = Screen.height;

        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (targetCamera == null || !targetCamera.orthographic)
            return;

        float designAspect = referenceResolution.x / referenceResolution.y;
        float deviceAspect = (float)Screen.width / Screen.height;
        float fittedSize = referenceOrthographicSize;

        if (deviceAspect < designAspect)
            fittedSize = referenceOrthographicSize * (designAspect / deviceAspect);

        targetCamera.orthographicSize = Mathf.Min(fittedSize, maxOrthographicSize);
    }
}
