using UnityEngine;

[CreateAssetMenu(fileName = "ChargerIdle", menuName = "Enemy Logic/Idle Logic/Charger Idle")]
public class ChargerIdleBehavior : EnemyIdleSOBase
{
    [Header("Wander")]
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float minDirectionDuration = 1.5f;
    [SerializeField] private float maxDirectionDuration = 3f;
    [SerializeField] private float wallCheckDistance = 0.8f;

    [Header("Forward Vision Ray")]
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask playerLayer;

    private static readonly Vector2[] Cardinals =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    private Vector2 currentDirection = Vector2.right;
    private float directionTimer;
    private float currentDirDuration;

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        if (enemy.cardinalMover != null)
        {
            enemy.cardinalMover.moveSpeed = wanderSpeed;
            enemy.cardinalMover.canMove = true;
        }

        PickNewDirection();
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        if (enemy.cardinalMover != null)
            enemy.cardinalMover.StopMovement();
    }

    public override void DoFrameUpdateLogic()
    {
        // NOTE: don't call base — base transitions to ChaseState, charger skips Chase.

        // Forward vision — the single ray in the direction the charger is walking
        if (CheckForwardVision())
        {
            enemy.SetAggroStatus(true);
            enemy.stateMachine.ChangeState(enemy.AttackState);
            return;
        }

        // External aggro fallback (if anything sets IsAggroed elsewhere)
        if (enemy.IsAggroed)
        {
            enemy.stateMachine.ChangeState(enemy.AttackState);
            return;
        }

        // Wall ahead — pick a new cardinal direction
        if (IsWallInDirection(currentDirection))
        {
            PickNewDirection();
            return;
        }

        // Timer expired — pick a new cardinal direction
        directionTimer += Time.deltaTime;
        if (directionTimer >= currentDirDuration)
            PickNewDirection();
    }

    private void PickNewDirection()
    {
        int startIndex = Random.Range(0, 4);
        for (int i = 0; i < 4; i++)
        {
            Vector2 candidate = Cardinals[(startIndex + i) % 4];
            // Prefer not to reverse straight back
            if (candidate == -currentDirection && i < 3) continue;
            if (!IsWallInDirection(candidate))
            {
                ApplyDirection(candidate);
                return;
            }
        }

        // All blocked — apply anything so we're not frozen
        ApplyDirection(Cardinals[Random.Range(0, 4)]);
    }

    private void ApplyDirection(Vector2 dir)
    {
        currentDirection = dir;
        directionTimer = 0f;
        currentDirDuration = Random.Range(minDirectionDuration, maxDirectionDuration);
        if (enemy.cardinalMover != null)
            enemy.cardinalMover.SetDirection(dir);
    }

    private bool IsWallInDirection(Vector2 dir)
    {
        Vector2 origin = (Vector2)enemy.transform.position + dir * 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, wallCheckDistance, obstacleLayer);
        return hit.collider != null;
    }

    private bool CheckForwardVision()
    {
        Vector2 origin = (Vector2)enemy.transform.position + currentDirection * 0.6f;
        RaycastHit2D hit = Physics2D.Raycast(
            origin, currentDirection, visionRange, playerLayer | obstacleLayer);
        Debug.DrawRay(origin, currentDirection * visionRange, Color.red);
        return hit.collider != null && hit.collider.CompareTag("Player");
    }
}
