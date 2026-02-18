// Full Path: Assets/Scripts/Enemy/DefaultEnemy/Behaviour Logic/Attack/EnemyAttackTwins.cs
using UnityEngine;

/// <summary>
/// Twins-specific Attack behavior.
/// 
/// - Stays stationary while attacking
/// - Continuously checks left/right vision
/// - Returns to Idle (Patrol) when player is no longer visible
/// - Shoots in the direction where player was detected
/// </summary>
[CreateAssetMenu(fileName = "Attack_Twins", menuName = "Enemy Logic/Attack Logic/Twins Attack")]
public class EnemyAttackTwins : EnemyAttackSOBase
{
    [Header("=== TWINS VISION SETTINGS ===")]
    [Tooltip("How far the twins can see on each side")]
    public float sightDistance = 7f;
    
    [Tooltip("Layer mask for detecting the player")]
    public LayerMask playerLayer;
    
    [Tooltip("Layer mask for obstacles that block vision")]
    public LayerMask obstacleLayer;

    [Header("=== ATTACK SETTINGS ===")]
    [Tooltip("Time between attacks")]
    public float attackCooldown = 1f;
    
    [Tooltip("How long before returning to patrol after losing sight")]
    public float loseSightThreshold = 1.5f;

    [Header("=== PROJECTILE SETTINGS ===")]
    [Tooltip("Projectile prefab to shoot")]
    public GameObject projectilePrefab;
    
    [Tooltip("Speed of the projectile")]
    public float projectileSpeed = 8f;
    
    [Tooltip("Offset from center to spawn projectile")]
    public float projectileSpawnOffset = 0.5f;
    
    [Tooltip("Maximum random spread angle in degrees (0 = perfectly accurate)")]
    [Range(0f, 45f)]
    public float spreadAngle = 10f;

    [Header("=== DEBUG ===")]
    public bool showDebugRays = true;

    // Runtime variables
    private float attackTimer = 0f;
    private float loseSightTimer = 0f;
    private Vector2 lastDetectedDirection = Vector2.right;
    
    // References
    private Transform leftEye;
    private Transform rightEye;
    private Twins twinsEnemy;

    public override void Initialize(GameObject gameObject, Enemy enemy)
    {
        base.Initialize(gameObject, enemy);
        
        twinsEnemy = enemy as Twins;
        if (twinsEnemy != null)
        {
            leftEye = twinsEnemy.LeftEye;
            rightEye = twinsEnemy.RightEye;
        }
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        
        // Stop movement while attacking
        if (enemy.agentMover != null)
            enemy.agentMover.canMove = false;
        enemy.StopPathfinding();
        
        attackTimer = 0f;
        loseSightTimer = 0f;
        
        // Get initial direction from Twins class if available
        if (twinsEnemy != null)
        {
            lastDetectedDirection = twinsEnemy.LastDetectedDirection;
        }
        
        Debug.Log($"[{enemy.name}] Twins Attack started, facing {(lastDetectedDirection.x > 0 ? "RIGHT" : "LEFT")}");
    }

