using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckSelectionSlotUI : MonoBehaviour
{
    [Header("Unit")]
    [SerializeField] private UnitData unitData = null;
    [SerializeField] private string displayNameOverride = "";

    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Image iconImage = null;
    [SerializeField] private GameObject selectedBorder = null;
    [SerializeField] private TMP_Text nameText = null;
    [SerializeField] private Button button = null;

    [Header("Nameplate")]
    [SerializeField] private Color nameplateColor = new Color(0.02f, 0.03f, 0.025f, 0.88f);
    [SerializeField] private Color nameplateAccentColor = new Color(1f, 0.78f, 0.22f, 0.95f);
    [SerializeField] private float nameplateHeight = 38f;

    private bool isSelected;
    private Image nameplateImage;
    private Image nameplateAccentImage;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (iconImage == null)
            iconImage = GetComponent<UnityEngine.UI.Image>();

        if (button != null)
            button.onClick.AddListener(ToggleSelect);

        ConfigureVisuals();
        Refresh();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(ToggleSelect);
    }

    private void Refresh()
    {
        ConfigureVisuals();

        if (unitData != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = unitData.GetIcon(1);
                iconImage.preserveAspect = false;
            }

            if (nameText != null)
            {
                string displayName = string.IsNullOrWhiteSpace(displayNameOverride)
                    ? unitData.unitName
                    : displayNameOverride;
                nameText.text = FormatDisplayName(displayName);
            }
        }

        SetSelectedVisual(false);
    }

    private void ToggleSelect()
    {
        if (DeckSelectionManager.Instance == null || unitData == null)
            return;

        isSelected = DeckSelectionManager.Instance.ToggleUnit(unitData);
        SetSelectedVisual(isSelected);
    }

    private void SetSelectedVisual(bool selected)
    {
        if (selectedBorder != null)
            selectedBorder.SetActive(selected);
    }

    private void ConfigureVisuals()
    {
        ConfigureRootBackground();
        ConfigureIcon();
        ConfigureNameplate();
        ConfigureNameText();
        ConfigureSelectedBorder();
    }

    private void ConfigureRootBackground()
    {
        Image rootImage = GetComponent<Image>();

        if (rootImage == null)
            return;

        rootImage.color = new Color(0.03f, 0.06f, 0.04f, 0.95f);
        rootImage.raycastTarget = true;
    }

    private void ConfigureIcon()
    {
        if (iconImage == null)
            return;

        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(6f, 6f);
        iconRect.offsetMax = new Vector2(-6f, -6f);

        iconImage.color = Color.white;
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = false;
    }

    private void ConfigureNameplate()
    {
        if (nameText == null)
            return;

        if (nameplateImage == null)
        {
            Transform existing = transform.Find("Nameplate_Background");

            if (existing != null)
                nameplateImage = existing.GetComponent<Image>();

            if (nameplateImage == null)
            {
                GameObject nameplateObject = new GameObject("Nameplate_Background");
                nameplateObject.transform.SetParent(transform, false);
                nameplateImage = nameplateObject.AddComponent<Image>();
            }
        }

        RectTransform nameplateRect = nameplateImage.rectTransform;
        nameplateRect.anchorMin = new Vector2(0f, 0f);
        nameplateRect.anchorMax = new Vector2(1f, 0f);
        nameplateRect.pivot = new Vector2(0.5f, 0f);
        nameplateRect.anchoredPosition = new Vector2(0f, 6f);
        nameplateRect.sizeDelta = new Vector2(-12f, nameplateHeight);

        nameplateImage.color = nameplateColor;
        nameplateImage.raycastTarget = false;
        nameplateImage.transform.SetSiblingIndex(Mathf.Max(0, nameText.transform.GetSiblingIndex() - 1));

        if (nameplateAccentImage == null)
        {
            Transform existingAccent = nameplateImage.transform.Find("Accent");

            if (existingAccent != null)
                nameplateAccentImage = existingAccent.GetComponent<Image>();

            if (nameplateAccentImage == null)
            {
                GameObject accentObject = new GameObject("Accent");
                accentObject.transform.SetParent(nameplateImage.transform, false);
                nameplateAccentImage = accentObject.AddComponent<Image>();
            }
        }

        RectTransform accentRect = nameplateAccentImage.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 3f);

        nameplateAccentImage.color = nameplateAccentColor;
        nameplateAccentImage.raycastTarget = false;
    }

    private void ConfigureNameText()
    {
        if (nameText == null)
            return;

        RectTransform textRect = nameText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = new Vector2(0f, 8f);
        textRect.sizeDelta = new Vector2(-16f, nameplateHeight - 4f);

        nameText.transform.SetAsLastSibling();
        nameText.raycastTarget = false;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontStyle = FontStyles.Bold;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 11f;
        nameText.fontSizeMax = 22f;

        Shadow shadow = nameText.GetComponent<Shadow>();

        if (shadow == null)
            shadow = nameText.gameObject.AddComponent<Shadow>();

        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    private void ConfigureSelectedBorder()
    {
        if (selectedBorder == null)
            return;

        RectTransform selectedRect = selectedBorder.transform as RectTransform;

        if (selectedRect != null)
        {
            selectedRect.anchorMin = Vector2.zero;
            selectedRect.anchorMax = Vector2.one;
            selectedRect.offsetMin = new Vector2(2f, 2f);
            selectedRect.offsetMax = new Vector2(-2f, -2f);
        }

        Image selectedImage = selectedBorder.GetComponent<Image>();

        if (selectedImage != null)
        {
            selectedImage.color = new Color(1f, 0.82f, 0.16f, 0.22f);
            selectedImage.raycastTarget = false;
        }

        Outline selectedOutline = selectedBorder.GetComponent<Outline>();

        if (selectedOutline == null)
            selectedOutline = selectedBorder.AddComponent<Outline>();

        selectedOutline.effectColor = new Color(1f, 0.9f, 0.22f, 1f);
        selectedOutline.effectDistance = new Vector2(4f, -4f);

        if (nameplateImage != null)
            selectedBorder.transform.SetSiblingIndex(Mathf.Max(0, nameplateImage.transform.GetSiblingIndex() - 1));
        else
            selectedBorder.transform.SetAsLastSibling();
    }

    private string FormatDisplayName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return string.Empty;

        string displayName = rawName.Replace("_Data", string.Empty).Replace("_", " ").Replace("-", " ").Trim();

        for (int i = displayName.Length - 1; i > 0; i--)
        {
            char current = displayName[i];
            char previous = displayName[i - 1];

            if (char.IsUpper(current) && char.IsLower(previous))
                displayName = displayName.Insert(i, " ");
        }

        while (displayName.Contains("  "))
            displayName = displayName.Replace("  ", " ");

        return displayName;
    }
}
