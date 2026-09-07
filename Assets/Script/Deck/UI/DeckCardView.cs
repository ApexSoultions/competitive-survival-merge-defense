using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds deck card prefab children (Unit_Icon, Deck_Level, Deck_Image) to loadout items.
/// Deck_Image = main portrait. Unit_Icon = small tag/class badge.
/// </summary>
public sealed class DeckCardView : MonoBehaviour
{
    private static readonly Color SelectedTint = new Color(1f, 0.85f, 0.25f, 1f);
    private static readonly Color EmptyFrameTint = new Color(1f, 1f, 1f, 0.12f);

    private static readonly string[] PortraitChildNames =
    {
        "Deck_Image",
        "Ability_Image",
        "Relic_Image",
        "Speciality_Image"
    };

    private static readonly string[] LevelChildNames =
    {
        "Deck_Level",
        "Ability_Level",
        "Relic_Level",
        "Speciality_Level"
    };

    [Header("Category Tag Badges (Inspector)")]
    public Sprite abilityCategoryTag;
    public Sprite relicCategoryTag;
    public Sprite specialTileCategoryTag;

    [Header("Optional Shared Settings")]
    public DeckCardVisualSettings visualSettings;

    private Image frameImage;
    private Image unitIcon;
    private Image tagBackground;
    private Image deckImage;
    private TMP_Text deckLevel;
    private TMP_Text deckName;
    private Color frameDefaultColor = Color.white;
    private bool resolved;

    public void ApplyVisualSettings(DeckCardVisualSettings settings)
    {
        if (settings == null)
            return;

        visualSettings = settings;
        if (settings.abilityCategoryTag != null)
            abilityCategoryTag = settings.abilityCategoryTag;
        if (settings.relicCategoryTag != null)
            relicCategoryTag = settings.relicCategoryTag;
        if (settings.specialTileCategoryTag != null)
            specialTileCategoryTag = settings.specialTileCategoryTag;
    }

    public void BindUnit(UnitData unit, int level = 1)
    {
        EnsureResolved();
        if (unit == null)
        {
            SetEmpty();
            return;
        }

        ApplyDeckPortrait(unit.GetDeckPortrait(level));
        ApplyTagIcon(unit.GetTagIcon());
        SetLevelText("LVL " + level);
        SetNameText(unit.unitName);
        ShowLevel(true);
        ShowName(!string.IsNullOrEmpty(unit.unitName));
        ShowTagBadge(unit.GetTagIcon() != null);
    }

    public Image FrameImage
    {
        get
        {
            EnsureResolved();
            return frameImage;
        }
    }

    public Image PortraitImage
    {
        get
        {
            EnsureResolved();
            return deckImage;
        }
    }

    public Image TagIconImage
    {
        get
        {
            EnsureResolved();
            return unitIcon;
        }
    }

    public Image TagBackgroundImage
    {
        get
        {
            EnsureResolved();
            return tagBackground;
        }
    }

    public TMP_Text LevelLabel
    {
        get
        {
            EnsureResolved();
            return deckLevel;
        }
    }

    public void ApplyUnitTagFallback(Sprite fallbackIcon)
    {
        EnsureResolved();
        if (fallbackIcon == null || unitIcon == null)
            return;

        if (unitIcon.sprite != null && unitIcon.enabled)
            return;

        ApplyTagIcon(fallbackIcon);
        ShowTagBadge(true);
    }

    public void ApplyTagFrameSprite(Sprite frameSprite)
    {
        EnsureResolved();
        if (tagBackground == null || frameSprite == null)
            return;

        tagBackground.sprite = frameSprite;
        tagBackground.color = Color.white;
        tagBackground.enabled = true;
    }

    public void BindPortrait(Sprite portrait, int level, Sprite categoryTag = null)
    {
        EnsureResolved();
        ApplyDeckPortrait(portrait);
        ApplyTagIcon(categoryTag);
        SetLevelText("LVL " + level);
        ShowLevel(true);
        ShowName(false);
        ShowTagBadge(categoryTag != null);
    }

    public void BindAbility(ActiveAbilityDefinition ability, int level = 1)
    {
        EnsureResolved();
        if (ability == null)
        {
            SetEmpty();
            return;
        }

        Sprite categoryTag = ResolveAbilityTag();
        ApplyDeckPortrait(ability.icon);
        ApplyTagIcon(categoryTag);
        SetLevelText("LVL " + level);
        SetNameText(ability.displayName);
        ShowLevel(true);
        ShowName(!string.IsNullOrEmpty(ability.displayName));
        ShowTagBadge(categoryTag != null);
    }

    public void BindRelic(RelicDefinition relic, int level = 1)
    {
        EnsureResolved();
        if (relic == null)
        {
            SetEmpty();
            return;
        }

        Sprite categoryTag = ResolveRelicTag();
        ApplyDeckPortrait(relic.icon);
        ApplyTagIcon(categoryTag);
        SetLevelText("LVL " + level);
        SetNameText(relic.displayName);
        ShowLevel(true);
        ShowName(!string.IsNullOrEmpty(relic.displayName));
        ShowTagBadge(categoryTag != null);
    }

