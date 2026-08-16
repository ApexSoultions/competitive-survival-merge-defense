using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum EnemyDamageType
{
    Normal,
    Critical,
    Poison,
    Physical,
    Fire,
    Frost,
    Nature,
    Lightning,
    Arcane,
    Healing,
    Mana,
    ManaGain
}

public enum EnemyStatusType
{
    Slow,
    Poison,
    Stun
}

[DisallowMultipleComponent]
public sealed class EnemyCombatFeedback : MonoBehaviour
{
    private const float CanvasPixelsPerUnit = 100f;
    private const int StatusCount = 3;
    private const int SlowStatusIndex = (int)EnemyStatusType.Slow;

    private readonly float[] statusEndTimes = new float[StatusCount];
    private readonly GameObject[] statusIcons = new GameObject[StatusCount];
    private Enemy enemy;
    private EnemyCombatFeedbackTheme theme;
    private Canvas feedbackCanvas;
    private Image healthFill;
    private SpriteRenderer[] visualRenderers;
    private Color[] visualRendererColors;
    private StunStatusUI stunStatus;
    private PoisonStatusUI poisonStatus;
    private float displayedHealth = 1f;
    private float healthVelocity;
    private float flashHoldRemaining;
    private float flashRecoverAge;
    private float flashRecoverDuration;
    private Color activeFlashColor;
    private int lastStatusCount = -1;
    private bool initialized;
    private bool deathPlayed;
    private bool flashActive;

    [Header("Status Tint")]
    [SerializeField, Range(0f, 1f)] private float slowTintStrength = 0.38f;

    public void Initialize(Enemy owner, Transform visualRoot)
    {
        if (initialized || owner == null)
            return;

        enemy = owner;
        theme = EnemyCombatFeedbackTheme.LoadDefault();
        Transform rendererRoot = visualRoot != null ? visualRoot : owner.transform;
        visualRenderers = rendererRoot.GetComponentsInChildren<SpriteRenderer>(true);
        visualRendererColors = new Color[visualRenderers.Length];
        for (int i = 0; i < visualRenderers.Length; i++)
            visualRendererColors[i] = visualRenderers[i] != null ? visualRenderers[i].color : Color.white;

        if (theme != null && theme.HealthFrameSprite != null && theme.HealthFillSprite != null)
            BuildWorldSpaceCanvas();
        else
            Debug.LogWarning("Enemy combat feedback theme is missing. Run Tools/Combat Feedback/Rebuild All Assets.", owner);

        stunStatus = GetComponent<StunStatusUI>();
        if (stunStatus == null)
            stunStatus = gameObject.AddComponent<StunStatusUI>();
        stunStatus.Initialize(statusIcons[(int)EnemyStatusType.Stun], rendererRoot, theme != null ? theme.StatusIconFrameSprite : null);

        poisonStatus = GetComponent<PoisonStatusUI>();
        if (poisonStatus == null)
            poisonStatus = gameObject.AddComponent<PoisonStatusUI>();
        poisonStatus.Initialize(statusIcons[(int)EnemyStatusType.Poison], theme != null ? theme.StatusIconFrameSprite : null);

        initialized = true;
    }

    private void Update()
    {
        if (!initialized || enemy == null || deathPlayed)
            return;

        UpdateHealthBar();
        UpdateSlowStatus();
        UpdateHitFlash();
        LayoutStatusIconsIfChanged();
    }

    public void PlayDamage(float damage, EnemyDamageType damageType)
    {
        if (!initialized || deathPlayed || damage <= 0f)
            return;

        bool isBoss = enemy != null && enemy.IsBoss;
        FloatingDamagePool damagePool = FloatingDamagePool.Instance;
        if (damagePool != null)
            damagePool.Show(transform.position, damage, damageType, theme, isBoss);
        CombatFeedbackBurst.SpawnImpact(transform.position + Vector3.up * 0.52f, damageType, theme, isBoss);
        CombatScreenShake.Request(damageType, isBoss);
        BeginHitFlash(damageType);
    }

    public void ShowStatus(EnemyStatusType statusType, float duration, Sprite statusIcon = null)
    {
        if (!initialized || duration <= 0f)
            return;

        if (!MobileQualityRuntime.EnableStatusIcons)
            return;

        if (statusType == EnemyStatusType.Stun)
        {
            stunStatus.Show(duration, statusIcon);
        }
        else if (statusType == EnemyStatusType.Poison)
        {
            poisonStatus.Show(duration, statusIcon);
        }
        else
        {
            statusEndTimes[SlowStatusIndex] = Time.time + duration;
            if (statusIcons[SlowStatusIndex] != null)
            {
                if (statusIcon != null && statusIcons[SlowStatusIndex].TryGetComponent(out Image icon))
                    icon.sprite = statusIcon;
                statusIcons[SlowStatusIndex].SetActive(true);
            }

            if (!flashActive)
                ApplyRestingColors();
        }

        LayoutStatusIconsIfChanged(true);
    }

