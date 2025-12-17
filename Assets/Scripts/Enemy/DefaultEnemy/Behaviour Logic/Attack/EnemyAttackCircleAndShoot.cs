// Full Path: Assets/Scripts/Enemy/DefaultEnemy/Behaviour Logic/Attack/EnemyAttackCircleAndShoot.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Attack_CircleAndShoot_Data", menuName = "Enemy Logic/Attack Logic Data/Circle and Shoot Data")]
public class EnemyAttackCircleAndShoot : EnemyAttackSOBase
{
    [Header("Shooting Config")]
    public GameObject bulletPrefab;
    public float timeBetweenAttacks = 1f; // Used here for shooting timer
    public float bulletSpeed = 10f;       // Used here for PerformShoot

    [Header("Circling Config")]
    // --- THIS IS THE FIX ---
    // REMOVE the 'preferredShootingRange' line from here.
    // It is inherited from EnemyAttackSOBase and you set its value (e.g., to 5)
    // on the "Attack_CircleAndShoot_Data.asset" file in the Unity Inspector.
    // --- END FIX ---
    public float minimumDistance = 4f;        // Minimum distance to maintain
    [Tooltip("Minimum time (seconds) to circle in one direction.")]
    public float minCircleSwitchInterval = 5.0f; 
    [Tooltip("Maximum time (seconds) to circle in one direction.")]
    public float maxCircleSwitchInterval = 10.0f;

    [Tooltip("How frequently to recalculate the next point on the circle (seconds).")]

    public float circlePathUpdateInterval = 0.2f;

    // Runtime variables
    private float _internalCircleDirection = 1;
    private float circleDirectionTimer = 0f;
    private float circlePathUpdateTimer = 0f;
    private float shootTimer = 0f; // <<< ADDED shooting timer here
    // No need to store GridGenerator ref, access via enemy.currentRoomGridGenerator
    private float _currentCircleSwitchInterval;
    // Initialize is handled by base class
    // public override void Initialize(GameObject ownerGameObject, Enemy ownerEnemy) { base.Initialize(ownerGameObject, ownerEnemy); }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        _internalCircleDirection = (Random.value > 0.5f) ? 1 : -1;
        circleDirectionTimer = 0f;
        circlePathUpdateTimer = circlePathUpdateInterval; // Trigger immediate update
        shootTimer = 0f; // <<< Reset shoot timer on enter

        // Randomize initial circle switch interval
        _currentCircleSwitchInterval = Random.Range(minCircleSwitchInterval, maxCircleSwitchInterval);
        enemy?.StopPathfinding();
        Debug.Log($"Stopping existing pathfinding for circling logic.");
        if (enemy?.agentMover != null) enemy.agentMover.canMove = true;

