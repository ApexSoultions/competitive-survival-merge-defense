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

    private Image frameImage;
    private Image unitIcon;
    private Image deckImage;
    private TMP_Text deckLevel;
    private TMP_Text deckName;
    private Color frameDefaultColor = Color.white;
    private bool resolved;

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

    public void BindAbility(ActiveAbilityDefinition ability)
    {
        EnsureResolved();
        if (ability == null)
        {
            SetEmpty();
            return;
        }

        ApplyDeckPortrait(ability.icon);
        ApplyTagIcon(null);
        SetLevelText(string.Empty);
        SetNameText(ability.displayName);
        ShowLevel(false);
        ShowName(!string.IsNullOrEmpty(ability.displayName));
        ShowTagBadge(false);
    }

    public void BindRelic(RelicDefinition relic)
    {
        EnsureResolved();
        if (relic == null)
        {
            SetEmpty();
            return;
        }

        ApplyDeckPortrait(relic.icon);
        ApplyTagIcon(null);
        SetLevelText(string.Empty);
        SetNameText(relic.displayName);
        ShowLevel(false);
        ShowName(!string.IsNullOrEmpty(relic.displayName));
        ShowTagBadge(false);
    }

    public void BindSpecialTile(SpecialTileDefinition tile)
    {
        EnsureResolved();
        if (tile == null)
        {
            SetEmpty();
            return;
        }

        ApplyDeckPortrait(tile.icon);
        ApplyTagIcon(null);
        SetLevelText(string.Empty);
        SetNameText(tile.displayName);
        ShowLevel(false);
        ShowName(!string.IsNullOrEmpty(tile.displayName));
        ShowTagBadge(false);
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

    private void EnsureResolved()
    {
        if (resolved)
            return;

        resolved = true;
        frameImage = GetComponent<Image>();
        unitIcon = FindChildImage("Unit_Icon");
        deckImage = FindChildImage("Deck_Image");
        deckLevel = FindChildText("Deck_Level");
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

        Transform tagRoot = FindChildRecursive(transform, "Unity_Tag_BackGround");
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
