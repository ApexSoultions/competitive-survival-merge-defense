using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class QuestCanvasUI : MonoBehaviour
{
    private const int QuestCount = 8;

    private readonly List<QuestEntry> quests = new List<QuestEntry>(QuestCount);

    private Button dailyTabButton;
    private Button weeklyTabButton;
    private Button achievementsTabButton;
    private TMP_Text dailyTimerText;
    private TMP_Text weeklyTimerText;

    private bool initialized;
    private float dailyResetSeconds = 4f * 3600f + 32f * 60f + 18f;
    private float weeklyResetSeconds = 3f * 24f * 3600f + 4f * 3600f + 32f * 60f + 18f;
    private float timerRefreshClock;

    private void OnEnable()
    {
        EnsureInitialized();
        SelectTab(dailyTabButton);
        RefreshAllQuests();
        RefreshTimers(true);
    }

    private void Update()
    {
        dailyResetSeconds = Mathf.Max(0f, dailyResetSeconds - Time.unscaledDeltaTime);
        weeklyResetSeconds = Mathf.Max(0f, weeklyResetSeconds - Time.unscaledDeltaTime);

        timerRefreshClock += Time.unscaledDeltaTime;
        if (timerRefreshClock >= 1f)
            RefreshTimers(false);
    }

    public void ShowQuestCanvas()
    {
        gameObject.SetActive(true);
        EnsureInitialized();
        SelectTab(dailyTabButton);
        Debug.Log("Quest page opened.");
    }

    public void HideQuestCanvas()
    {
        Debug.Log("Quest page closed.");
        gameObject.SetActive(false);
    }

    public void ShowDaily()
    {
        EnsureInitialized();
        SelectTab(dailyTabButton);
        Debug.Log("Quest page: Daily Quests selected.");
    }

    public void ShowWeekly()
    {
        EnsureInitialized();
        SelectTab(weeklyTabButton);
        Debug.Log("Quest page: Weekly Quests selected.");
    }

    public void ShowAchievements()
    {
        EnsureInitialized();
        SelectTab(achievementsTabButton);
        Debug.Log("Quest page: Achievements selected.");
    }

    public void ClaimAll()
    {
        EnsureInitialized();

        int claimedCount = 0;
        for (int i = 0; i < quests.Count; i++)
        {
            QuestEntry quest = quests[i];
            if (quest.Claimed || quest.Current < quest.Required)
                continue;

            quest.Claimed = true;
            claimedCount++;
            RefreshQuest(quest);
        }

        if (claimedCount > 0)
            Debug.Log("Quest page: claimed " + claimedCount + " completed quest reward(s).");
        else
            Debug.Log("Quest page: no completed quest rewards are ready yet.");
    }

    public void PressQuestButton(int questIndex)
    {
        EnsureInitialized();

        if (questIndex < 0 || questIndex >= quests.Count)
            return;

        QuestEntry quest = quests[questIndex];
        if (quest.Claimed)
        {
            Debug.Log("Quest page: " + quest.Title + " is already claimed.");
            return;
        }

        if (quest.Current >= quest.Required)
        {
            quest.Claimed = true;
            RefreshQuest(quest);
            Debug.Log("Quest page: claimed reward for " + quest.Title + ".");
            return;
        }

        quest.Current = Mathf.Min(quest.Required, quest.Current + quest.Step);
        RefreshQuest(quest);

        if (quest.Current >= quest.Required)
            Debug.Log("Quest page: " + quest.Title + " completed. Press Claim to collect.");
        else
            Debug.Log("Quest page: progress added to " + quest.Title + ".");
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        dailyTabButton = FindComponent<Button>("QuestTab_Daily");
        weeklyTabButton = FindComponent<Button>("QuestTab_Weekly");
        achievementsTabButton = FindComponent<Button>("QuestTab_Achievements");
        dailyTimerText = FindComponent<TMP_Text>("QuestDailyTimerText");
        weeklyTimerText = FindComponent<TMP_Text>("QuestWeeklyTimerText");

        WireButton("QuestCloseButton", HideQuestCanvas);
        WireButton("QuestTab_Daily", ShowDaily);
        WireButton("QuestTab_Weekly", ShowWeekly);
        WireButton("QuestTab_Achievements", ShowAchievements);
        WireButton("QuestClaimAllButton", ClaimAll);

        CreateQuestEntries();
        RefreshAllQuests();
    }

    private void CreateQuestEntries()
    {
        quests.Clear();

        AddQuest(0, "Win 3 battles", 2, 3, 1);
        AddQuest(1, "Collect 500 coins", 320, 500, 90);
        AddQuest(2, "Upgrade 1 hero", 0, 1, 1);
        AddQuest(3, "Open 1 chest", 0, 1, 1);
        AddQuest(4, "Win 20 battles", 8, 20, 3);
        AddQuest(5, "Earn 5,000 League Points", 1250, 5000, 750);
        AddQuest(6, "Complete 10 Co-op battles", 3, 10, 2);
        AddQuest(7, "Reach Team Power 150,000", 125850, 150000, 8000);
    }

    private void AddQuest(int index, string title, int current, int required, int step)
    {
        Button actionButton = FindComponent<Button>("QuestGoButton_" + index);
        TMP_Text buttonLabel = actionButton != null ? actionButton.GetComponentInChildren<TMP_Text>(true) : null;
        TMP_Text progressText = FindComponent<TMP_Text>("QuestProgressText_" + index);
        RectTransform progressFill = FindComponent<RectTransform>("QuestProgressFill_" + index);
        RectTransform progressTrack = FindComponent<RectTransform>("QuestProgressTrack_" + index);

        QuestEntry quest = new QuestEntry(index, title, current, required, step, actionButton, buttonLabel, progressText, progressFill, progressTrack);
        quests.Add(quest);

        if (actionButton != null)
        {
            int capturedIndex = index;
            actionButton.onClick.AddListener(() => PressQuestButton(capturedIndex));
        }
    }

    private void RefreshAllQuests()
    {
        for (int i = 0; i < quests.Count; i++)
            RefreshQuest(quests[i]);
    }

    private static void RefreshQuest(QuestEntry quest)
    {
        float progress = quest.Required <= 0 ? 1f : Mathf.Clamp01((float)quest.Current / quest.Required);

        if (quest.ProgressFill != null)
        {
            Vector2 size = quest.ProgressFill.sizeDelta;
            size.x = quest.FillMaxWidth * progress;
            quest.ProgressFill.sizeDelta = size;
        }

        if (quest.ProgressText != null)
            quest.ProgressText.text = FormatNumber(quest.Current) + " / " + FormatNumber(quest.Required);

        if (quest.ButtonLabel != null)
            quest.ButtonLabel.text = quest.Claimed ? "Done" : quest.Current >= quest.Required ? "Claim" : "Go";

        if (quest.ActionButton != null)
            quest.ActionButton.interactable = !quest.Claimed;
    }

    private void RefreshTimers(bool force)
    {
        if (!force && timerRefreshClock < 1f)
            return;

        timerRefreshClock = 0f;

        if (dailyTimerText != null)
            dailyTimerText.text = "Daily reset in: " + FormatTime(dailyResetSeconds, false);

        if (weeklyTimerText != null)
            weeklyTimerText.text = FormatTime(weeklyResetSeconds, true);
    }

    private void SelectTab(Button selectedButton)
    {
        TintTab(dailyTabButton, dailyTabButton == selectedButton);
        TintTab(weeklyTabButton, weeklyTabButton == selectedButton);
        TintTab(achievementsTabButton, achievementsTabButton == selectedButton);
    }

    private static void TintTab(Button button, bool selected)
    {
        if (button == null || button.targetGraphic == null)
            return;

        button.targetGraphic.color = selected ? Color.white : new Color(0.72f, 0.68f, 0.8f, 1f);
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

    private static string FormatTime(float seconds, bool includeDays)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int days = totalSeconds / 86400;
        totalSeconds %= 86400;
        int hours = totalSeconds / 3600;
        totalSeconds %= 3600;
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;

        if (includeDays && days > 0)
            return days + "d " + hours.ToString("00") + ":" + minutes.ToString("00") + ":" + secs.ToString("00");

        return hours.ToString("00") + ":" + minutes.ToString("00") + ":" + secs.ToString("00");
    }

    private static string FormatNumber(int value)
    {
        return value.ToString("N0");
    }

    private sealed class QuestEntry
    {
        public readonly int Index;
        public readonly string Title;
        public readonly int Required;
        public readonly int Step;
        public readonly Button ActionButton;
        public readonly TMP_Text ButtonLabel;
        public readonly TMP_Text ProgressText;
        public readonly RectTransform ProgressFill;
        public readonly float FillMaxWidth;

        public int Current;
        public bool Claimed;

        public QuestEntry(
            int index,
            string title,
            int current,
            int required,
            int step,
            Button actionButton,
            TMP_Text buttonLabel,
            TMP_Text progressText,
            RectTransform progressFill,
            RectTransform progressTrack)
        {
            Index = index;
            Title = title;
            Current = current;
            Required = required;
            Step = Mathf.Max(1, step);
            ActionButton = actionButton;
            ButtonLabel = buttonLabel;
            ProgressText = progressText;
            ProgressFill = progressFill;
            FillMaxWidth = progressTrack != null && progressTrack.sizeDelta.x > 0f ? progressTrack.sizeDelta.x : 260f;
        }
    }
}