    public override void DoFrameUpdateLogic()
    {
        // Don't process new attacks while already attacking (animation playing)
        if (enemy.IsAttacking) return;
        
        // Check if we can still see the player
        bool canSeePlayer = CanSeePlayerLeftOrRight(out Vector2 detectedDirection);
        
        if (canSeePlayer)
        {
            loseSightTimer = 0f;
            lastDetectedDirection = detectedDirection;
            
            // Update Twins class
            if (twinsEnemy != null)
            {
                twinsEnemy.LastKnownTargetPosition = playerTransform.position;
                twinsEnemy.LastDetectedDirection = detectedDirection;
            }
            
            // Attack logic - use cooldown timer
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackCooldown)
            {
                attackTimer = 0f;
                // Start attack animation (will call PerformAttack via animation event or immediately)
                enemy.StartAttackAnimation();
            }
        }
        else
        {
            // Lost sight - count timer
            loseSightTimer += Time.deltaTime;
            
            if (loseSightTimer >= loseSightThreshold)
            {
                Debug.Log($"[{enemy.name}] Lost sight of player, returning to Patrol");
                // Go back to Idle (which uses TwinsPatrol SO)
                enemy.stateMachine.ChangeState(enemy.IdleState);
                return;
            }
        }
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
    }

    // ==========================================
    // VISION SYSTEM
    // ==========================================

    private bool CanSeePlayerLeftOrRight(out Vector2 detectedDirection)
    {
        detectedDirection = Vector2.zero;
        
        if (RaycastForPlayer(Vector2.left, leftEye))
        {
            detectedDirection = Vector2.left;
            return true;
        }
        
        if (RaycastForPlayer(Vector2.right, rightEye))
        {
            detectedDirection = Vector2.right;
            return true;
        }
        
        return false;
    }

    private bool RaycastForPlayer(Vector2 direction, Transform eyeTransform)
    {
        Vector2 rayOrigin = eyeTransform != null ? 
            (Vector2)eyeTransform.position : 
            (Vector2)transform.position;
        
        RaycastHit2D obstacleHit = Physics2D.Raycast(rayOrigin, direction, sightDistance, obstacleLayer);
        float effectiveDistance = obstacleHit.collider != null ? obstacleHit.distance : sightDistance;
        
        RaycastHit2D playerHit = Physics2D.Raycast(rayOrigin, direction, effectiveDistance, playerLayer);
        
        if (showDebugRays)
        {
            Color rayColor = direction.x < 0 ? Color.blue : Color.red;
            if (playerHit.collider != null)
                Debug.DrawRay(rayOrigin, direction * playerHit.distance, Color.green);
            else
                Debug.DrawRay(rayOrigin, direction * effectiveDistance, rayColor);
        }
        
        return playerHit.collider != null;
    }

    // ==========================================
    // ATTACK SYSTEM
    // ==========================================

    /// <summary>
    /// Called by Enemy.PerformAttackFromSO() - either from animation event or immediately.
    /// Uses the lastDetectedDirection to shoot the projectile.
    /// </summary>
    public override void PerformAttack()
    {
        ShootProjectile(lastDetectedDirection);
    }

    /// <summary>
    /// Shoots a projectile in the specified direction from the appropriate eye.
    /// </summary>
    private void ShootProjectile(Vector2 direction)
    {
        Debug.Log($"[{enemy.name}] Shooting towards {direction}!");
        
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[{enemy.name}] No projectile prefab assigned!");
            return;
        }
        
        if (PoolManager.Instance == null)
        {
            Debug.LogError($"[{enemy.name}] PoolManager.Instance is null!");
            return;
        }
        
        // Determine which eye to shoot from based on direction
        // Left direction = shoot from left eye, Right direction = shoot from right eye
        Transform firePoint = direction.x < 0 ? leftEye : rightEye;
        
        // Calculate spawn position from the appropriate eye
        Vector3 spawnPos;
        if (firePoint != null)
        {
            // Spawn from the eye position with a small offset in the shooting direction
            spawnPos = firePoint.position + (Vector3)(direction * projectileSpawnOffset);
        }
        else
        {
            // Fallback to enemy center if eye is not assigned
            spawnPos = transform.position + (Vector3)(direction * projectileSpawnOffset);
        }
        
        // Apply random spread to the direction
        Vector2 spreadDirection = ApplySpread(direction);
        
        // Calculate rotation (point projectile in the direction)
        float angle = Mathf.Atan2(spreadDirection.y, spreadDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle - 90f); // -90 because Unity's "up" is forward for 2D
        
        // Spawn projectile from pool
        GameObject projectile = PoolManager.Instance.Spawn(projectilePrefab, spawnPos, rotation);
        
        // Initialize the projectile for pooling
        EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();
        if (projectileScript != null)
        {
            // Required for pooling - tells projectile which prefab it came from
            projectileScript.Initialize(projectilePrefab);
            
            // Set the velocity with the spread direction
            projectileScript.SetDirection(spreadDirection, projectileSpeed);
        }
        else
        {
            // Fallback if no EnemyProjectile script - just set velocity directly
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = spreadDirection * projectileSpeed;
            }
            Debug.LogWarning($"[{enemy.name}] Projectile prefab is missing EnemyProjectile script!");
        }
    }

    /// <summary>
    /// Apply random spread to the shooting direction
    /// </summary>
    private Vector2 ApplySpread(Vector2 direction)
    {
        if (spreadAngle <= 0f)
            return direction;
        
        // Get the base angle of the direction
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Add random spread (-spreadAngle to +spreadAngle)
        float randomSpread = Random.Range(-spreadAngle, spreadAngle);
        float newAngle = baseAngle + randomSpread;
        
        // Convert back to direction vector
        float newAngleRad = newAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(newAngleRad), Mathf.Sin(newAngleRad));
    }

    public override void ResetValues()
    {
        base.ResetValues();
        attackTimer = 0f;
        loseSightTimer = 0f;
    }
}
