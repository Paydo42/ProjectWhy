using UnityEngine;

[CreateAssetMenu(fileName = "Attack_CircleAndShoot", menuName = "Enemy Logic/Attack Logic/Circle and Shoot")]
public class EnemyAttackCircleAndShoot : EnemyAttackSOBase
{
    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float timeBetweenAttacks = 1f;
    [SerializeField] private float bulletSpeed = 10f;
    private float _timer;

    [Header("Movement")]
    [SerializeField] private float circleSpeed = 4f;
    [SerializeField] private float preferredShootingRange = 5f;
    [SerializeField] private float rangeDeadZone = 1f;
    [SerializeField, Tooltip("How quickly the enemy turns towards the desired direction. Lower values are slower/smoother.")]
    private float turnSpeed = 5f; // Added for smoothing
    
    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private float obstacleCheckDistance = 1f;
    // --- FIX: Re-added collider size reduction ---
    [SerializeField, Tooltip("How much smaller the detection box should be than the feeler collider.")]
    private float colliderSizeReduction = 0.05f;
    
    private int circleDirection = 1;

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        circleDirection = (Random.value > 0.5f) ? 1 : -1;
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        Vector2 directionToPlayer = (playerTransform.position - enemy.transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(playerTransform.position, enemy.transform.position);

        Vector2 circleMovement = new Vector2(directionToPlayer.y, -directionToPlayer.x) * circleDirection;
        Vector2 rangeCorrectionMovement = Vector2.zero;

        if (distanceToPlayer > preferredShootingRange)
        {
            rangeCorrectionMovement = directionToPlayer;
        }
        else if (distanceToPlayer < preferredShootingRange - rangeDeadZone)
        {
            rangeCorrectionMovement = -directionToPlayer;
        }
        
        Vector2 desiredMoveDirection = (circleMovement + rangeCorrectionMovement).normalized;

        Vector2 boxSize = enemy.avoidanceCollider.size - Vector2.one * colliderSizeReduction; // Use reduction
        RaycastHit2D hit = Physics2D.BoxCast(enemy.transform.position, boxSize, 0f, desiredMoveDirection, obstacleCheckDistance, obstacleLayerMask);

        Vector2 targetMoveDirection;
        if (hit.collider != null)
        {
            Vector2 slideDirection = desiredMoveDirection - (Vector2)Vector3.Project(desiredMoveDirection, hit.normal);
            
             // --- FIX: Prevent normalizing zero vector ---
            if (slideDirection.sqrMagnitude > Mathf.Epsilon)
            {
                targetMoveDirection = slideDirection.normalized;
            }
             else
            {
                 targetMoveDirection = new Vector2(hit.normal.y, -hit.normal.x);
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
            targetMoveDirection * circleSpeed,
            Time.deltaTime * turnSpeed
        );

        enemy.MoveEnemy(smoothedVelocity); // Apply smoothed velocity

        _timer += Time.deltaTime;
        if (_timer >= timeBetweenAttacks)
        {
            _timer = 0f;
            if (bulletPrefab != null && PoolManager.Instance != null)
            {
                GameObject bulletObj = PoolManager.Instance.Spawn(bulletPrefab, enemy.transform.position, Quaternion.identity);
                EnemyProjectile bullet = bulletObj.GetComponent<EnemyProjectile>();
                if (bullet != null)
                {
                    bullet.Initialize(bulletPrefab);
                    bullet.SetDirection(directionToPlayer, bulletSpeed);
                }
            }
        }
        
        if (!enemy.IsWithInAttackDistance)
        {
             enemy.stateMachine.ChangeState(enemy.IdleState);
        }
    }

    public override void ResetValues()
    {
        base.ResetValues();
        _timer = 0f;
    }
}