    public void PlayDeath()
    {
        if (!initialized || deathPlayed)
            return;

        deathPlayed = true;
        flashActive = false;
        stunStatus.ClearImmediate();
        poisonStatus.ClearImmediate();
        if (feedbackCanvas != null)
            feedbackCanvas.gameObject.SetActive(false);

        RestoreRendererColors();
        CombatFeedbackBurst.SpawnDeath(transform.position + Vector3.up * 0.4f, theme, enemy != null && enemy.IsBoss);
    }

    private void BuildWorldSpaceCanvas()
    {
        bool isBoss = enemy != null && enemy.IsBoss;
        Vector2 barSize = isBoss ? theme.BossBarSize : theme.NormalBarSize;
        Vector2 barOffset = isBoss ? theme.BossBarOffset : theme.NormalBarOffset;

        GameObject canvasObject = new GameObject("CombatInfo", typeof(RectTransform), typeof(Canvas));
        RectTransform canvasTransform = canvasObject.GetComponent<RectTransform>();
        canvasTransform.SetParent(transform, false);
        canvasTransform.localPosition = new Vector3(barOffset.x, barOffset.y, -0.05f);
        canvasTransform.localRotation = Quaternion.identity;
        canvasTransform.localScale = Vector3.one / CanvasPixelsPerUnit;
        canvasTransform.sizeDelta = barSize * CanvasPixelsPerUnit;

        feedbackCanvas = canvasObject.GetComponent<Canvas>();
        feedbackCanvas.renderMode = RenderMode.WorldSpace;
        feedbackCanvas.overrideSorting = true;
        feedbackCanvas.sortingLayerName = "Tower";
        feedbackCanvas.sortingOrder = 65;

        RectTransform healthBarRoot = CreateRect("HealthBar", canvasTransform);
        Stretch(healthBarRoot);

        RectTransform frameTransform = CreateRect("Frame", healthBarRoot);
        Stretch(frameTransform);
        Image frame = frameTransform.gameObject.AddComponent<Image>();
        frame.sprite = theme.HealthFrameSprite;
        frame.preserveAspect = false;
        frame.raycastTarget = false;

        RectTransform fillTransform = CreateRect("Fill", healthBarRoot);
        Stretch(fillTransform, isBoss ? 9f : 6f, isBoss ? 6f : 4f);
        healthFill = fillTransform.gameObject.AddComponent<Image>();
        healthFill.sprite = theme.HealthFillSprite;
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthFill.fillAmount = 1f;
        healthFill.raycastTarget = false;

        RectTransform statusRoot = CreateRect("Statuses", canvasTransform);
        statusRoot.anchorMin = new Vector2(0.5f, 1f);
        statusRoot.anchorMax = new Vector2(0.5f, 1f);
        statusRoot.pivot = new Vector2(0.5f, 0f);
        statusRoot.anchoredPosition = new Vector2(0f, 5f);
        statusRoot.sizeDelta = new Vector2(90f, isBoss ? 26f : 20f);

        CreateStatusIcon(statusRoot, EnemyStatusType.Slow, "S", theme.SlowColor, isBoss);
        CreateStatusIcon(statusRoot, EnemyStatusType.Poison, "P", theme.PoisonColor, isBoss);
        CreateStatusIcon(statusRoot, EnemyStatusType.Stun, "!", theme.StunColor, isBoss);
    }

    private void CreateStatusIcon(RectTransform parent, EnemyStatusType type, string labelText, Color color, bool isBoss)
    {
        int index = (int)type;
        float iconSize = isBoss ? 24f : 18f;
        RectTransform iconTransform = CreateRect(type + "Icon", parent);
        iconTransform.anchorMin = new Vector2(0.5f, 0f);
        iconTransform.anchorMax = new Vector2(0.5f, 0f);
        iconTransform.pivot = new Vector2(0.5f, 0f);
        iconTransform.sizeDelta = Vector2.one * iconSize;

        Image icon = iconTransform.gameObject.AddComponent<Image>();
        icon.sprite = theme.StatusIconFrameSprite;
        icon.color = color;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        RectTransform labelTransform = CreateRect("Label", iconTransform);
        Stretch(labelTransform, 2f, 2f);
        TextMeshProUGUI label = labelTransform.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontStyle = FontStyles.Bold;
        label.fontSize = isBoss ? 14f : 11f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.outlineColor = new Color32(33, 14, 30, 255);
        label.outlineWidth = 0.18f;
        label.raycastTarget = false;

        statusIcons[index] = iconTransform.gameObject;
        statusIcons[index].SetActive(false);
    }

    private void UpdateHealthBar()
    {
        if (healthFill == null || enemy.MaxHealth <= 0f)
            return;

        float targetHealth = Mathf.Clamp01(enemy.CurrentHealth / enemy.MaxHealth);
        displayedHealth = Mathf.SmoothDamp(displayedHealth, targetHealth, ref healthVelocity, Mathf.Max(0.01f, theme.SmoothHealthTime));
        healthFill.fillAmount = displayedHealth;
    }

