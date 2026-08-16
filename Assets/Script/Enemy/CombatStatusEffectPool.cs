using System.Collections.Generic;
using UnityEngine;

internal enum CombatStatusVisualKind
{
    Stun,
    Poison
}

internal static class CombatStatusEffectPool
{
    private const int StunPrewarmCount = 16;
    private const int PoisonPrewarmCount = 24;
    private static readonly Stack<PooledStatusVisual> StunAvailable = new Stack<PooledStatusVisual>(StunPrewarmCount);
    private static readonly Stack<PooledStatusVisual> PoisonAvailable = new Stack<PooledStatusVisual>(PoisonPrewarmCount);
    private static Transform poolRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        StunAvailable.Clear();
        PoisonAvailable.Clear();
        poolRoot = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsurePool();
    }

    public static PooledStatusVisual Acquire(CombatStatusVisualKind kind, Transform parent, Vector3 localPosition, float scale)
    {
        if (!MobileQualityRuntime.EnableStatusIcons)
            return null;

        EnsurePool();
        Stack<PooledStatusVisual> available = kind == CombatStatusVisualKind.Stun ? StunAvailable : PoisonAvailable;
        if (available.Count == 0)
            return null;

        PooledStatusVisual visual = available.Pop();
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * scale;
        visual.gameObject.SetActive(true);
        visual.Play();
        return visual;
    }

    public static void Release(PooledStatusVisual visual)
    {
        if (visual == null)
            return;

        visual.StopAndClear();
        visual.transform.SetParent(poolRoot, false);
        visual.gameObject.SetActive(false);
        Stack<PooledStatusVisual> available = visual.Kind == CombatStatusVisualKind.Stun ? StunAvailable : PoisonAvailable;
        available.Push(visual);
    }

    private static void EnsurePool()
    {
        if (poolRoot != null)
            return;

        GameObject root = new GameObject("Combat Status Effect Pool");
        Object.DontDestroyOnLoad(root);
        poolRoot = root.transform;

        Prewarm("CombatFeedback/StunEffect", CombatStatusVisualKind.Stun, StunPrewarmCount, StunAvailable);
        Prewarm("CombatFeedback/PoisonAura", CombatStatusVisualKind.Poison, PoisonPrewarmCount, PoisonAvailable);
    }

    private static void Prewarm(string resourcePath, CombatStatusVisualKind kind, int count, Stack<PooledStatusVisual> available)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogWarning(resourcePath + " prefab is missing; the status icon will still be shown.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject instance = Object.Instantiate(prefab, poolRoot);
            instance.name = kind + " Effect";
            PooledStatusVisual visual = instance.AddComponent<PooledStatusVisual>();
            visual.Initialize(kind);
            visual.StopAndClear();
            instance.SetActive(false);
            available.Push(visual);
        }
    }
}

[DisallowMultipleComponent]
internal sealed class PooledStatusVisual : MonoBehaviour
{
    private ParticleSystem[] particles;

    public CombatStatusVisualKind Kind { get; private set; }

    public void Initialize(CombatStatusVisualKind kind)
    {
        Kind = kind;
        particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    public void Play()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Clear(true);
            particles[i].Play(true);
        }
    }

    public void StopEmitting()
    {
        for (int i = 0; i < particles.Length; i++)
            particles[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    public void StopAndClear()
    {
        if (particles == null)
            return;
        for (int i = 0; i < particles.Length; i++)
            particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