        // Request first path
       if (enemy.playerTransform != null && enemy.currentRoomGridGenerator != null)
        {
            CalculateAndRequestCirclePath();
            Debug.Log($"[{enemy.name}] CircleAndShoot SO: Initial circling path requested.");
        }
        else
        { 
            /* Error Logging */ 
            // Updated error message to be more specific
            Debug.LogError($"[{enemy?.name}] CircleAndShoot SO: Missing enemy.playerTransform or enemy.currentRoomGridGenerator on enter.");
        }

    }

    // Public getter for external logic if needed
    public int GetCircleDirection()
     {
        return (int)_internalCircleDirection;
    }

    // Called every frame by EnemyAttackState's FrameUpdate
    public override void DoFrameUpdateLogic()
    {
        // base.DoFrameUpdateLogic(); // Base logic is empty

        if (enemy == null || playerTransform == null || enemy.currentRoomGridGenerator == null || enemy.agentMover == null) return;

        // --- Circle Direction Switching ---
        circleDirectionTimer += Time.deltaTime;
        if (circleDirectionTimer >= _currentCircleSwitchInterval) 
        {
            circleDirectionTimer = 0f;
            _internalCircleDirection *= -1; 
            _currentCircleSwitchInterval = Random.Range(minCircleSwitchInterval, maxCircleSwitchInterval);
        }

        // --- Pathfinding Update Timer ---
        circlePathUpdateTimer += Time.deltaTime;
        if (circlePathUpdateTimer >= circlePathUpdateInterval) {
            circlePathUpdateTimer = 0f;
            CalculateAndRequestCirclePath();
            Debug.Log($"[{enemy.name}] CircleAndShoot SO: Updated circling path.");
        }

        // --- MOVED: Shooting Logic ---
        shootTimer += Time.deltaTime;
        // Check if SO has shooting configured (timeBetweenAttacks > 0)
        if (timeBetweenAttacks > 0 && shootTimer >= timeBetweenAttacks)
        {
            if (HasLineOfSight()) // Use helper method below
            {
                PerformShoot(); // Use helper method below
            }
            shootTimer = 0f; // Reset timer regardless of LoS
        }
        // --- END MOVED ---

        // Transition logic remains in EnemyAttackState
    }

    // Helper method to calculate the next point on the circle and request path
    private void CalculateAndRequestCirclePath()
    {
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        float currentDistance = directionToPlayer.magnitude;
        if (currentDistance < 0.01f) currentDistance = 0.01f;

        // This line now works because 'preferredShootingRange' is inherited
        Vector3 desiredPosition = playerTransform.position - directionToPlayer.normalized * preferredShootingRange;
        Vector3 tangentDirection = Vector3.Cross(directionToPlayer.normalized, Vector3.forward);
        tangentDirection *= _internalCircleDirection;

        Node startNode = enemy.currentRoomGridGenerator.GetNodeFromWorldPoint(transform.position);

        float [] lookAheadMultipliers = new float [] {3.5f, 2.0f , 1.0f};
        Node bestTargetNode = null;
        foreach (float multiplier in lookAheadMultipliers)
        {
            float checkDistance = (enemy.agentMover != null ? enemy.agentMover.moveSpeed : 3f) * circlePathUpdateInterval * multiplier;
            Debug.DrawRay(transform.position, tangentDirection * checkDistance, Color.cyan, 0.1f); // Visualize check direction
            Debug.Log($"[{enemy.name}] CircleAndShoot SO: Checking circle target at distance {checkDistance} (multiplier {multiplier}).");
            Vector3 potentialTarget = desiredPosition + tangentDirection * checkDistance;

            // Apply "Push Away" logic to the potential point
            if (currentDistance < minimumDistance)
            {
                Vector3 pushAway = (transform.position - playerTransform.position).normalized * (minimumDistance - currentDistance);
                potentialTarget += pushAway * 0.5f;
            }
            //Check 1
            Node destNode = enemy.currentRoomGridGenerator.GetNodeFromWorldPoint(potentialTarget);
            
            // Check if node exists, is walkable, and is NOT occupied by another enemy (High Penalty)
            if (destNode == null || destNode.isObstacle || destNode.movementPenalty > 10) 
            {
                continue; // Destination is bad, try a shorter jump
            }
            //Check 2
            Vector3 midPoint =  Vector3.Lerp(transform.position, potentialTarget, 0.5f);
            Node midNode = enemy.currentRoomGridGenerator.GetNodeFromWorldPoint(midPoint);
            if (midNode == null || midNode.isObstacle || midNode.movementPenalty > 10) 
            {
                continue; // Midpoint is bad, try a shorter jump
            }
            // If both checks passed, select this node
            bestTargetNode = destNode;
            break;
        }

        // 3. Execution
        

        if (bestTargetNode != null)
        {
            enemy.RequestPath(bestTargetNode.transform.position);
            if (enemy.agentMover != null) enemy.agentMover.canMove = true;
        }
        else
        {
            // All forward spots are blocked by walls or enemies.
            // Fallback: Just try to get to the 'perfect circle' spot (0 lookahead) to maintain range.
            Node fallbackNode = enemy.currentRoomGridGenerator.GetNodeFromWorldPoint(desiredPosition);
            if (fallbackNode != null && !fallbackNode.isObstacle)
            {
                enemy.RequestPath(fallbackNode.transform.position);
            }
        }
    
        // --- SMOOTHNESS FIX: Calculate a longer look-ahead distance for smoother paths ---
        // Instead of looking ahead just 1.5x the move speed, we look further to get a multi-node path
        /*float lookAheadDistance = (enemy.agentMover != null ? enemy.agentMover.moveSpeed : 3f) * circlePathUpdateInterval * 3.5f; // Increased from 1.5f to 3.5f
        Vector3 circleTargetPoint = desiredPosition + tangentDirection * lookAheadDistance;

        if(currentDistance < minimumDistance) {
            Vector3 pushAway = (transform.position - playerTransform.position).normalized * (minimumDistance - currentDistance);
            circleTargetPoint += pushAway * 0.5f;
        }

        Node targetNode = enemy.currentRoomGridGenerator.GetNodeFromWorldPoint(circleTargetPoint);

        if (targetNode != null && !targetNode.isObstacle)
        {
            enemy.RequestPath(targetNode.transform.position);
            Debug.Log($"[{enemy.name}] CircleAndShoot SO: Requested path to circling point at {targetNode.transform.position} (smooth multi-node path).");
            if (enemy.agentMover != null) enemy.agentMover.canMove = true;
        }
        else
        {
            Node fallbackNode = enemy.currentRoomGridGenerator.GetNodeFromWorldPoint(desiredPosition);
            if (fallbackNode != null && !fallbackNode.isObstacle)
            {
                enemy.RequestPath(fallbackNode.transform.position);
                if (enemy.agentMover != null) enemy.agentMover.canMove = true;
                Debug.LogWarning($"[{enemy.name}] CircleAndShoot SO: Fallback to desired position node for circling path.");
            }
            else
            {
                enemy.StopPathfinding();
                Debug.LogWarning($"[{enemy.name}] CircleAndShoot SO: No valid node found for circling path.");
            }
        }
        */
    }

    // --- ADDED: Helper Methods for Shooting (Moved from State) ---
    private bool HasLineOfSight()
    {
        if (playerTransform == null || enemy == null) return false;
        Vector2 startPoint = transform.position; // Use SO's transform ref
        Vector2 targetPoint = playerTransform.position;
        Vector2 direction = targetPoint - startPoint;
        float distance = direction.magnitude;
        if (distance < 0.1f) return true; // Close enough, assume LoS
        // Use the LineOfSightMask from the Enemy script
        RaycastHit2D hit = Physics2D.Raycast(startPoint, direction.normalized, distance, enemy.LineOfSightMask);
        // Debug.DrawRay(startPoint, direction.normalized * distance, (hit.collider == null) ? Color.green : Color.yellow, 0.02f);
        return hit.collider == null; // True if nothing hit
    }

    private void PerformShoot()
    {
        // Use fields directly from this SO instance (bulletPrefab, bulletSpeed)
        if (bulletPrefab == null || playerTransform == null || PoolManager.Instance == null || enemy == null) {
            Debug.LogError($"[{enemy?.name}] PerformShoot in SO missing refs: Bullet:{bulletPrefab!=null}, Player:{playerTransform!=null}, Pool:{PoolManager.Instance!=null}");
            return;
        }

        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        // Consider adding a FirePoint Transform reference to Enemy.cs or passing it via Initialize?
        GameObject bulletObj = PoolManager.Instance.Spawn(bulletPrefab, transform.position, Quaternion.identity);
        EnemyProjectile bullet = bulletObj.GetComponent<EnemyProjectile>();
        if (bullet != null)
        {
            bullet.Initialize(bulletPrefab);
            bullet.SetDirection(directionToPlayer, bulletSpeed); // Use bulletSpeed from this SO
            // Debug.Log($"[{enemy.name}] Fired projectile from SO!");
        }
        else { Debug.LogError($"Bullet prefab '{bulletPrefab.name}' missing EnemyProjectile script!", bulletPrefab); }
    }
    // --- END ADDED ---

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        enemy?.StopPathfinding();
        // Debug.Log($"[{enemy?.name}] CircleAndShoot SO: Exiting logic.");
    }

    public override void DoPhysicsUpdateLogic() { /* Intentionally empty */ } // Renamed to match base class
    public override void ResetValues() { /* Reset internal SO state if needed */ }
}