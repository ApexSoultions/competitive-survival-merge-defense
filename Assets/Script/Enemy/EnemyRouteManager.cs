using UnityEngine;

public class EnemyRouteManager : MonoBehaviour
{
    public static EnemyRouteManager Instance { get; private set; }

    [SerializeField] private EnemyRoute[] allRoutes;

    private void Awake()
    {
        Instance = this;
    }

    public EnemyRoute GetAlternateRoute(EnemyRoute currentRoute)
    {
        if (allRoutes == null || allRoutes.Length == 0)
            return null;

        EnemyRoute bestRoute = null;
        float bestDistance = float.MaxValue;

        foreach (EnemyRoute route in allRoutes)
        {
            if (route == null || route == currentRoute)
                continue;

            float distance = Vector3.Distance(
                currentRoute.transform.position,
                route.transform.position
            );

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestRoute = route;
            }
        }

        return bestRoute;
    }

    public EnemyRoute GetRandomRoute()
    {
        if (allRoutes == null || allRoutes.Length == 0)
            return null;

        int index = UnityEngine.Random.Range(0, allRoutes.Length);
        return allRoutes[index];
    }
}