    private void UpdateSlowStatus()
    {
        GameObject slowIcon = statusIcons[SlowStatusIndex];
        if (Time.time < statusEndTimes[SlowStatusIndex])
            return;

        if (slowIcon != null && slowIcon.activeSelf)
            slowIcon.SetActive(false);

        if (!flashActive)
            ApplyRestingColors();
    }

    private void BeginHitFlash(EnemyDamageType damageType)
    {
        Color themeFlash = theme != null ? theme.HitFlashColor : new Color(1f, 0.5f, 0.3f, 1f);
        if (damageType == EnemyDamageType.Poison)
        {
            Color poisonColor = theme != null ? theme.PoisonDamageColor : Color.green;
            activeFlashColor = Color.Lerp(GetAverageBaseColor(), poisonColor, 0.42f);
            flashHoldRemaining = 0.028f;
            flashRecoverDuration = 0.075f;
        }
        else
        {
            if (damageType == EnemyDamageType.Critical)
                activeFlashColor = Color.Lerp(themeFlash, Color.white, 0.3f);
            else if (damageType != EnemyDamageType.Normal && theme != null)
                activeFlashColor = Color.Lerp(GetAverageBaseColor(), theme.GetDamageColor(damageType), 0.46f);
            else
                activeFlashColor = themeFlash;
            flashHoldRemaining = damageType == EnemyDamageType.Critical ? 0.07f : 0.05f;
            flashRecoverDuration = damageType == EnemyDamageType.Critical ? 0.12f : 0.09f;
        }

        flashRecoverAge = 0f;
        flashActive = true;
        ApplyRendererColor(activeFlashColor);
    }

    private void UpdateHitFlash()
    {
        if (!flashActive)
            return;

        if (flashHoldRemaining > 0f)
        {
            flashHoldRemaining -= Time.deltaTime;
            return;
        }

        flashRecoverAge += Time.deltaTime;
        float progress = Mathf.Clamp01(flashRecoverAge / flashRecoverDuration);
        for (int i = 0; i < visualRenderers.Length; i++)
        {
            if (visualRenderers[i] == null)
                continue;
            Color restColor = GetRestingColor(i);
            visualRenderers[i].color = Color.Lerp(activeFlashColor, restColor, progress);
        }

        if (progress >= 1f)
        {
            flashActive = false;
            ApplyRestingColors();
        }
    }

    private void LayoutStatusIconsIfChanged(bool force = false)
    {
        int activeCount = 0;
        for (int i = 0; i < statusIcons.Length; i++)
        {
            if (statusIcons[i] != null && statusIcons[i].activeSelf)
                activeCount++;
        }

        if (!force && activeCount == lastStatusCount)
            return;
        lastStatusCount = activeCount;

        float spacing = enemy != null && enemy.IsBoss ? 27f : 21f;
        float startX = -((activeCount - 1) * spacing) * 0.5f;
        int activeIndex = 0;

        for (int i = 0; i < statusIcons.Length; i++)
        {
            if (statusIcons[i] == null || !statusIcons[i].activeSelf)
                continue;
            statusIcons[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(startX + activeIndex * spacing, 0f);
            activeIndex++;
        }
    }

    private Color GetAverageBaseColor()
    {
        if (visualRendererColors == null || visualRendererColors.Length == 0)
            return Color.white;
        return visualRendererColors[0];
    }

    private void ApplyRendererColor(Color color)
    {
        for (int i = 0; i < visualRenderers.Length; i++)
        {
            if (visualRenderers[i] != null)
                visualRenderers[i].color = color;
        }
    }

    private void ApplyRestingColors()
    {
        for (int i = 0; i < visualRenderers.Length; i++)
        {
            if (visualRenderers[i] == null)
                continue;
            visualRenderers[i].color = GetRestingColor(i);
        }
    }

    private Color GetRestingColor(int rendererIndex)
    {
        Color color = stunStatus != null
            ? stunStatus.GetRestingColor(rendererIndex, visualRendererColors[rendererIndex])
            : visualRendererColors[rendererIndex];

        if (Time.time < statusEndTimes[SlowStatusIndex])
        {
            Color slowColor = theme != null ? theme.SlowColor : new Color(0.25f, 0.78f, 1f, 1f);
            color = Color.Lerp(color, slowColor, slowTintStrength);
        }

        return color;
    }

    private void OnDisable()
    {
        flashActive = false;
        RestoreRendererColors();
    }

    private void RestoreRendererColors()
    {
        if (visualRenderers == null || visualRendererColors == null)
            return;

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            if (visualRenderers[i] != null)
                visualRenderers[i].color = visualRendererColors[i];
        }
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        RectTransform rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect, float horizontalInset = 0f, float verticalInset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontalInset, verticalInset);
        rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
    }
}