    public void BindSpecialTile(SpecialTileDefinition tile, int level = 1)
    {
        EnsureResolved();
        if (tile == null)
        {
            SetEmpty();
            return;
        }

        Sprite categoryTag = ResolveSpecialTileTag();
        ApplyDeckPortrait(tile.icon);
        ApplyTagIcon(categoryTag);
        SetLevelText("LVL " + level);
        SetNameText(tile.displayName);
        ShowLevel(true);
        ShowName(!string.IsNullOrEmpty(tile.displayName));
        ShowTagBadge(categoryTag != null);
    }

    public void SetEmpty()
    {
        EnsureResolved();
        ApplyDeckPortrait(null);
        ApplyTagIcon(null);
        SetLevelText(string.Empty);
        SetNameText(string.Empty);
        ShowLevel(false);
        ShowName(false);
        ShowTagBadge(false);
        SetSelectedHighlight(false);
    }

    public void SetSelectedHighlight(bool on)
    {
        EnsureResolved();
        if (frameImage == null)
            return;
        frameImage.color = on ? SelectedTint : frameDefaultColor;
    }

    private Sprite ResolveAbilityTag()
    {
        if (abilityCategoryTag != null)
            return abilityCategoryTag;
        return visualSettings != null ? visualSettings.abilityCategoryTag : null;
    }

    private Sprite ResolveRelicTag()
    {
        if (relicCategoryTag != null)
            return relicCategoryTag;
        return visualSettings != null ? visualSettings.relicCategoryTag : null;
    }

    private Sprite ResolveSpecialTileTag()
    {
        if (specialTileCategoryTag != null)
            return specialTileCategoryTag;
        return visualSettings != null ? visualSettings.specialTileCategoryTag : null;
    }

    private void EnsureResolved()
    {
        if (resolved)
            return;

        resolved = true;
        frameImage = GetComponent<Image>();
        unitIcon = FindChildImage("Unit_Icon");
        tagBackground = FindChildImage("Unity_Tag_BackGround");
        deckImage = FindFirstChildImage(PortraitChildNames);
        deckLevel = FindFirstChildText(LevelChildNames);
        deckName = FindChildText("Deck_Name");
        if (deckName == null)
            deckName = FindChildText("Unit_Name");

        if (frameImage != null)
            frameDefaultColor = frameImage.color;
    }

    private void ApplyDeckPortrait(Sprite sprite)
    {
        if (deckImage == null)
            return;

        if (sprite != null)
        {
            deckImage.sprite = sprite;
            deckImage.color = Color.white;
            deckImage.enabled = true;
            deckImage.preserveAspect = true;
            return;
        }

        deckImage.sprite = null;
        deckImage.color = EmptyFrameTint;
        deckImage.enabled = true;
    }

    private void ApplyTagIcon(Sprite sprite)
    {
        if (unitIcon == null)
            return;

        if (sprite != null)
        {
            unitIcon.sprite = sprite;
            unitIcon.color = Color.white;
            unitIcon.enabled = true;
            unitIcon.preserveAspect = true;
            return;
        }

        unitIcon.sprite = null;
        unitIcon.color = EmptyFrameTint;
        unitIcon.enabled = false;
    }

    private void ShowTagBadge(bool show)
    {
        if (unitIcon != null)
            unitIcon.gameObject.SetActive(show);

        Transform tagRoot = tagBackground != null ? tagBackground.transform : FindChildRecursive(transform, "Unity_Tag_BackGround");
        if (tagRoot != null)
            tagRoot.gameObject.SetActive(show);
    }

    private void SetLevelText(string text)
    {
        if (deckLevel == null)
            return;
        deckLevel.text = text;
    }

    private void SetNameText(string text)
    {
        if (deckName == null)
            return;
        deckName.text = text;
    }

    private void ShowLevel(bool show)
    {
        if (deckLevel == null)
            return;
        deckLevel.gameObject.SetActive(show);
    }

    private void ShowName(bool show)
    {
        if (deckName == null)
            return;
        deckName.gameObject.SetActive(show);
    }

    private Image FindFirstChildImage(string[] childNames)
    {
        for (int i = 0; i < childNames.Length; i++)
        {
            Image image = FindChildImage(childNames[i]);
            if (image != null)
                return image;
        }

        return null;
    }

    private TMP_Text FindFirstChildText(string[] childNames)
    {
        for (int i = 0; i < childNames.Length; i++)
        {
            TMP_Text text = FindChildText(childNames[i]);
            if (text != null)
                return text;
        }

        return null;
    }

    private Image FindChildImage(string childName)
    {
        Transform child = FindChildRecursive(transform, childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private TMP_Text FindChildText(string childName)
    {
        Transform child = FindChildRecursive(transform, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
