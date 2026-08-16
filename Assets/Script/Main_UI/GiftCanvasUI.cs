using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GiftCanvasUI : MonoBehaviour
{
    private const int LoginRewardCount = 7;
    private const int FriendGiftCount = 2;

    private TMP_Text dailyTimerText;
    private TMP_Text loginTimerText;
    private TMP_Text dailyClaimLabel;
    private TMP_Text claimAllLabel;
    private TMP_Text redeemStatusText;
    private TMP_InputField redeemInput;

    private readonly TMP_Text[] loginStateLabels = new TMP_Text[LoginRewardCount];
    private readonly TMP_Text[] friendClaimLabels = new TMP_Text[FriendGiftCount];
    private readonly Button[] loginButtons = new Button[LoginRewardCount];
    private readonly Button[] friendButtons = new Button[FriendGiftCount];

    private bool initialized;
    private bool dailyClaimed;
    private bool redeemClaimed;
    private readonly bool[] loginClaimed = new bool[LoginRewardCount];
    private readonly bool[] friendClaimed = new bool[FriendGiftCount];
    private int currentLoginDay = 1;
    private float dailySeconds = 4f * 3600f + 32f * 60f + 18f;
    private float loginSeconds = 13f * 24f * 3600f + 4f * 3600f + 32f * 60f + 18f;
    private float timerClock;

    private void OnEnable()
    {
        EnsureInitialized();
        RefreshAll();
        RefreshTimers(true);
    }

    private void Update()
    {
        dailySeconds = Mathf.Max(0f, dailySeconds - Time.unscaledDeltaTime);
        loginSeconds = Mathf.Max(0f, loginSeconds - Time.unscaledDeltaTime);

        timerClock += Time.unscaledDeltaTime;
        if (timerClock >= 1f)
            RefreshTimers(false);
    }

    public void ShowGiftCanvas()
    {
        gameObject.SetActive(true);
        EnsureInitialized();
        RefreshAll();
        Debug.Log("Gift page opened.");
    }

    public void HideGiftCanvas()
    {
        Debug.Log("Gift page closed.");
        gameObject.SetActive(false);
    }

    public void ClaimDailyGift()
    {
        EnsureInitialized();

        if (dailyClaimed)
        {
            Debug.Log("Gift page: daily gift already claimed.");
            return;
        }

        dailyClaimed = true;
        RefreshDailyGift();
        Debug.Log("Gift page: daily gift claimed.");
    }

    public void ClaimLoginReward(int dayIndex)
    {
        EnsureInitialized();

        if (dayIndex < 0 || dayIndex >= loginClaimed.Length)
            return;

        if (loginClaimed[dayIndex])
        {
            Debug.Log("Gift page: login day " + (dayIndex + 1) + " already claimed.");
            return;
        }

        if (dayIndex > currentLoginDay)
        {
            Debug.Log("Gift page: login day " + (dayIndex + 1) + " is locked.");
            return;
        }

        loginClaimed[dayIndex] = true;
        if (dayIndex == currentLoginDay && currentLoginDay < loginClaimed.Length - 1)
            currentLoginDay++;

        RefreshLoginRewards();
        Debug.Log("Gift page: login day " + (dayIndex + 1) + " reward claimed.");
    }

    public void ClaimFriendGift(int giftIndex)
    {
        EnsureInitialized();

        if (giftIndex < 0 || giftIndex >= friendClaimed.Length)
            return;

        if (friendClaimed[giftIndex])
        {
            Debug.Log("Gift page: friend/guild gift already claimed.");
            return;
        }

        friendClaimed[giftIndex] = true;
        RefreshFriendGifts();
        Debug.Log("Gift page: friend/guild gift " + (giftIndex + 1) + " claimed.");
    }

    public void ClaimAllFriendGifts()
    {
        EnsureInitialized();

        int claimedCount = 0;
        for (int i = 0; i < friendClaimed.Length; i++)
        {
            if (friendClaimed[i])
                continue;

            friendClaimed[i] = true;
            claimedCount++;
        }

        RefreshFriendGifts();
        Debug.Log(claimedCount > 0
            ? "Gift page: claimed " + claimedCount + " friend/guild gift(s)."
            : "Gift page: no friend/guild gifts are ready.");
    }

    public void RedeemCode()
    {
        EnsureInitialized();

        string code = redeemInput != null ? redeemInput.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(code))
        {
            SetText(redeemStatusText, "Enter a code first");
            Debug.Log("Gift page: redeem code is empty.");
            return;
        }

        if (redeemClaimed)
        {
            SetText(redeemStatusText, "Already redeemed");
            Debug.Log("Gift page: redeem code already used this session.");
            return;
        }

        redeemClaimed = true;
        SetText(redeemStatusText, "Reward sent!");
        Debug.Log("Gift page: redeemed gift code " + code + ".");
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        loginClaimed[0] = true;

        dailyTimerText = FindComponent<TMP_Text>("GiftDailyTimerText");
        loginTimerText = FindComponent<TMP_Text>("GiftLoginTimerText");
        dailyClaimLabel = FindComponent<TMP_Text>("GiftDailyClaimLabel");
        claimAllLabel = FindComponent<TMP_Text>("GiftClaimAllLabel");
        redeemStatusText = FindComponent<TMP_Text>("GiftRedeemStatusText");
        redeemInput = FindComponent<TMP_InputField>("GiftRedeemInput");

        WireButton("GiftCloseButton", HideGiftCanvas);
        WireButton("GiftDailyClaimButton", ClaimDailyGift);
        WireButton("GiftClaimAllButton", ClaimAllFriendGifts);
        WireButton("GiftRedeemButton", RedeemCode);

        for (int i = 0; i < LoginRewardCount; i++)
        {
            int capturedIndex = i;
            loginButtons[i] = FindComponent<Button>("GiftLoginDayButton_" + i);
            loginStateLabels[i] = FindComponent<TMP_Text>("GiftLoginStateText_" + i);
            if (loginButtons[i] != null)
                loginButtons[i].onClick.AddListener(() => ClaimLoginReward(capturedIndex));
        }

        for (int i = 0; i < FriendGiftCount; i++)
        {
            int capturedIndex = i;
            friendButtons[i] = FindComponent<Button>("GiftFriendClaimButton_" + i);
            friendClaimLabels[i] = FindComponent<TMP_Text>("GiftFriendClaimLabel_" + i);
            if (friendButtons[i] != null)
                friendButtons[i].onClick.AddListener(() => ClaimFriendGift(capturedIndex));
        }
    }

    private void RefreshAll()
    {
        RefreshDailyGift();
        RefreshLoginRewards();
        RefreshFriendGifts();
    }

    private void RefreshDailyGift()
    {
        SetText(dailyClaimLabel, dailyClaimed ? "Claimed" : "Claim");

        Button button = FindComponent<Button>("GiftDailyClaimButton");
        if (button != null)
            button.interactable = !dailyClaimed;
    }

    private void RefreshLoginRewards()
    {
        for (int i = 0; i < LoginRewardCount; i++)
        {
            if (loginStateLabels[i] != null)
            {
                if (loginClaimed[i])
                    loginStateLabels[i].text = "Done";
                else if (i == currentLoginDay)
                    loginStateLabels[i].text = "Ready";
                else
                    loginStateLabels[i].text = "Locked";
            }

            if (loginButtons[i] != null)
                loginButtons[i].interactable = !loginClaimed[i] && i <= currentLoginDay;
        }
    }

    private void RefreshFriendGifts()
    {
        int remaining = 0;
        for (int i = 0; i < FriendGiftCount; i++)
        {
            if (!friendClaimed[i])
                remaining++;

            SetText(friendClaimLabels[i], friendClaimed[i] ? "Claimed" : "Claim");
            if (friendButtons[i] != null)
                friendButtons[i].interactable = !friendClaimed[i];
        }

        SetText(claimAllLabel, remaining > 0 ? "Claim All" : "All Claimed");

        Button claimAllButton = FindComponent<Button>("GiftClaimAllButton");
        if (claimAllButton != null)
            claimAllButton.interactable = remaining > 0;
    }

    private void RefreshTimers(bool force)
    {
        if (!force && timerClock < 1f)
            return;

        timerClock = 0f;
        SetText(dailyTimerText, "Refreshes in: " + FormatTime(dailySeconds, false));
        SetText(loginTimerText, "Resets in: " + FormatTime(loginSeconds, true));
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

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}
