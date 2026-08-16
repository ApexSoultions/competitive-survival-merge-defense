using UnityEngine;

public class EnemyPathFollower : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;

    [Header("Visual Rotation")]
    [SerializeField] private Transform visual;
    [SerializeField] private float rotationOffset = 0f;

    private Transform[] waypoints;
    private EnemyRoute currentRoute;
    private int currentIndex;

    public void SetRoute(EnemyRoute route)
    {
        if (route == null)
            return;

        currentRoute = route;
        waypoints = route.Waypoints;
        currentIndex = 0;

        rotationOffset = route.RouteRotationOffset;

        if (route.WaypointCount > 0)
            transform.position = route.GetWaypointPosition(0);
    }

    private void Update()
    {
        if (!BattleFlowState.IsGameplayActive || waypoints == null || waypoints.Length == 0)
            return;

        if (currentIndex >= waypoints.Length)
            return;

        Vector3 targetPosition = currentRoute != null
            ? currentRoute.GetWaypointPosition(currentIndex)
            : waypoints[currentIndex].position;

        Vector3 direction = targetPosition - transform.position;

        RotateVisual(direction);

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) <= 0.05f)
        {
            currentIndex++;

            if (currentIndex >= waypoints.Length)
            {
                UnityEngine.Debug.Log("Enemy reached the end.");
                Destroy(gameObject);
            }
        }
    }

    private void RotateVisual(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (visual != null)
            visual.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
        else
            transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
    }
}
