using System.Collections.Generic;
using UnityEngine;

public static class CombatFeedbackBurst
{
    private const int PoolSize = 40;
    private static readonly Stack<PooledCombatBurst> Available = new Stack<PooledCombatBurst>(PoolSize);
    private static readonly List<PooledCombatBurst> All = new List<PooledCombatBurst>(PoolSize);
    private static Transform poolRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Available.Clear();
        All.Clear();
        poolRoot = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsurePool(EnemyCombatFeedbackTheme.LoadDefault());
    }

    public static void SpawnImpact(Vector3 position, EnemyDamageType damageType, EnemyCombatFeedbackTheme theme, bool isBoss)
    {
        Color color;
        int count;
        float speed;
        float size;
        float lifetime;

        if (damageType == EnemyDamageType.Critical)
        {
            color = theme != null ? theme.CriticalDamageColor : new Color(1f, 0.6f, 0.1f, 1f);
            count = isBoss ? 13 : 10;
            speed = isBoss ? 1.9f : 1.5f;
            size = isBoss ? 0.15f : 0.11f;
            lifetime = 0.31f;
        }
        else if (damageType == EnemyDamageType.Poison)
        {
            color = theme != null ? theme.PoisonDamageColor : Color.green;
            count = isBoss ? 8 : 5;
            speed = isBoss ? 0.95f : 0.72f;
            size = isBoss ? 0.12f : 0.085f;
            lifetime = 0.38f;
        }
        else
        {
            color = theme != null ? theme.GetDamageColor(damageType) : Color.white;
            count = isBoss ? 10 : 7;
            speed = isBoss ? 1.8f : 1.35f;
            size = isBoss ? 0.14f : 0.1f;
            lifetime = 0.28f;
        }

        count = MobileQualityRuntime.ScaleParticleCount(count);
        Play(position, color, theme, count, speed, size, lifetime);

        if ((damageType == EnemyDamageType.Critical || isBoss) && MobileQualityRuntime.ParticleBudgetScale >= 0.75f)
        {
            Play(
                position,
                Color.Lerp(color, Color.white, 0.72f),
                theme,
                MobileQualityRuntime.ScaleParticleCount(isBoss ? 9 : 6),
                speed * 0.62f,
                size * 0.58f,
                lifetime * 0.72f);
        }
    }

    public static void SpawnDeath(Vector3 position, EnemyCombatFeedbackTheme theme, bool isBoss)
    {
        Color color = isBoss
            ? new Color(0.82f, 0.22f, 1f, 1f)
            : new Color(1f, 0.3f, 0.12f, 1f);
        int count = MobileQualityRuntime.ScaleParticleCount(isBoss ? 30 : 18);
        Play(position, color, theme, count, isBoss ? 3f : 2.1f, isBoss ? 0.24f : 0.16f, isBoss ? 0.7f : 0.5f);
    }

    private static void Play(Vector3 position, Color color, EnemyCombatFeedbackTheme theme, int count, float speed, float size, float lifetime)
    {
        EnsurePool(theme);
        PooledCombatBurst burst = TakeOldestOrAvailable();
        if (burst != null)
            burst.Play(position, color, count, speed, size, lifetime);
    }

    private static void EnsurePool(EnemyCombatFeedbackTheme theme)
    {
        if (poolRoot != null)
            return;

        GameObject root = new GameObject("Combat Burst Pool");
        Object.DontDestroyOnLoad(root);
        poolRoot = root.transform;

        for (int i = 0; i < PoolSize; i++)
        {
            GameObject item = new GameObject("Combat Burst", typeof(ParticleSystem), typeof(PooledCombatBurst));
            item.transform.SetParent(poolRoot, false);
            PooledCombatBurst burst = item.GetComponent<PooledCombatBurst>();
            burst.Initialize(theme != null ? theme.ParticleMaterial : null);
            burst.StopImmediately();
            All.Add(burst);
            Available.Push(burst);
        }
    }

    private static PooledCombatBurst TakeOldestOrAvailable()
    {
        if (Available.Count > 0)
            return Available.Pop();

        PooledCombatBurst oldest = null;
        float oldestAge = float.MinValue;
        for (int i = 0; i < All.Count; i++)
        {
            PooledCombatBurst candidate = All[i];
            if (candidate.IsPlaying && candidate.Age > oldestAge)
            {
                oldest = candidate;
                oldestAge = candidate.Age;
            }
        }
        if (oldest != null)
            oldest.StopImmediately();
        return oldest;
    }

    internal static void Return(PooledCombatBurst burst)
    {
        burst.transform.SetParent(poolRoot, false);
        Available.Push(burst);
    }
}

[DisallowMultipleComponent]
internal sealed class PooledCombatBurst : MonoBehaviour
{
    private ParticleSystem particles;
    private float age;
    private float lifetime;
    private bool playing;

    public bool IsPlaying => playing;
    public float Age => age;

    public void Initialize(Material material)
    {
        particles = GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.None;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.45f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.18f, 1f),
            new Keyframe(1f, 0f)));

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sortingLayerName = "Tower";
        particleRenderer.sortingOrder = 72;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (material != null)
            particleRenderer.sharedMaterial = material;
    }

    public void Play(Vector3 position, Color color, int count, float speed, float size, float effectLifetime)
    {
        transform.position = position;
        age = 0f;
        lifetime = effectLifetime;
        playing = true;
        gameObject.SetActive(true);

        particles.Clear(true);
        ParticleSystem.MainModule main = particles.main;
        main.duration = Mathf.Max(0.1f, lifetime);
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.72f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.65f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size);
        main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.45f));

        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams { applyShapeToPosition = true };
        particles.Emit(emit, count);
        particles.Play(true);
    }

    private void Update()
    {
        if (!playing)
            return;
        age += Time.deltaTime;
        if (age >= lifetime + 0.08f)
            Release();
    }

    private void Release()
    {
        if (!playing)
            return;
        StopImmediately();
        CombatFeedbackBurst.Return(this);
    }

    public void StopImmediately()
    {
        playing = false;
        age = 0f;
        if (particles != null)
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        gameObject.SetActive(false);
    }
}
