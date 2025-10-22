using UnityEngine;

[CreateAssetMenu(fileName = "Idle_ApproachPlayer", menuName = "Enemy Logic/Idle Logic/Approach Player")]
public class EnemyIdleApproachPlayer : EnemyIdleSOBase
{
    [Header("Movement")]
    [SerializeField] private float approachSpeed = 3f;
    [SerializeField, Tooltip("How quickly the enemy turns towards the desired direction. Lower values are slower/smoother.")]
    private float turnSpeed = 5f; // Added for smoothing

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private float obstacleCheckDistance = 1f;
    // --- FIX: Re-added collider size reduction ---
    [SerializeField, Tooltip("How much smaller the detection box should be than the feeler collider.")]
    private float colliderSizeReduction = 0.05f;

    public override void DoFrameUpdateLogic()
    {
        Vector2 desiredMoveDirection = (playerTransform.position - enemy.transform.position).normalized;

        Vector2 boxSize = enemy.avoidanceCollider.size - Vector2.one * colliderSizeReduction; // Use reduction
        RaycastHit2D hit = Physics2D.BoxCast(enemy.transform.position, boxSize, 0f, desiredMoveDirection, obstacleCheckDistance, obstacleLayerMask);

        Vector2 targetMoveDirection;

        if (hit.collider != null)
        {
            Vector2 slideDirection = desiredMoveDirection - (Vector2)Vector3.Project(desiredMoveDirection, hit.normal);
            
            // --- FIX: Prevent normalizing zero vector ---
            if (slideDirection.sqrMagnitude > Mathf.Epsilon) // Epsilon is a very small number
            {
                targetMoveDirection = slideDirection.normalized;
            }
            else
            {
                // If projection results in zero, try moving perpendicular to the normal
                targetMoveDirection = new Vector2(hit.normal.y, -hit.normal.x);
                 // Optional: Check dot product again if needed, depends on geometry
                 if (Vector2.Dot(targetMoveDirection, desiredMoveDirection) < 0) {
                      targetMoveDirection = -targetMoveDirection;
                 }
            }
        }
        else
        {
            targetMoveDirection = desiredMoveDirection;
        }
        
        // --- FIX: Smoothly interpolate the current velocity towards the target direction ---
        Vector2 smoothedVelocity = Vector2.Lerp(
            enemy.RB.linearVelocity, // Current velocity
            targetMoveDirection * approachSpeed, // Target velocity
            Time.deltaTime * turnSpeed // Smoothing factor
        );
        
        enemy.MoveEnemy(smoothedVelocity); // Apply the smoothed velocity

        if (enemy.IsWithInAttackDistance)
        {
            enemy.stateMachine.ChangeState(enemy.AttackState);
        }
    }
}