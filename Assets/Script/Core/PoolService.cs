using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight generic object pool for M2+ content (ability VFX, impacts, etc.).
/// Rule: never Instantiate/Destroy repeatedly in combat hot paths — Get/Release instead.
/// Existing specialized pools (AbilityVfxPool, FloatingDamagePool) remain; new systems should use this or those pools.
/// </summary>
public sealed class PoolService : MonoBehaviour
{
    public static PoolService Instance { get; private set; }

    private readonly Dictionary<int, Queue<GameObject>> available = new Dictionary<int, Queue<GameObject>>();
    private readonly Dictionary<int, int> activeCounts = new Dictionary<int, int>();
    private Transform inactiveRoot;
    private int totalActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
            new GameObject("Pool Service", typeof(PoolService));
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        inactiveRoot = new GameObject("Inactive Pooled").transform;
        inactiveRoot.SetParent(transform, false);
    }

    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        return Instance != null ? Instance.Take(prefab, position, rotation) : null;
    }

    public static T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        if (prefab == null)
            return null;
        GameObject go = Get(prefab.gameObject, position, rotation);
        return go != null ? go.GetComponent<T>() : null;
    }

    public static void Release(GameObject instance)
    {
        if (Instance != null)
            Instance.Return(instance);
        else if (instance != null)
            instance.SetActive(false);
    }

    public static void Release(Component component)
    {
        if (component != null)
            Release(component.gameObject);
    }

    private GameObject Take(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        if (totalActive >= MobileQualityRuntime.MaxConcurrentVfx)
            return null;

        int key = prefab.GetInstanceID();
        if (!available.TryGetValue(key, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            available[key] = queue;
        }

        GameObject item = null;
        while (queue.Count > 0 && item == null)
        {
            GameObject candidate = queue.Dequeue();
            if (candidate == null)
                continue;

            var pooled = candidate.GetComponent<PooledInstance>();
            if (pooled != null && !pooled.IsInPool)
                continue;

            item = candidate;
        }

        if (item == null)
        {
            item = Instantiate(prefab, inactiveRoot);
            item.name = prefab.name;
            var tracker = item.GetComponent<PooledInstance>();
            if (tracker == null)
                tracker = item.AddComponent<PooledInstance>();
            tracker.PoolKey = key;
            tracker.IsInPool = true;
            ApplyParticleBudget(item);
        }

        var state = item.GetComponent<PooledInstance>();
        if (state != null)
            state.IsInPool = false;

        item.transform.SetParent(null, false);
        item.transform.SetPositionAndRotation(position, rotation);
        item.SetActive(true);
        totalActive++;
        activeCounts.TryGetValue(key, out int count);
        activeCounts[key] = count + 1;
        return item;
    }

    private void Return(GameObject item)
    {
        if (item == null)
            return;

        var tracker = item.GetComponent<PooledInstance>();
        if (tracker == null)
            tracker = item.AddComponent<PooledInstance>();

        if (tracker.IsInPool)
            return;

        int key = tracker.PoolKey != 0 ? tracker.PoolKey : item.GetInstanceID();
        tracker.PoolKey = key;
        tracker.IsInPool = true;

        item.SetActive(false);
        item.transform.SetParent(inactiveRoot, false);

        if (!available.TryGetValue(key, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            available[key] = queue;
        }

        queue.Enqueue(item);
        totalActive = Mathf.Max(0, totalActive - 1);
        if (activeCounts.TryGetValue(key, out int count))
            activeCounts[key] = Mathf.Max(0, count - 1);
    }

    private static void ApplyParticleBudget(GameObject item)
    {
        float scale = MobileQualityRuntime.ParticleBudgetScale;
        if (Mathf.Approximately(scale, 1f))
            return;

        ParticleSystem[] systems = item.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem.MainModule main = systems[i].main;
            main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(main.maxParticles * scale));
        }
    }
}

/// <summary>Marks a pooled GameObject and stores its pool key + in-pool state for double-release safety.</summary>
public sealed class PooledInstance : MonoBehaviour
{
    public int PoolKey;
    public bool IsInPool;
}
