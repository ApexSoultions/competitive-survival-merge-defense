using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerInfoUI : MonoBehaviour
{
    public static TowerInfoUI Instance { get; private set; }

    [SerializeField] private RectTransform popup;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private Vector2 popupSize = new Vector2(430f, 150f);
    [SerializeField] private Vector2 canvasPadding = new Vector2(24f, 24f);
    [SerializeField] private Color backgroundColor = new Color(0.02f, 0.08f, 0.04f, 0.94f);

    private Camera mainCamera;
    private Canvas parentCanvas;
    private RectTransform canvasRect;
    private BoardTower currentTower;

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
        parentCanvas = popup != null ? popup.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
        canvasRect = parentCanvas != null ? parentCanvas.transform as RectTransform : null;

        ConfigurePopupVisuals();

        if (popup != null)
            popup.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (popup == null || !popup.gameObject.activeSelf)
            return;

        if (currentTower == null)
        {
            Hide();
            return;
        }

        PositionPopup(currentTower);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(BoardTower tower)
    {
        if (tower == null || popup == null || infoText == null)
            return;

        currentTower = tower;
        mainCamera = mainCamera != null ? mainCamera : Camera.main;

        ConfigurePopupVisuals();
        popup.gameObject.SetActive(true);
        popup.SetAsLastSibling();

        infoText.text = BuildInfoText(tower);
        PositionPopup(tower);
    }

    public void Hide()
    {
        currentTower = null;

        if (popup != null)
            popup.gameObject.SetActive(false);
    }

    private void ConfigurePopupVisuals()
    {
        if (popup == null)
            return;

        popup.localRotation = Quaternion.identity;
        popup.localScale = Vector3.one;
        popup.pivot = new Vector2(0.5f, 0f);
        popup.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, popupSize.x);
        popup.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, popupSize.y);

        Image background = popup.GetComponentInChildren<Image>(true);

        if (background == null)
        {
            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(popup, false);
            background = backgroundObject.AddComponent<Image>();
        }

        background.gameObject.SetActive(true);
        background.color = backgroundColor;
        background.raycastTarget = false;
        background.transform.SetAsFirstSibling();

        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.localRotation = Quaternion.identity;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        if (infoText == null)
            return;

        RectTransform textRect = infoText.rectTransform;
        textRect.localRotation = Quaternion.identity;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 12f);
        textRect.offsetMax = new Vector2(-18f, -12f);

        infoText.raycastTarget = false;
        infoText.color = Color.white;
        infoText.alignment = TextAlignmentOptions.Center;
        infoText.textWrappingMode = TextWrappingModes.Normal;
        infoText.enableAutoSizing = true;
        infoText.fontSizeMin = 18f;
        infoText.fontSizeMax = 32f;
        infoText.overflowMode = TextOverflowModes.Ellipsis;

        Shadow textShadow = infoText.GetComponent<Shadow>();

        if (textShadow == null)
            textShadow = infoText.gameObject.AddComponent<Shadow>();

        textShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        textShadow.effectDistance = new Vector2(2f, -2f);
    }

    private string BuildInfoText(BoardTower tower)
    {
        UnitData unitData = tower.UnitData;
        string unitName = unitData != null && !string.IsNullOrWhiteSpace(unitData.unitName)
            ? unitData.unitName
            : tower.gameObject.name;

        if (unitData == null)
            return "<b>" + unitName + "</b>\nLv. " + tower.Level;

        return string.Format(
            "<b>{0}</b>  <color=#FFE27A>Lv. {1}</color>\nDamage {2}   Range {3:0.#}\nSpeed {4:0.#}/s   Mana {5}",
            unitName,
            tower.Level,
            unitData.attackDamage,
            unitData.attackRange,
            unitData.attackSpeed,
            unitData.manaCost
        );
    }

    private void PositionPopup(BoardTower tower)
    {
        if (popup == null || tower == null)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Vector3 anchorPosition = GetUnitHeadPosition(tower) + worldOffset;
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(anchorPosition);

        if (screenPosition.z < 0f)
        {
            Hide();
            return;
        }

        if (canvasRect == null || parentCanvas == null)
        {
            popup.position = screenPosition;
            return;
        }

        Camera canvasCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : parentCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, canvasCamera, out Vector2 localPoint))
            return;

        Rect canvasBounds = canvasRect.rect;
        Vector2 popupBounds = popup.rect.size;

        float minX = canvasBounds.xMin + canvasPadding.x + popupBounds.x * popup.pivot.x;
        float maxX = canvasBounds.xMax - canvasPadding.x - popupBounds.x * (1f - popup.pivot.x);
        float minY = canvasBounds.yMin + canvasPadding.y + popupBounds.y * popup.pivot.y;
        float maxY = canvasBounds.yMax - canvasPadding.y - popupBounds.y * (1f - popup.pivot.y);

        localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
        localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

        popup.anchoredPosition = localPoint;
    }

    private static Vector3 GetUnitHeadPosition(BoardTower tower)
    {
        SpriteRenderer unitRenderer = tower.SpriteRenderer;

        if (unitRenderer == null || unitRenderer.sprite == null)
            return tower.transform.position;

        Bounds visualBounds = unitRenderer.bounds;
        return new Vector3(
            visualBounds.center.x,
            visualBounds.max.y,
            tower.transform.position.z
        );
    }
}
