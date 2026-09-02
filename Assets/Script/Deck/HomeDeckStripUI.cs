using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Home deck preview strip + Edit button. Opens DeckBuilderPanelUI.
/// </summary>
public sealed class HomeDeckStripUI : MonoBehaviour
{
    [SerializeField] private Button editButton;
    [SerializeField] private Image[] previewIcons;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private DeckBuilderPanelUI deckBuilder;

    private bool editBound;

    private void OnEnable()
    {
        LoadoutService.EnsureExists();
        if (LoadoutService.Instance != null)
            LoadoutService.Instance.LoadoutChanged += Refresh;
        EnsureEditBound();
        Refresh();
    }

    private void OnDisable()
    {
        if (LoadoutService.Instance != null)
            LoadoutService.Instance.LoadoutChanged -= Refresh;
    }

    private void OnDestroy()
    {
        if (editButton != null && editBound)
            editButton.onClick.RemoveListener(OpenDeckBuilder);
        editBound = false;
    }

    private void Start()
    {
        EnsureEditBound();
        if (deckBuilder == null)
            deckBuilder = FindFirstObjectByType<DeckBuilderPanelUI>(FindObjectsInactive.Include);
    }

    public void Bind(Button edit, Image[] icons, TMP_Text status, DeckBuilderPanelUI builder)
    {
        if (editButton != null && editBound)
        {
            editButton.onClick.RemoveListener(OpenDeckBuilder);
            editBound = false;
        }

        if (edit != null)
            editButton = edit;
        if (icons != null)
            previewIcons = icons;
        if (status != null)
            statusText = status;
        if (builder != null)
            deckBuilder = builder;

        EnsureEditBound();
        Refresh();
    }

    public void SetDeckBuilder(DeckBuilderPanelUI builder)
    {
        if (builder != null)
            deckBuilder = builder;
    }

    public void OpenDeckBuilder()
    {
        if (deckBuilder == null)
            deckBuilder = FindFirstObjectByType<DeckBuilderPanelUI>(FindObjectsInactive.Include);
        if (deckBuilder != null)
            deckBuilder.Open();
        else
            HubUiLog.Warn("[HomeDeckStripUI] DeckBuilderPanelUI missing.");
    }

    private void EnsureEditBound()
    {
        if (editButton == null || editBound)
            return;
        editButton.onClick.AddListener(OpenDeckBuilder);
        editBound = true;
    }

    private void Refresh()
    {
        LoadoutService service = LoadoutService.EnsureExists();
        MatchLoadout loadout = service != null ? service.SavedLoadout : null;
        UnitData[] units = loadout != null ? loadout.units : null;

        if (previewIcons != null)
        {
            for (int i = 0; i < previewIcons.Length; i++)
            {
                Image icon = previewIcons[i];
                if (icon == null)
                    continue;
                UnitData unit = units != null && i < units.Length ? units[i] : null;
                Sprite sprite = unit != null ? unit.GetIcon(1) : null;
                if (sprite != null)
                {
                    icon.sprite = sprite;
                    icon.color = Color.white;
                    icon.enabled = true;
                }
                else
                {
                    icon.sprite = null;
                    icon.color = new Color(1f, 1f, 1f, 0.15f);
                    icon.enabled = true;
                }
            }
        }

        if (statusText != null)
        {
            int unitCount = loadout != null ? loadout.CountUnits() : 0;
            int activeCount = loadout != null ? loadout.CountActives() : 0;
            int needUnits = service != null ? service.UnitSlots : 6;
            int needActives = service != null ? service.ActiveSlots : 2;
            statusText.text = unitCount + "/" + needUnits + " units · " + activeCount + "/" + needActives + " abilities";
        }
    }
}
