using System.Collections;
using TMPro;
using UnityEngine;

public class WavePopupUI : MonoBehaviour
{
    public static WavePopupUI Instance { get; private set; }

    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text popupText;
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        Instance = this;

        if (popupRoot != null)
        {
            canvasGroup = popupRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = popupRoot.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public IEnumerator ShowWaveIntro(int waveNumber, float duration)
    {
        if (popupRoot == null || popupText == null)
            yield break;

        popupRoot.SetActive(true);

        float timer = duration;
        float shownAge = 0f;
        float fade = Mathf.Min(Mathf.Max(0f, fadeDuration), Mathf.Max(0f, duration * 0.5f));

        while (timer > 0f)
        {
            popupText.text = "WAVE " + waveNumber + "\nSTARTS IN " + Mathf.CeilToInt(timer);

            if (canvasGroup != null)
            {
                float fadeIn = fade > 0f ? Mathf.Clamp01(shownAge / fade) : 1f;
                float fadeOut = fade > 0f ? Mathf.Clamp01(timer / fade) : 1f;
                canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Min(fadeIn, fadeOut));
            }

            timer -= Time.deltaTime;
            shownAge += Time.deltaTime;
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        popupRoot.SetActive(false);
    }
}
