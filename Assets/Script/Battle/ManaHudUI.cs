using TMPro;
using UnityEngine;

/// <summary>
/// Footer mana readout. Lives on ManaPanel so the number still updates when TopUIRoot is hidden.
/// </summary>
public class ManaHudUI : MonoBehaviour
{
    public static ManaHudUI Instance { get; private set; }

    [SerializeField] private TMP_Text manaText;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        Instance = this;
        ManaManager.OnManaChanged += HandleManaChanged;
        SyncFromManager();
    }

    private void OnDisable()
    {
        ManaManager.OnManaChanged -= HandleManaChanged;
        if (Instance == this)
            Instance = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void SyncFromManager()
    {
        if (ManaManager.Instance != null)
            HandleManaChanged(ManaManager.Instance.CurrentMana);
    }

    private void HandleManaChanged(int newMana)
    {
        if (manaText != null)
            manaText.text = newMana.ToString();
    }

    /// <summary>
    /// World position for mana orb VFX — ManaPanel center on the gameplay plane.
    /// Returns <paramref name="fallback"/> if the HUD projects outside the bottom-left screen region.
    /// </summary>
    public Vector3 GetManaVfxWorldPosition(Vector3 fallback)
    {
        RectTransform panel = transform as RectTransform;
        if (panel == null && manaText != null)
            panel = manaText.rectTransform;
        if (panel == null)
            return fallback;

        if (!TryGetHudScreenCenter(panel, out Vector2 screenCenter))
            return fallback;

        // Mana counter is bottom-left footer — reject wrong-side / off-map projections.
        if (screenCenter.x > Screen.width * 0.45f || screenCenter.y > Screen.height * 0.45f)
            return fallback;

        Vector3 world = CanvasMapSpace.ScreenToGameplayWorld(screenCenter);
        if (!IsFinite(world))
            return fallback;

        world.z = fallback.z;
        return world;
    }

    private static bool TryGetHudScreenCenter(RectTransform panel, out Vector2 screenCenter)
    {
        screenCenter = default;
        Camera camera = Camera.main;
        if (panel == null || camera == null)
            return false;

        Vector3[] corners = new Vector3[4];
        panel.GetWorldCorners(corners);

        Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 b = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        screenCenter = (a + b) * 0.5f;
        return float.IsFinite(screenCenter.x) && float.IsFinite(screenCenter.y);
    }

    private static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
