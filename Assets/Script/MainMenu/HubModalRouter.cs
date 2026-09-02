using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns hub modal open/close (Shop / Event / Gift / Quest).
/// Wired by MainMenuUI — keeps modal concerns out of battle/loadout flow.
/// </summary>
[DisallowMultipleComponent]
public sealed class HubModalRouter : MonoBehaviour
{
    [SerializeField] private GameObject shopCanvas;
    [SerializeField] private Button[] shopOpenButtons;
    [SerializeField] private Button[] shopCloseButtons;

    [SerializeField] private GameObject eventCanvas;
    [SerializeField] private Button[] eventOpenButtons;
    [SerializeField] private Button[] eventCloseButtons;

    [SerializeField] private GameObject giftCanvas;
    [SerializeField] private Button[] giftOpenButtons;
    [SerializeField] private Button[] giftCloseButtons;

    [SerializeField] private GameObject questCanvas;
    [SerializeField] private Button[] questOpenButtons;
    [SerializeField] private Button[] questCloseButtons;

    private bool wired;

    public void Configure(
        GameObject shop,
        Button[] shopOpen,
        Button[] shopClose,
        GameObject events,
        Button[] eventOpen,
        Button[] eventClose,
        GameObject gift,
        Button[] giftOpen,
        Button[] giftClose,
        GameObject quest,
        Button[] questOpen,
        Button[] questClose)
    {
        shopCanvas = shop;
        shopOpenButtons = shopOpen;
        shopCloseButtons = shopClose;
        eventCanvas = events;
        eventOpenButtons = eventOpen;
        eventCloseButtons = eventClose;
        giftCanvas = gift;
        giftOpenButtons = giftOpen;
        giftCloseButtons = giftClose;
        questCanvas = quest;
        questOpenButtons = questOpen;
        questCloseButtons = questClose;
    }

    public void Wire()
    {
        if (wired)
            return;
        wired = true;

        BindAll(shopOpenButtons, ShowShop);
        BindAll(shopCloseButtons, HideShop);
        BindAll(eventOpenButtons, ShowEvent);
        BindAll(eventCloseButtons, HideEvent);
        BindAll(giftOpenButtons, ShowGift);
        BindAll(giftCloseButtons, HideGift);
        BindAll(questOpenButtons, ShowQuest);
        BindAll(questCloseButtons, HideQuest);

        PrepareCloseButtons(shopCloseButtons);
        PrepareCloseButtons(eventCloseButtons);
        PrepareCloseButtons(giftCloseButtons);
        PrepareCloseButtons(questCloseButtons);
    }

    public void Unwire()
    {
        if (!wired)
            return;
        wired = false;

        UnbindAll(shopOpenButtons, ShowShop);
        UnbindAll(shopCloseButtons, HideShop);
        UnbindAll(eventOpenButtons, ShowEvent);
        UnbindAll(eventCloseButtons, HideEvent);
        UnbindAll(giftOpenButtons, ShowGift);
        UnbindAll(giftCloseButtons, HideGift);
        UnbindAll(questOpenButtons, ShowQuest);
        UnbindAll(questCloseButtons, HideQuest);
    }

    public void ShowShop() => SetModal(shopCanvas, true, "shop");
    public void HideShop() => SetModal(shopCanvas, false, "shop");
    public void ShowEvent() => SetModal(eventCanvas, true, "event");
    public void HideEvent() => SetModal(eventCanvas, false, "event");
    public void ShowGift() => SetModal(giftCanvas, true, "gift");
    public void HideGift() => SetModal(giftCanvas, false, "gift");
    public void ShowQuest() => SetModal(questCanvas, true, "quest");
    public void HideQuest() => SetModal(questCanvas, false, "quest");

    private static void SetModal(GameObject canvas, bool visible, string name)
    {
        if (canvas != null)
            canvas.SetActive(visible);
        HubUiLog.Info("Main Menu: " + name + (visible ? " opened." : " closed."));
    }

    /// <summary>
    /// Ensures close buttons receive clicks without spawning extra Canvas components.
    /// </summary>
    private static void PrepareCloseButtons(Button[] buttons)
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            button.interactable = true;
            button.transform.SetAsLastSibling();

            Graphic graphic = button.targetGraphic;
            if (graphic == null)
                graphic = button.GetComponent<Graphic>();
            if (graphic != null)
                graphic.raycastTarget = true;

            // Strip legacy per-button Canvas spam if an older build added them.
            Canvas extraCanvas = button.GetComponent<Canvas>();
            if (extraCanvas != null && extraCanvas.overrideSorting && extraCanvas.sortingOrder >= 10000)
            {
                GraphicRaycaster raycaster = button.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                    Destroy(raycaster);
                Destroy(extraCanvas);
            }
        }
    }

    private static void BindAll(Button[] buttons, UnityEngine.Events.UnityAction action)
    {
        if (buttons == null)
            return;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].onClick.AddListener(action);
        }
    }

    private static void UnbindAll(Button[] buttons, UnityEngine.Events.UnityAction action)
    {
        if (buttons == null)
            return;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].onClick.RemoveListener(action);
        }
    }
}
