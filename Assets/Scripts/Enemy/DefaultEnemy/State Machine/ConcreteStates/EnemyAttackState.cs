// Full Path: Assets/Scripts/Enemy/DefaultEnemy/State Machine/ConcreteStates/EnemyAttackState.cs
using UnityEngine;

public class EnemyAttackState : EnemyState
{
    private float shootTimer;
    private EnemyAttackCircleAndShoot attackData; // SO for config data

    // State variables for direction switching
    private float circleDirectionTimer;
    private bool currentCircleDirectionClockwise = true;

    // Buffer distance for exiting attack state (hysteresis)
    // This value could also be moved to the Enemy script or the attackData SO if needed per enemy type
    private float exitAttackRangeBuffer = 1.0f; // How much further than preferred range player must be to exit Attack

    // Constructor
    public EnemyAttackState(Enemy enemy, EnemyStateMachine enemyStateMachine) : base(enemy, enemyStateMachine)
    {
        attackData = enemy.EnemyAttackBaseInstance as EnemyAttackCircleAndShoot;

        // Error handling for incorrect SO assignment
        if (attackData == null && enemy.EnemyAttackBaseInstance != null) {
            Debug.LogError($"EnemyAttackState on {enemy.name} received '{enemy.EnemyAttackBaseInstance.GetType().Name}' but needs 'EnemyAttackCircleAndShoot'!", enemy);
        } else if (attackData == null) {
             Debug.LogError($"EnemyAttackState on {enemy.name} requires 'EnemyAttackCircleAndShoot' assigned!", enemy);
        }
    }

    // On entering state
    public override void EnterState()
    {
        base.EnterState();
        Debug.Log($"==== {enemy.name} entering Attack State ====");
        shootTimer = 0f;
        circleDirectionTimer = 0f;

        // Determine initial circle direction
        if (attackData != null) {
             attackData.DoEnterLogic();
             currentCircleDirectionClockwise = (attackData.GetCircleDirection() == 1);
        } else {
             currentCircleDirectionClockwise = (Random.value > 0.5f); // Fallback
        }
         // Debug.Log($"AttackState: Initial circle direction clockwise: {currentCircleDirectionClockwise}"); // Less spammy logs

        // Configure Movement
        if (enemy.agentMover != null)
            enemy.agentMover.canMove = true;
        else
             Debug.LogError($"Enemy {enemy.name} has no AgentMover component!", enemy);

        // Configure Steering Behaviours
        if (enemy.seekBehaviour != null) enemy.seekBehaviour.enabled = false;
        if (enemy.circleTargetBehaviour != null) {
            enemy.circleTargetBehaviour.enabled = true;
            enemy.circleTargetBehaviour.clockwise = currentCircleDirectionClockwise;
        } else { Debug.LogWarning($"Enemy {enemy.name} missing CircleTargetBehaviour!", enemy); }
        if (enemy.obstacleAvoidanceBehaviour != null) enemy.obstacleAvoidanceBehaviour.enabled = true;
        if (enemy.wallFollowingBehaviour != null) enemy.wallFollowingBehaviour.enabled = true; // Keep wall following active
    }

    // On exiting state
    public override void ExitState()
    {
        base.ExitState();
        Debug.Log($"==== {enemy.name} exiting Attack State ====");

        // Reset Steering Behaviours
        if (enemy.seekBehaviour != null) enemy.seekBehaviour.enabled = true;
        if (enemy.circleTargetBehaviour != null) enemy.circleTargetBehaviour.enabled = false;

        attackData?.DoExitLogic();
    }

