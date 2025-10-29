// Full Path: Assets/Scripts/Enemy/DefaultEnemy/Behaviour Logic/Attack/EnemyAttackCircleAndShoot.cs
using UnityEngine;
using System.Collections.Generic; // Keep if PoolManager might need it later

[CreateAssetMenu(fileName = "Attack_CircleAndShoot_Data", menuName = "Enemy Logic/Attack Logic Data/Circle and Shoot Data")]
public class EnemyAttackCircleAndShoot : EnemyAttackSOBase
{
    [Header("Shooting Config")]
    public GameObject bulletPrefab;
    public float timeBetweenAttacks = 1f; // Used here for shooting timer
    public float bulletSpeed = 10f;       // Used here for PerformShoot

    [Header("Circling Config")]
    public float preferredShootingRange = 5f; // Used here for movement calculation AND by State for transitions
    public float minimumDistance = 4f;        // Minimum distance to maintain
    public float circleSwitchInterval = 5.0f; // How often to switch circling direction
    [Tooltip("How frequently to recalculate the next point on the circle (seconds).")]
    public float circlePathUpdateInterval = 0.2f;

    // Runtime variables
    private float _internalCircleDirection = 1;
    private float circleDirectionTimer = 0f;
    private float circlePathUpdateTimer = 0f;
    private float shootTimer = 0f; // <<< ADDED shooting timer here
    // No need to store GridGenerator ref, access via enemy.currentRoomGridGenerator

    // Initialize is handled by base class
    // public override void Initialize(GameObject ownerGameObject, Enemy ownerEnemy) { base.Initialize(ownerGameObject, ownerEnemy); }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        _internalCircleDirection = (Random.value > 0.5f) ? 1 : -1;
        circleDirectionTimer = 0f;
        circlePathUpdateTimer = circlePathUpdateInterval; // Trigger immediate update
        shootTimer = 0f; // <<< Reset shoot timer on enter

        enemy?.StopPathfinding();
        if (enemy?.agentMover != null) enemy.agentMover.canMove = true;

        // Request first path
        if (playerTransform != null && enemy?.currentRoomGridGenerator != null) {
            CalculateAndRequestCirclePath();
        } else { /* Error Logging */ }
    }

    // Public getter for external logic if needed
    public int GetCircleDirection() { return (int)_internalCircleDirection; }

    // Called every frame by EnemyAttackState's FrameUpdate
    public override void DoFrameUpdateLogic()
    {
        // base.DoFrameUpdateLogic(); // Base logic is empty

        if (enemy == null || playerTransform == null || enemy.currentRoomGridGenerator == null || enemy.agentMover == null) return;

        // --- Circle Direction Switching ---
        circleDirectionTimer += Time.deltaTime;
        if (circleDirectionTimer >= circleSwitchInterval) {
            circleDirectionTimer = 0f;
            _internalCircleDirection *= -1;
        }

        // --- Pathfinding Update Timer ---
        circlePathUpdateTimer += Time.deltaTime;
        if (circlePathUpdateTimer >= circlePathUpdateInterval) {
            circlePathUpdateTimer = 0f;
            CalculateAndRequestCirclePath();
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

        Vector3 desiredPosition = playerTransform.position - directionToPlayer.normalized * preferredShootingRange;
        Vector3 tangentDirection = Vector3.Cross(directionToPlayer.normalized, Vector3.forward);
        tangentDirection *= _internalCircleDirection;

        float lookAheadDistance = (enemy.agentMover != null ? enemy.agentMover.moveSpeed : 3f) * circlePathUpdateInterval * 1.5f;
        Vector3 circleTargetPoint = desiredPosition + tangentDirection * lookAheadDistance;

        if(currentDistance < minimumDistance) {
            Vector3 pushAway = (transform.position - playerTransform.position).normalized * (minimumDistance - currentDistance);
            circleTargetPoint += pushAway * 0.5f;
        }

        Node targetNode = enemy.currentRoomGridGenerator.GetNodeFromWorldPoint(circleTargetPoint);

        if (targetNode != null && !targetNode.isObstacle) {
            enemy.RequestPath(targetNode.transform.position);
            if (enemy.agentMover != null) enemy.agentMover.canMove = true;
        } else {
            Node fallbackNode = enemy.currentRoomGridGenerator.GetNodeFromWorldPoint(desiredPosition);
            if (fallbackNode != null && !fallbackNode.isObstacle) {
                enemy.RequestPath(fallbackNode.transform.position);
                 if (enemy.agentMover != null) enemy.agentMover.canMove = true;
            } else {
                enemy.StopPathfinding();
            }
        }
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