// Full Path: Assets/Scripts/Enemy/DefaultEnemy/Behaviour Logic/Idle/EnemyIdleRandomWanderer.cs
using UnityEngine;
using System.Collections.Generic; // Keep for List if needed elsewhere, though not directly used now

[CreateAssetMenu(fileName = "EnemyIdleRandomWanderer", menuName = "Enemy Logic/Idle Logic/Random Wanderer")]
public class EnemyIdleRandomWanderer : EnemyIdleSOBase
{
    [SerializeField] private float wanderRadius = 3f; // Renamed from RandomMovementRange for clarity
    // REMOVED: RandomMovementSpeed - AgentMover controls speed
    [SerializeField] private float waitTimeAtDestination = 2.0f; // How long to wait after reaching a point
    [SerializeField] private float maxWanderTime = 10.0f; // Max time before picking a new point even if not reached

    // Runtime variables
    private float currentWaitTimer = 0f;
    private float currentWanderTimer = 0f;
    private bool isWaiting = false;
    private GridGenerator currentRoomGridGenerator; // Need this to find valid nodes

    // Variables from original script (keep if needed for other logic, otherwise remove)
    // private Vector3 _targetPosition;
    // private Vector3 _direction;


    public override void Initialize(GameObject gameObject, Enemy enemy)
    {
        base.Initialize(gameObject, enemy);
        // We need the grid generator reference from the Enemy script
        currentRoomGridGenerator = enemy.GetComponentInParent<GridGenerator>(); // Or however Enemy script gets it
         if (currentRoomGridGenerator == null) Debug.LogError($"EnemyIdleRandomWanderer: Could not find GridGenerator for {enemy.name}", enemy);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        enemy.StopPathfinding(); // Ensure stopped from previous state
        isWaiting = false;
        currentWaitTimer = 0f;
        currentWanderTimer = 0f;
        FindAndSetWanderPath(); // Find the first wander point immediately
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic(); // Handles the IsAggroed check and transition to ChaseState

        // Don't do wander logic if we are transitioning out (handled by base class)
        if (enemy.IsAggroed) return;

        if (isWaiting)
        {
            // Waiting at a destination
            currentWaitTimer += Time.deltaTime;
            if (currentWaitTimer >= waitTimeAtDestination)
            {
                isWaiting = false;
                FindAndSetWanderPath(); // Find a new point
            }
        }
        else // If not waiting, we are moving (or trying to)
        {
             currentWanderTimer += Time.deltaTime;

            // Check if AgentMover reached its destination (!isFollowingPath)
            // Or if we've wandered too long without reaching it
            if ((enemy.agentMover != null && !enemy.agentMover.isFollowingPath && currentWanderTimer > 0.1f /* small delay to avoid instant trigger */)
                || currentWanderTimer >= maxWanderTime )
            {
                 // Reached destination or timed out
                // Debug.Log($"[{enemy.name}] Reached wander point or timed out.");
                enemy.StopPathfinding(); // Stop current movement
                isWaiting = true;       // Start waiting timer
                currentWaitTimer = 0f;
            }
        }

        // REMOVED: Direct movement logic from original script
        // _direction = (_targetPosition - enemy.transform.position).normalized;
        // enemy.MoveEnemy(RandomMovementSpeed * _direction);
        // if ((enemy.transform.position - _targetPosition).sqrMagnitude < 0.1f) { ... }
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        enemy.StopPathfinding(); // Ensure movement stops when leaving idle wander
    }

    private void FindAndSetWanderPath()
    {
        if (currentRoomGridGenerator == null)
        {
            Debug.LogWarning($"[{enemy.name}] Cannot wander, GridGenerator not found.");
            isWaiting = true; // Go back to waiting if grid is missing
            currentWaitTimer = 0f;
            return;
        }

        Vector2 randomDirection = Random.insideUnitCircle * wanderRadius;
        Vector3 targetPosition = enemy.transform.position + (Vector3)randomDirection;

        // Find the nearest valid node to the random point
        Node targetNode = currentRoomGridGenerator.GetNodeFromWorldPoint(targetPosition);

        // Check if the node is valid and different from the current closest node to avoid short paths
        Node currentNode = currentRoomGridGenerator.GetNodeFromWorldPoint(enemy.transform.position);
        if (targetNode != null && !targetNode.isObstacle && targetNode != currentNode)
        {
            // Use the non-periodic RequestPath from Enemy.cs
            enemy.RequestPath(targetNode.transform.position);
            enemy.agentMover.canMove = true; // Allow movement for this path
            currentWanderTimer = 0f; // Reset wander timer
            isWaiting = false; // Ensure we are not waiting anymore
            // Debug.Log($"[{enemy.name}] Wandering towards {targetNode.name}");
        }
        else
        {
            // Failed to find a valid *different* node, maybe try again immediately or wait?
            // Trying again immediately could cause performance issues if space is tight.
            // Let's just wait for the next cycle.
            // Debug.LogWarning($"[{enemy.name}] Could not find valid wander node near {targetPosition} or it was the same as current. Waiting...");
            isWaiting = true; // Start waiting timer early if no valid point found
            currentWaitTimer = 0f;
            enemy.StopPathfinding(); // Make sure enemy stops if it can't find a path
        }
    }

     public override void DoPhysicsLogic()
     {
         base.DoPhysicsLogic();
         // Movement handled by AgentMover
     }

    // REMOVED: GetRandomPointInCircle() - logic is now inside FindAndSetWanderPath
}