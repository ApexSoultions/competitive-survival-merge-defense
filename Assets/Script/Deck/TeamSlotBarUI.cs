using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamSlotBarUI : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField] private UnitData[] selectedDeckUnits;
    [SerializeField] private TowerBoardCell[] boardCells;
    [SerializeField] private Image[] slotImages;
    [SerializeField] private TMP_Text[] levelTexts;

    [Header("Slot Style")]
    [SerializeField] private Sprite slotFrameSprite;
    [SerializeField] private Sprite crownBadgeSprite;
    [SerializeField] private Sprite crownIconSprite;
    [SerializeField] private Color emptySlotColor = new Color(1f, 1f, 1f, 0.22f);
    [SerializeField] private Color occupiedSlotColor = Color.white;
    [SerializeField] private Color levelBadgeColor = new Color(0.42f, 0.22f, 0.72f, 0.95f);

    private readonly List<Image> resolvedSlotImages = new List<Image>();
    private readonly List<TMP_Text> resolvedLevelTexts = new List<TMP_Text>();

    private void OnEnable()
    {
        TowerBoardCell.BoardChanged += Refresh;
        SummonManager.SelectedDeckChanged += SetSelectedDeck;

        ResolveReferences();
        Refresh();
    }

    private void Start()
    {
        ResolveReferences();
        Refresh();
    }

    private void OnDisable()
    {
        TowerBoardCell.BoardChanged -= Refresh;
        SummonManager.SelectedDeckChanged -= SetSelectedDeck;
    }

    public void SetSelectedDeck(UnitData[] deckUnits)
    {
        selectedDeckUnits = deckUnits;
        Refresh();
    }

    public void Refresh()
    {
        ResolveReferences();

        for (int i = 0; i < resolvedSlotImages.Count; i++)
        {
            Image slotImage = resolvedSlotImages[i];
            TMP_Text levelText = i < resolvedLevelTexts.Count ? resolvedLevelTexts[i] : null;
            UnitData unitData = selectedDeckUnits != null && i < selectedDeckUnits.Length ? selectedDeckUnits[i] : null;

            if (slotImage == null)
                continue;

            Image portrait = GetOrCreatePortrait(slotImage);
            Image crownBadge = GetOrCreateCrownBadge(slotImage);

            if (unitData == null)
            {
                if (slotFrameSprite != null)
                    slotImage.sprite = slotFrameSprite;

                portrait.sprite = null;
                portrait.color = emptySlotColor;
                slotImage.color = occupiedSlotColor;
                SetLevelText(levelText, string.Empty, false);
                SetCrownVisible(crownBadge, false);
                continue;
            }

            if (slotFrameSprite != null)
                slotImage.sprite = slotFrameSprite;

            int highestLevel = GetHighestBoardLevel(unitData);
            portrait.sprite = unitData.GetIcon(highestLevel);
            portrait.color = occupiedSlotColor;
            portrait.preserveAspect = false;
            portrait.raycastTarget = false;
            slotImage.color = occupiedSlotColor;
            slotImage.raycastTarget = false;

            ApplyLevelBadgeColor(levelText);
            SetLevelText(levelText, "Level " + highestLevel, true);
            SetCrownVisible(crownBadge, true);
        }
    }

    private int GetHighestBoardLevel(UnitData unitData)
    {
        int highestLevel = 1;

        if (unitData == null || boardCells == null)
            return highestLevel;

        foreach (TowerBoardCell cell in boardCells)
        {
            BoardTower tower = cell != null ? cell.CurrentTower : null;

            if (tower == null || tower.UnitData != unitData)
                continue;

            highestLevel = Mathf.Max(highestLevel, Mathf.Max(1, tower.Level));
        }

        return highestLevel;
    }

    private void ResolveReferences()
    {
        ResolveDeck();
        ResolveBoardCells();
        ResolveSlots();
    }

    private void ResolveDeck()
    {
        if (SummonManager.Instance == null)
            return;

        UnitData[] managerDeck = SummonManager.Instance.SelectedDeckUnits;

        if (managerDeck != null && managerDeck.Length > 0)
            selectedDeckUnits = managerDeck;
    }

    private void ResolveBoardCells()
    {
        if (boardCells != null && boardCells.Length > 0)
            return;

        if (SummonManager.Instance != null && SummonManager.Instance.BoardCells != null && SummonManager.Instance.BoardCells.Length > 0)
        {
            boardCells = SummonManager.Instance.BoardCells;
            return;
        }

        boardCells = FindObjectsByType<TowerBoardCell>(FindObjectsSortMode.None);
    }

    private void ResolveSlots()
    {
        resolvedSlotImages.Clear();
        resolvedLevelTexts.Clear();

        if (slotImages != null && slotImages.Length > 0)
        {
            foreach (Image slotImage in slotImages)
            {
                if (slotImage != null)
                    resolvedSlotImages.Add(slotImage);
            }
        }
        else
        {
            Image[] childImages = GetComponentsInChildren<Image>(true);

            foreach (Image image in childImages)
            {
                if (image == null || image.transform == transform)
                    continue;

                if (image.gameObject.name.Contains("TeamSlot", StringComparison.OrdinalIgnoreCase))
                    resolvedSlotImages.Add(image);
            }

            resolvedSlotImages.Sort((left, right) => GetHierarchyOrder(left.transform).CompareTo(GetHierarchyOrder(right.transform)));
        }

        if (levelTexts != null && levelTexts.Length > 0)
        {
            foreach (TMP_Text text in levelTexts)
            {
                if (text != null)
                    resolvedLevelTexts.Add(text);
            }
        }

        while (resolvedLevelTexts.Count < resolvedSlotImages.Count)
            resolvedLevelTexts.Add(EnsureLevelText(resolvedSlotImages[resolvedLevelTexts.Count]));
    }

    private static Image GetOrCreatePortrait(Image slotImage)
    {
        Transform existing = slotImage.transform.Find("Icon");
        if (existing != null && existing.TryGetComponent(out Image existingImage))
        {
            ApplyPortraitLayout(existing as RectTransform, existingImage);
            return existingImage;
        }

        RectTransform icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
        icon.gameObject.layer = slotImage.gameObject.layer;
        icon.SetParent(slotImage.transform, false);
        icon.SetAsFirstSibling();

        Image portrait = icon.GetComponent<Image>();
        ApplyPortraitLayout(icon, portrait);
        return portrait;
    }

    private static void ApplyPortraitLayout(RectTransform icon, Image portrait)
    {
        if (icon == null)
            return;

        icon.anchorMin = Vector2.zero;
        icon.anchorMax = Vector2.one;
        icon.offsetMin = new Vector2(4f, 4f);
        icon.offsetMax = new Vector2(-4f, -4f);
        icon.SetAsFirstSibling();

        if (portrait == null)
            return;

        portrait.raycastTarget = false;
        portrait.preserveAspect = false;
        portrait.type = Image.Type.Simple;
    }

    private TMP_Text EnsureLevelText(Image slotImage)
    {
        if (slotImage == null)
            return null;

        Transform existing = slotImage.transform.Find("LevelBadge/LevelText");

        if (existing != null && existing.TryGetComponent(out TMP_Text existingText))
        {
            ApplyLevelBadgeLayout(existing.parent as RectTransform);
            return existingText;
        }

        RectTransform badge = new GameObject("LevelBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
        badge.gameObject.layer = slotImage.gameObject.layer;
        badge.SetParent(slotImage.transform, false);
        ApplyLevelBadgeLayout(badge);

        Image badgeImage = badge.GetComponent<Image>();
        badgeImage.color = levelBadgeColor;
        badgeImage.raycastTarget = false;

        RectTransform textTransform = new GameObject("LevelText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Shadow)).GetComponent<RectTransform>();
        textTransform.gameObject.layer = slotImage.gameObject.layer;
        textTransform.SetParent(badge, false);
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.one;
        textTransform.offsetMin = new Vector2(2f, 0f);
        textTransform.offsetMax = new Vector2(-2f, 0f);

        TextMeshProUGUI text = textTransform.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = 18f;
        text.fontStyle = FontStyles.Bold;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        Shadow shadow = textTransform.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(1.4f, -1.4f);

        return text;
    }

    private static void ApplyLevelBadgeLayout(RectTransform badge)
    {
        if (badge == null)
            return;

        badge.anchorMin = new Vector2(0f, 0f);
        badge.anchorMax = new Vector2(1f, 0f);
        badge.pivot = new Vector2(0.5f, 0f);
        badge.anchoredPosition = new Vector2(0f, 4f);
        badge.sizeDelta = new Vector2(-8f, 28f);
    }

    private Image GetOrCreateCrownBadge(Image slotImage)
    {
        Transform existing = slotImage.transform.Find("CrownBadge");
        if (existing != null && existing.TryGetComponent(out Image existingBadge))
        {
            ApplyCrownLayout(existing as RectTransform);
            ApplyCrownSprites(existingBadge);
            return existingBadge;
        }

        RectTransform badge = new GameObject("CrownBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
        badge.gameObject.layer = slotImage.gameObject.layer;
        badge.SetParent(slotImage.transform, false);
        ApplyCrownLayout(badge);

        Image badgeImage = badge.GetComponent<Image>();
        badgeImage.raycastTarget = false;
        badgeImage.preserveAspect = true;

        RectTransform crown = new GameObject("Crown", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
        crown.gameObject.layer = slotImage.gameObject.layer;
        crown.SetParent(badge, false);
        crown.anchorMin = new Vector2(0.18f, 0.18f);
        crown.anchorMax = new Vector2(0.82f, 0.82f);
        crown.offsetMin = Vector2.zero;
        crown.offsetMax = Vector2.zero;

        Image crownImage = crown.GetComponent<Image>();
        crownImage.raycastTarget = false;
        crownImage.preserveAspect = true;

        ApplyCrownSprites(badgeImage);
        return badgeImage;
    }

    private static void ApplyCrownLayout(RectTransform badge)
    {
        if (badge == null)
            return;

        badge.anchorMin = new Vector2(0f, 1f);
        badge.anchorMax = new Vector2(0f, 1f);
        badge.pivot = new Vector2(0.5f, 0.5f);
        badge.anchoredPosition = new Vector2(6f, -6f);
        badge.sizeDelta = new Vector2(44f, 44f);
        badge.SetAsLastSibling();
    }

    private void ApplyCrownSprites(Image badgeImage)
    {
        if (badgeImage == null)
            return;

        if (crownBadgeSprite != null)
            badgeImage.sprite = crownBadgeSprite;

        Transform crown = badgeImage.transform.Find("Crown");
        if (crown != null && crown.TryGetComponent(out Image crownImage) && crownIconSprite != null)
            crownImage.sprite = crownIconSprite;
    }

    private static void SetCrownVisible(Image crownBadge, bool visible)
    {
        if (crownBadge != null)
            crownBadge.gameObject.SetActive(visible);
    }

    private void ApplyLevelBadgeColor(TMP_Text levelText)
    {
        if (levelText == null || levelText.transform.parent == null)
            return;

        if (levelText.transform.parent.TryGetComponent(out Image badgeImage))
            badgeImage.color = levelBadgeColor;
    }

    private static void SetLevelText(TMP_Text text, string value, bool visible)
    {
        if (text == null)
            return;

        text.text = value;

        if (text.transform.parent != null)
            text.transform.parent.gameObject.SetActive(visible);
    }

    private static int GetHierarchyOrder(Transform transform)
    {
        if (transform == null)
            return int.MaxValue;

        int order = transform.GetSiblingIndex();
        Transform current = transform.parent;
        int multiplier = 100;

        while (current != null)
        {
            order += current.GetSiblingIndex() * multiplier;
            multiplier *= 100;
            current = current.parent;
        }

        return order;
    }
}
