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

    public Vector3 GetManaVfxWorldPosition(Vector3 fallback)
    {
        if (manaText == null)
            return fallback;

        Canvas canvas = manaText.GetComponentInParent<Canvas>();
        Camera camera = Camera.main;
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay && camera != null)
        {
            Vector3 screenPosition = manaText.rectTransform.position;
            screenPosition.z = Mathf.Abs(camera.transform.position.z);
            Vector3 world = camera.ScreenToWorldPoint(screenPosition);
            world.z = fallback.z;
            return world;
        }

        return manaText.transform.position;
    }
}
