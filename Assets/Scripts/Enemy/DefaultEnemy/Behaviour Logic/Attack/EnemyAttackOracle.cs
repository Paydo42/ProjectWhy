using UnityEngine;

[CreateAssetMenu(fileName = "OracleAttack", menuName = "Enemy Logic/Attack Logic/Oracle Attack")]
public class EnemyAttackOracle : EnemyAttackSOBase
{
    [Header("Oracle Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float timeBetweenShots = 2f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float visionRange = 20f;
    [SerializeField] private LayerMask obstacleLayer;

    private float _timer;
    private Rigidbody2D _playerRb;

    public override void Initialize(GameObject gameObject, Enemy enemy)
    {
        // 1. Run base logic (assigns 'enemy', 'transform', 'gameObject', and 'playerTransform')
        base.Initialize(gameObject, enemy);
        
        // 2. Safely get the Rigidbody from the player found by the base class
        if (playerTransform != null)
        {
            _playerRb = playerTransform.GetComponent<Rigidbody2D>();
        }
        else
        {
            // Fallback: If base failed to find player, try again manually
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                _playerRb = playerObj.GetComponent<Rigidbody2D>();
            }
        }
        
        _timer = timeBetweenShots; 
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        _timer = timeBetweenShots;
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (playerTransform == null)
        {
            // Attempt to re-acquire player (e.g., if they respawned)
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                _playerRb = playerObj.GetComponent<Rigidbody2D>();
            }
            return;
        }

        _timer += Time.deltaTime;

        if (_timer >= timeBetweenShots)
        {
            if (CheckLineOfSight())
            {
                ShootPredictive();
                _timer = 0f;
            }
        }
    }

    public override void DoPhysicsUpdateLogic() { base.DoPhysicsUpdateLogic(); }
    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType) { base.DoAnimationTriggerEventLogic(triggerType); }
    public override void DoExitLogic() { base.DoExitLogic(); }
    public override void ResetValues() { base.ResetValues(); _timer = 0f; }

    private bool CheckLineOfSight()
    {
        if (playerTransform == null) return false;

        Vector2 direction = playerTransform.position - transform.position;
        float distance = direction.magnitude;

        if (distance > visionRange) return false;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, obstacleLayer);
        Debug.DrawRay(transform.position, direction.normalized * distance, Color.red, 0.5f);

        if (hit.collider != null)
        {
            return false; // Wall hit
        }
        return true;
    }

    private void ShootPredictive()
    {
        if (playerTransform == null || PoolManager.Instance == null) return;

        Vector2 targetPos = playerTransform.position;

        // 1. Prediction Logic
        if (_playerRb != null)
        {
            float distance = Vector2.Distance(transform.position, targetPos);
            float travelTime = distance / projectileSpeed;
            targetPos += _playerRb.linearVelocity * travelTime;
        }

        Vector2 shootDirection = (targetPos - (Vector2)transform.position).normalized;

        // 2. Calculate Rotation (Visuals)
        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 3. Spawn from Pool using the rotation
        GameObject bulletObj = PoolManager.Instance.Spawn(projectilePrefab, transform.position, rotation);

        // 4. Initialize Bullet Script (Logic)
        EnemyProjectile bulletScript = bulletObj.GetComponent<EnemyProjectile>();
        if (bulletScript != null)
        {
            // Required for pooling to know where to return
            bulletScript.Initialize(projectilePrefab);
            
            // Sets the velocity on the Rigidbody
            bulletScript.SetDirection(shootDirection, projectileSpeed);
        }
        else
        {
            Debug.LogError("Projectile prefab is missing the EnemyProjectile script!");
        }
    }
}