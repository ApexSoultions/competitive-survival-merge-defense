using System.Collections.Generic;
using UnityEngine;

internal enum CombatStatusVisualKind
{
    Stun,
    Poison,
    Burn,
    Freeze,
    Mark
}

internal static class CombatStatusEffectPool
{
    private const int StunPrewarmCount = 16;
    private const int PoisonPrewarmCount = 24;
    private const int BurnPrewarmCount = 20;
    private const int FreezePrewarmCount = 16;
    private const int MarkPrewarmCount = 12;

    private static readonly Stack<PooledStatusVisual> StunAvailable = new Stack<PooledStatusVisual>(StunPrewarmCount);
    private static readonly Stack<PooledStatusVisual> PoisonAvailable = new Stack<PooledStatusVisual>(PoisonPrewarmCount);
    private static readonly Stack<PooledStatusVisual> BurnAvailable = new Stack<PooledStatusVisual>(BurnPrewarmCount);
    private static readonly Stack<PooledStatusVisual> FreezeAvailable = new Stack<PooledStatusVisual>(FreezePrewarmCount);
    private static readonly Stack<PooledStatusVisual> MarkAvailable = new Stack<PooledStatusVisual>(MarkPrewarmCount);
    private static Transform poolRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        StunAvailable.Clear();
        PoisonAvailable.Clear();
        BurnAvailable.Clear();
        FreezeAvailable.Clear();
        MarkAvailable.Clear();
        poolRoot = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsurePool();
    }

    public static PooledStatusVisual Acquire(
        CombatStatusVisualKind kind,
        Transform parent,
        Vector3 localPosition,
        float scale,
        Color? tint = null)
    {
        if (!MobileQualityRuntime.EnableStatusIcons)
            return null;

        EnsurePool();
        Stack<PooledStatusVisual> available = GetStack(kind);
        if (available == null || available.Count == 0)
            return null;

        PooledStatusVisual visual = available.Pop();
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * scale;
        if (tint.HasValue)
            visual.ApplyTint(tint.Value);
        visual.gameObject.SetActive(true);
        visual.Play();
        return visual;
    }

    public static void Release(PooledStatusVisual visual)
    {
        if (visual == null)
            return;

        visual.StopAndClear();
        visual.RestoreTint();
        visual.transform.SetParent(poolRoot, false);
        visual.gameObject.SetActive(false);
        Stack<PooledStatusVisual> available = GetStack(visual.Kind);
        if (available != null)
            available.Push(visual);
    }

    private static Stack<PooledStatusVisual> GetStack(CombatStatusVisualKind kind)
    {
        switch (kind)
        {
            case CombatStatusVisualKind.Stun:
                return StunAvailable;
            case CombatStatusVisualKind.Poison:
                return PoisonAvailable;
            case CombatStatusVisualKind.Burn:
                return BurnAvailable;
            case CombatStatusVisualKind.Freeze:
                return FreezeAvailable;
            case CombatStatusVisualKind.Mark:
                return MarkAvailable;
            default:
                return null;
        }
    }

    private static void EnsurePool()
    {
        if (poolRoot != null)
            return;

        GameObject root = new GameObject("Combat Status Effect Pool");
        Object.DontDestroyOnLoad(root);
        poolRoot = root.transform;

        // Reuse existing Resources prefabs with per-kind tint at acquire time.
        Prewarm("CombatFeedback/StunEffect", CombatStatusVisualKind.Stun, StunPrewarmCount, StunAvailable);
        Prewarm("CombatFeedback/PoisonAura", CombatStatusVisualKind.Poison, PoisonPrewarmCount, PoisonAvailable);
        Prewarm("CombatFeedback/PoisonAura", CombatStatusVisualKind.Burn, BurnPrewarmCount, BurnAvailable);
        Prewarm("CombatFeedback/StunEffect", CombatStatusVisualKind.Freeze, FreezePrewarmCount, FreezeAvailable);
        Prewarm("CombatFeedback/PoisonAura", CombatStatusVisualKind.Mark, MarkPrewarmCount, MarkAvailable);
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
    private SpriteRenderer[] spriteRenderers;
    private Color[] particleStartColors;
    private Color[] spriteBaseColors;

    public CombatStatusVisualKind Kind { get; private set; }

    public void Initialize(CombatStatusVisualKind kind)
    {
        Kind = kind;
        particles = GetComponentsInChildren<ParticleSystem>(true);
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        particleStartColors = new Color[particles.Length];
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            particleStartColors[i] = main.startColor.color;
        }

        spriteBaseColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteBaseColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
    }

    public void ApplyTint(Color tint)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            main.startColor = tint;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;
            Color c = tint;
            c.a = spriteBaseColors[i].a;
            spriteRenderers[i].color = c;
        }
    }

    public void RestoreTint()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            main.startColor = particleStartColors[i];
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = spriteBaseColors[i];
        }
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
