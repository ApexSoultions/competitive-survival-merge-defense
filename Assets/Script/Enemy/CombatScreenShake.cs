using UnityEngine;

[DefaultExecutionOrder(10000)]
public sealed class CombatScreenShake : MonoBehaviour
{
    private static CombatScreenShake instance;

    private Camera targetCamera;
    private Vector3 appliedOffset;
    private float remainingTime;
    private float totalTime;
    private float strength;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance == null)
            new GameObject("Combat Screen Shake", typeof(CombatScreenShake));
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Request(EnemyDamageType damageType, bool isBoss)
    {
        if (instance == null || !BattleFlowState.IsGameplayActive)
            return;

        if (!MobileQualityRuntime.EnableScreenShake)
            return;

        float requestedStrength = damageType == EnemyDamageType.Critical ? 0.038f : 0.018f;
        float requestedDuration = damageType == EnemyDamageType.Critical ? 0.1f : 0.065f;

        if (isBoss)
            requestedStrength *= 1.2f;

        instance.strength = Mathf.Max(instance.strength, requestedStrength);
        instance.totalTime = Mathf.Max(instance.totalTime, requestedDuration);
        instance.remainingTime = Mathf.Max(instance.remainingTime, requestedDuration);
    }

    private void LateUpdate()
    {
        ResolveCamera();
        if (targetCamera == null)
            return;

        targetCamera.transform.localPosition -= appliedOffset;
        appliedOffset = Vector3.zero;

        if (remainingTime <= 0f || !BattleFlowState.IsGameplayActive)
        {
            remainingTime = 0f;
            strength = 0f;
            return;
        }

        remainingTime -= Time.deltaTime;
        float fade = totalTime > 0f ? Mathf.Clamp01(remainingTime / totalTime) : 0f;
        Vector2 noise = Random.insideUnitCircle * (strength * fade);
        appliedOffset = new Vector3(noise.x, noise.y, 0f);
        targetCamera.transform.localPosition += appliedOffset;
    }

    private void ResolveCamera()
    {
        Camera current = Camera.main;
        if (current == targetCamera)
            return;

        if (targetCamera != null)
            targetCamera.transform.localPosition -= appliedOffset;

        targetCamera = current;
        appliedOffset = Vector3.zero;
    }

    private void OnDisable()
    {
        if (targetCamera != null)
            targetCamera.transform.localPosition -= appliedOffset;
        appliedOffset = Vector3.zero;
    }
}
