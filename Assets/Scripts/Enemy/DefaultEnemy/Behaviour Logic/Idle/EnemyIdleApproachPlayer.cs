using UnityEngine;

[CreateAssetMenu(fileName = "Idle_ApproachPlayer_Raycast", menuName = "Enemy Logic/Idle Logic/Approach Player (Raycast)")]
public class EnemyIdleApproachPlayer : EnemyIdleSOBase
{
    [Header("Movement")]
    [SerializeField] private float approachSpeed = 3f;

    [Header("Obstacle Avoidance (3-Ray System)")]
    [SerializeField, Tooltip("Layer(s) to consider obstacles. IMPORTANT: Add your 'Enemy' layer here too to avoid clumping.")]
    private LayerMask obstacleLayerMask;
    
    [SerializeField, Tooltip("How far forward the rays will check for obstacles.")]
    private float raycastDistance = 1f;
    
    [SerializeField, Tooltip("How far to the side the 'head' and 'legs' rays are from the center 'body' ray.")]
    private float raySpread = 0.3f;

    [Header("Avoidance Tuning")]
    [SerializeField, Range(0f, 1f), Tooltip("How closely the enemy must be facing the player after clearing a wall to stop sliding. 0.8 = good default.")]
    private float clearPathDotThreshold = 0.8f;

    [SerializeField, Tooltip("Failsafe. How long (in seconds) the enemy will slide with clear rays before giving up. Prevents infinite loops.")]
    private float maxSlideDuration = 2.0f;

    // --- STATE VARIABLES ---
    // (These are instance-specific because Enemy.cs clones this asset)
    private bool isAvoiding = false;
    private Vector2 lastSlideDirection;
    private float slideTimer; // Failsafe timer

    public override void Initialize(GameObject g, Enemy e)
    {
        base.Initialize(g, e);
        // Ensure state is reset for each enemy
        isAvoiding = false;
        lastSlideDirection = Vector2.zero;
        slideTimer = 0f;
    }

    public override void DoFrameUpdateLogic()
    {
        if (playerTransform == null) return;

        Vector2 desiredMoveDirection = (playerTransform.position - enemy.transform.position).normalized;
        Vector2 transformPos = enemy.transform.position;

        Vector2 perpendicularRight = new Vector2(desiredMoveDirection.y, -desiredMoveDirection.x);
        Vector2 centerOrigin = transformPos;
        Vector2 rightOrigin = transformPos + perpendicularRight * raySpread;
        Vector2 leftOrigin = transformPos - perpendicularRight * raySpread;

        bool oldQueriesStartInColliders = Physics2D.queriesStartInColliders;
        Physics2D.queriesStartInColliders = false;

        RaycastHit2D hitCenter = Physics2D.Raycast(centerOrigin, desiredMoveDirection, raycastDistance, obstacleLayerMask);
        RaycastHit2D hitRight = Physics2D.Raycast(rightOrigin, desiredMoveDirection, raycastDistance, obstacleLayerMask);
        RaycastHit2D hitLeft = Physics2D.Raycast(leftOrigin, desiredMoveDirection, raycastDistance, obstacleLayerMask);

        Physics2D.queriesStartInColliders = oldQueriesStartInColliders;

        Debug.DrawRay(centerOrigin, desiredMoveDirection * raycastDistance, (hitCenter.collider != null) ? Color.red : Color.green);
        Debug.DrawRay(rightOrigin, desiredMoveDirection * raycastDistance, (hitRight.collider != null) ? Color.red : Color.green);
        Debug.DrawRay(leftOrigin, desiredMoveDirection * raycastDistance, (hitLeft.collider != null) ? Color.red : Color.green);

        RaycastHit2D primaryHit = default(RaycastHit2D);
        bool obstacleHit = (hitCenter.collider != null || hitRight.collider != null || hitLeft.collider != null);

        if (hitCenter.collider != null) { primaryHit = hitCenter; }
        else if (hitRight.collider != null) { primaryHit = hitRight; }
        else if (hitLeft.collider != null) { primaryHit = hitLeft; }
        

        Vector2 finalVelocity;

        if (obstacleHit)
        {
            // --- STATE: AVOID ---
            // We hit a wall. Enter or stay in avoiding state.
            isAvoiding = true;
            slideTimer = 0f; // Reset the failsafe timer every time we hit a wall
            
            Vector2 slideDirection = new Vector2(primaryHit.normal.y, -primaryHit.normal.x).normalized;
            
            if (slideDirection.sqrMagnitude < 0.1f)
            {
                slideDirection = new Vector2(0, 1); // Failsafe
            }

            lastSlideDirection = slideDirection;
            finalVelocity = lastSlideDirection * approachSpeed;
            Debug.Log("Obstacle detected. Setting slide direction to: " + lastSlideDirection);
        }
        else
        {
            // --- STATE: SEEK (or transition) ---
            if (isAvoiding)
            {
                // We *were* avoiding, but now the rays are clear.
                slideTimer += Time.deltaTime; // Start the failsafe timer

                // Check for Failsafe:
                if (slideTimer > maxSlideDuration)
                {
                    isAvoiding = false;
                    finalVelocity = desiredMoveDirection * approachSpeed;
                    Debug.Log("Max slide time reached. Failsafe triggered. Resetting state.");
                }
                // Check for Corner Clear:
                else if (Vector2.Dot(lastSlideDirection, desiredMoveDirection) > clearPathDotThreshold)
                {
                    isAvoiding = false;
                    finalVelocity = desiredMoveDirection * approachSpeed;
                    Debug.Log("Path clear. Switching to SEEK state.");
                }
                // Continue Sliding:
                else
                {
                    // We are clear of the raycast, but the player is still "around the corner".
                    finalVelocity = lastSlideDirection * approachSpeed;
                    Debug.Log("Rays clear, but player is not aligned. Continuing slide.");
                }
            }
            else
            {
                // We are not avoiding, and the path is clear.
                finalVelocity = desiredMoveDirection * approachSpeed;
                Debug.Log("Path clear. Seeking player.");
            }
        }

        enemy.MoveEnemy(finalVelocity);

        if (enemy.IsWithInAttackDistance)
        {
            enemy.stateMachine.ChangeState(enemy.AttackState);
        }
    }
}