    // Per frame logic
    public override void FrameUpdate()
    {
        base.FrameUpdate();
        if (attackData == null) return; // Cannot proceed without data

        // --- Handle Circle Direction Switching ---
        circleDirectionTimer += Time.deltaTime;
        // Read the interval from the Scriptable Object (attackData)
        if (circleDirectionTimer >= attackData.circleSwitchInterval)
        {
            circleDirectionTimer = 0f;
            currentCircleDirectionClockwise = !currentCircleDirectionClockwise;
            if (enemy.circleTargetBehaviour != null)
            {
                enemy.circleTargetBehaviour.clockwise = currentCircleDirectionClockwise;
                // Debug.Log($"AttackState ({enemy.name}): Switching circle direction..."); // Keep logs minimal
            }
        }

        // --- Handle Shooting Logic ---
        shootTimer += Time.deltaTime;
        bool shootIntervalReached = shootTimer >= attackData.timeBetweenAttacks;

        if (shootIntervalReached) {
            bool hasLoS = HasLineOfSight();
            // Debug.Log($"AttackState ({enemy.name}): Shoot timer reached. HasLoS: {hasLoS}"); // Keep logs minimal

            if (hasLoS) {
                PerformShoot();
            }
            shootTimer = 0f; // Reset timer after check
        }


        // --- Check for State Transitions with Hysteresis ---
        // Calculate current distance (only if needed for check)
        float distanceToPlayer = -1f;
        bool checkDistance = !enemy.IsWithInAttackDistance; // Only calculate distance if trigger says we are out

        if (checkDistance && Player.Instance != null) {
            distanceToPlayer = Vector2.Distance(enemy.transform.position, Player.Instance.transform.position);
        }

        // Exit Condition: Player is flagged as outside attack trigger *AND* is beyond preferred range + buffer
        if (checkDistance && distanceToPlayer > (attackData.preferredShootingRange + exitAttackRangeBuffer))
        {
             Debug.Log($"AttackState ({enemy.name}): Player out of buffered range ({distanceToPlayer:F2} > {attackData.preferredShootingRange + exitAttackRangeBuffer:F2}), changing to Chase State.");
            enemy.stateMachine.ChangeState(enemy.ChaseState);
            return;
        }
        // Optional log for debugging hysteresis:
        // else if (checkDistance) { // If trigger is out, but distance keeps us in
        //     Debug.Log($"AttackState ({enemy.name}): Player outside trigger but within buffer ({distanceToPlayer:F2} <= {attackData.preferredShootingRange + exitAttackRangeBuffer:F2}). Staying in Attack.");
        // }
    }

    // Checks Line of Sight
    private bool HasLineOfSight()
    {
        if (Player.Instance == null) return false;
        Vector2 startPoint = enemy.transform.position;
        Vector2 targetPoint = Player.Instance.transform.position;
        Vector2 direction = targetPoint - startPoint;
        float distance = direction.magnitude;
        if(distance < 0.1f) return true;
        RaycastHit2D hit = Physics2D.Raycast(startPoint, direction.normalized, distance, enemy.LineOfSightMask);
        // Debug.DrawRay(startPoint, direction.normalized * distance, (hit.collider == null) ? Color.green : Color.red);
        // if (hit.collider != null) {
        //     Debug.Log($"AttackState ({enemy.name}): LoS blocked by '{hit.collider.name}'"); // Keep logs minimal
        // }
        return hit.collider == null;
     }

    // Fires Projectile
    private void PerformShoot()
    {
        // Debug.Log($"AttackState ({enemy.name}): Attempting PerformShoot..."); // Keep logs minimal
        if (attackData == null || attackData.bulletPrefab == null || Player.Instance == null || PoolManager.Instance == null) {
             Debug.LogError($"AttackState ({enemy.name}): Cannot PerformShoot - missing critical data!");
             return;
        }
        Vector2 directionToPlayer = (Player.Instance.transform.position - enemy.transform.position).normalized;
        GameObject bulletObj = PoolManager.Instance.Spawn(attackData.bulletPrefab, enemy.transform.position, Quaternion.identity);
        EnemyProjectile bullet = bulletObj.GetComponent<EnemyProjectile>();
        if (bullet != null) {
            bullet.Initialize(attackData.bulletPrefab);
            bullet.SetDirection(directionToPlayer, attackData.bulletSpeed);
             // Debug.Log($"AttackState ({enemy.name}): Fired!"); // Keep logs minimal
        } else {
             Debug.LogError($"Bullet prefab '{attackData.bulletPrefab.name}' is missing EnemyProjectile script!", attackData.bulletPrefab);
        }
     }

    // Per fixed physics update
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        attackData?.DoPhysicsUpdateLogic();
    }
}