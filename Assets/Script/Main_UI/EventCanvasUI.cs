using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EventCanvasUI : MonoBehaviour
{
    private const int TabCount = 4;
    private const int EventCount = 3;

    private readonly string[] tabNames = { "Active", "Limited", "Boss", "Rewards" };
    private readonly string[] eventNames = { "Forest Festival", "Crystal Hunt", "Guild Boss" };
    private readonly float[] eventSeconds =
    {
        6f * 86400f + 12f * 3600f + 48f * 60f,
        3f * 86400f + 18f * 3600f + 21f * 60f,
        1f * 86400f + 8f * 3600f + 37f * 60f
    };

    private readonly Button[] tabButtons = new Button[TabCount];
    private readonly Image[] tabImages = new Image[TabCount];
    private readonly GameObject[] tabHighlights = new GameObject[TabCount];
    private readonly Button[] eventButtons = new Button[EventCount];
    private readonly TMP_Text[] eventTimerTexts = new TMP_Text[EventCount];
    private readonly TMP_Text[] eventButtonLabels = new TMP_Text[EventCount];
    private TMP_Text statusText;

    private bool initialized;
    private int selectedTab;
    private float timerClock;

    private void OnEnable()
    {
        EnsureInitialized();
        ShowTab(0);
        RefreshTimers(true);
    }

    private void Update()
    {
        for (int i = 0; i < eventSeconds.Length; i++)
            eventSeconds[i] = Mathf.Max(0f, eventSeconds[i] - Time.unscaledDeltaTime);

        timerClock += Time.unscaledDeltaTime;
        if (timerClock >= 1f)
            RefreshTimers(false);
    }

    public void ShowEventsCanvas()
    {
        gameObject.SetActive(true);
        EnsureInitialized();
        ShowTab(0);
        Debug.Log("Events page opened.");
    }

    public void HideEventsCanvas()
    {
        Debug.Log("Events page closed.");
        gameObject.SetActive(false);
    }

    public void ShowActive()
    {
        ShowTab(0);
    }

    public void ShowLimited()
    {
        ShowTab(1);
    }

    public void ShowBoss()
    {
        ShowTab(2);
    }

    public void ShowRewards()
    {
        ShowTab(3);
    }

    public void EnterForestFestival()
    {
        EnterEvent(0);
    }

    public void EnterCrystalHunt()
    {
        EnterEvent(1);
    }

    public void ChallengeGuildBoss()
    {
        EnterEvent(2);
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        WireButton("EventBackButton", HideEventsCanvas);

        for (int i = 0; i < TabCount; i++)
        {
            int capturedIndex = i;
            tabButtons[i] = FindComponent<Button>("EventTabButton_" + i);
            tabImages[i] = tabButtons[i] != null ? tabButtons[i].GetComponent<Image>() : null;
            Transform highlight = FindTransform(transform, "EventTabHighlight_" + i);
            tabHighlights[i] = highlight != null ? highlight.gameObject : null;

            if (tabButtons[i] != null)
                tabButtons[i].onClick.AddListener(() => ShowTab(capturedIndex));
        }

        for (int i = 0; i < EventCount; i++)
        {
            int capturedIndex = i;
            eventButtons[i] = FindComponent<Button>("EventActionButton_" + i);
            eventTimerTexts[i] = FindComponent<TMP_Text>("EventTimerText_" + i);
            eventButtonLabels[i] = FindComponent<TMP_Text>("EventActionLabel_" + i);

            if (eventButtons[i] != null)
                eventButtons[i].onClick.AddListener(() => EnterEvent(capturedIndex));
        }

        statusText = FindComponent<TMP_Text>("EventStatusText");
    }

    private void ShowTab(int index)
    {
        EnsureInitialized();
        selectedTab = Mathf.Clamp(index, 0, TabCount - 1);

        for (int i = 0; i < TabCount; i++)
        {
            bool selected = i == selectedTab;
            if (tabHighlights[i] != null)
            {
                tabHighlights[i].SetActive(selected);
                Image highlightImage = tabHighlights[i].GetComponent<Image>();
                if (highlightImage != null)
                    highlightImage.color = selected ? Color.white : new Color(1f, 1f, 1f, 0f);
            }

            if (tabImages[i] != null)
                tabImages[i].color = selected ? Color.white : new Color(0.72f, 0.72f, 0.78f, 1f);
        }

        SetText(statusText, tabNames[selectedTab] + " events selected");
        Debug.Log("Events page tab selected: " + tabNames[selectedTab]);
    }

    private void EnterEvent(int index)
    {
        if (index < 0 || index >= eventNames.Length)
            return;

        string actionName = index == 2 ? "Challenge" : "Enter";
        SetText(statusText, actionName + ": " + eventNames[index]);
        Debug.Log("Events page: " + actionName + " pressed for " + eventNames[index] + ".");
    }

    private void RefreshTimers(bool force)
    {
        if (!force && timerClock < 1f)
            return;

        timerClock = 0f;
        for (int i = 0; i < EventCount; i++)
            SetText(eventTimerTexts[i], "Ends in: " + FormatDuration(eventSeconds[i]));

        SetText(eventButtonLabels[0], "Enter");
        SetText(eventButtonLabels[1], "Enter");
        SetText(eventButtonLabels[2], "Challenge");
    }

    private void WireButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        Button button = FindComponent<Button>(objectName);
        if (button != null)
            button.onClick.AddListener(action);
    }

    private T FindComponent<T>(string objectName) where T : Component
    {
        Transform found = FindTransform(transform, objectName);
        return found != null ? found.GetComponent<T>() : null;
    }

    private static Transform FindTransform(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindTransform(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static string FormatDuration(float seconds)
    {
        int totalMinutes = Mathf.Max(0, Mathf.FloorToInt(seconds / 60f));
        int days = totalMinutes / 1440;
        totalMinutes %= 1440;
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;
        return days + "d " + hours.ToString("00") + "h " + minutes.ToString("00") + "m";
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}
