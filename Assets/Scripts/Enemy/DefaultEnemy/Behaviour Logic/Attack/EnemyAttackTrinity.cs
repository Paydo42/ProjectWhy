// Full Path: Assets/Scripts/Enemy/DefaultEnemy/Behaviour Logic/Attack/EnemyAttackTrinity.cs
using UnityEngine;

/// <summary>
/// Trinity-specific Attack behavior.
/// 
/// - Stays stationary while attacking
/// - Continuously checks left/right/up vision
/// - Returns to Idle (Patrol) when player is no longer visible
/// - LEFT/RIGHT eyes shoot regular projectiles
/// - TOP (Third) eye shoots laser-like projectile
/// </summary>
[CreateAssetMenu(fileName = "Attack_Trinity", menuName = "Enemy Logic/Attack Logic/Trinity Attack")]
public class EnemyAttackTrinity : EnemyAttackSOBase
{
    [Header("=== TRINITY VISION SETTINGS ===")]
    [Tooltip("How far Trinity can see on each side and up")]
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

    [Header("=== REGULAR PROJECTILE (Left/Right Eyes) ===")]
    [Tooltip("Projectile prefab for left/right eye attacks")]
    public GameObject regularProjectilePrefab;
    
    [Tooltip("Speed of the regular projectile")]
    public float regularProjectileSpeed = 8f;
    
    [Tooltip("Maximum random spread angle in degrees")]
    [Range(0f, 45f)]
    public float regularSpreadAngle = 10f;

    [Header("=== LASER PROJECTILE (Third Eye) ===")]
    [Tooltip("Laser projectile prefab for third eye attacks")]
    public GameObject laserProjectilePrefab;
    
    [Tooltip("Speed of the laser projectile")]
    public float laserProjectileSpeed = 15f;
    
    [Tooltip("Spread angle for laser (usually 0 for precision)")]
    [Range(0f, 15f)]
    public float laserSpreadAngle = 0f;

    [Header("=== SPAWN SETTINGS ===")]
    [Tooltip("Offset from eye position to spawn projectile")]
    public float projectileSpawnOffset = 0.5f;

    [Header("=== DEBUG ===")]
    public bool showDebugRays = true;

    // Runtime variables
    private float attackTimer = 0f;
    private float loseSightTimer = 0f;
    private Vector2 lastDetectedDirection = Vector2.right;
    private int lastDetectedEyeIndex = 1; // 0=left, 1=right, 2=third
    
    // References
    private Transform leftEye;
    private Transform rightEye;
    private Transform thirdEye;
    private Trinity trinityEnemy;

    public override void Initialize(GameObject gameObject, Enemy enemy)
    {
        base.Initialize(gameObject, enemy);
        
        trinityEnemy = enemy as Trinity;
        if (trinityEnemy != null)
        {
            leftEye = trinityEnemy.LeftEye;
            rightEye = trinityEnemy.RightEye;
            thirdEye = trinityEnemy.ThirdEye;
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
        
        // Get initial direction from Trinity class if available
        if (trinityEnemy != null)
        {
            lastDetectedDirection = trinityEnemy.LastDetectedDirection;
            lastDetectedEyeIndex = trinityEnemy.LastDetectedEyeIndex;
        }
        
        string eyeName = lastDetectedEyeIndex == 0 ? "LEFT" : (lastDetectedEyeIndex == 1 ? "RIGHT" : "TOP");
        Debug.Log($"[{enemy.name}] Trinity Attack started, detected with {eyeName} eye");
    }

    public override void DoFrameUpdateLogic()
    {
        // Don't process new attacks while already attacking (animation playing)
        if (enemy.IsAttacking) return;
        
        // Check if we can still see the player
        bool canSeePlayer = CanSeePlayer(out Vector2 detectedDirection, out int eyeIndex);
        
        if (canSeePlayer)
        {
            loseSightTimer = 0f;
            lastDetectedDirection = detectedDirection;
            lastDetectedEyeIndex = eyeIndex;
            
            // Update Trinity class
            if (trinityEnemy != null)
            {
                trinityEnemy.LastKnownTargetPosition = playerTransform.position;
                trinityEnemy.LastDetectedDirection = detectedDirection;
                trinityEnemy.LastDetectedEyeIndex = eyeIndex;
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

    private bool CanSeePlayer(out Vector2 detectedDirection, out int eyeIndex)
    {
        detectedDirection = Vector2.zero;
        eyeIndex = -1;
        
        // Check LEFT (eye index 0)
        if (RaycastForPlayer(Vector2.left, leftEye))
        {
            detectedDirection = Vector2.left;
            eyeIndex = 0;
            return true;
        }
        
        // Check RIGHT (eye index 1)
        if (RaycastForPlayer(Vector2.right, rightEye))
        {
            detectedDirection = Vector2.right;
            eyeIndex = 1;
            return true;
        }
        
        // Check UP (eye index 2 - Third Eye)
        if (RaycastForPlayer(Vector2.up, thirdEye))
        {
            detectedDirection = Vector2.up;
            eyeIndex = 2;
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
            Color rayColor;
            if (direction == Vector2.left) rayColor = Color.blue;
            else if (direction == Vector2.right) rayColor = Color.red;
            else rayColor = Color.magenta;
            
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
    /// </summary>
    public override void PerformAttack()
    {
        // Determine which type of projectile to shoot based on which eye detected
        if (lastDetectedEyeIndex == 2)
        {
            // Third eye detected - shoot laser
            ShootLaser(lastDetectedDirection);
        }
        else
        {
            // Left or Right eye detected - shoot regular projectile
            ShootRegularProjectile(lastDetectedDirection);
        }
    }

    /// <summary>
    /// Shoots a regular projectile from left or right eye
    /// </summary>
    private void ShootRegularProjectile(Vector2 direction)
    {
        string eyeName = direction.x < 0 ? "LEFT" : "RIGHT";
        Debug.Log($"[{enemy.name}] Shooting regular projectile from {eyeName} eye!");
        
        if (regularProjectilePrefab == null)
        {
            Debug.LogWarning($"[{enemy.name}] No regular projectile prefab assigned!");
            return;
        }
        
        if (PoolManager.Instance == null)
        {
            Debug.LogError($"[{enemy.name}] PoolManager.Instance is null!");
            return;
        }
        
        // Determine which eye to shoot from
        Transform firePoint = direction.x < 0 ? leftEye : rightEye;
        
        Vector3 spawnPos;
        if (firePoint != null)
        {
            spawnPos = firePoint.position + (Vector3)(direction * projectileSpawnOffset);
        }
        else
        {
            spawnPos = transform.position + (Vector3)(direction * projectileSpawnOffset);
        }
        
        // Apply random spread
        Vector2 spreadDirection = ApplySpread(direction, regularSpreadAngle);
        
        // Calculate rotation
        float angle = Mathf.Atan2(spreadDirection.y, spreadDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle - 90f);
        
        // Spawn projectile
        GameObject projectile = PoolManager.Instance.Spawn(regularProjectilePrefab, spawnPos, rotation);
        
        EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(regularProjectilePrefab);
            projectileScript.SetDirection(spreadDirection, regularProjectileSpeed);
        }
        else
        {
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = spreadDirection * regularProjectileSpeed;
            }
        }
    }

    /// <summary>
    /// Shoots a laser projectile from the third eye
    /// </summary>
    private void ShootLaser(Vector2 direction)
    {
        Debug.Log($"[{enemy.name}] Shooting LASER from THIRD EYE!");
        
        if (laserProjectilePrefab == null)
        {
            Debug.LogWarning($"[{enemy.name}] No laser projectile prefab assigned!");
            return;
        }
        
        if (PoolManager.Instance == null)
        {
            Debug.LogError($"[{enemy.name}] PoolManager.Instance is null!");
            return;
        }
        
        Vector3 spawnPos;
        if (thirdEye != null)
        {
            spawnPos = thirdEye.position + (Vector3)(direction * projectileSpawnOffset);
        }
        else
        {
            spawnPos = transform.position + (Vector3)(direction * projectileSpawnOffset);
        }
        
        // Apply spread (usually 0 for laser)
        Vector2 spreadDirection = ApplySpread(direction, laserSpreadAngle);
        
        // Calculate rotation
        float angle = Mathf.Atan2(spreadDirection.y, spreadDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle - 90f);
        
        // Spawn laser projectile
        GameObject projectile = PoolManager.Instance.Spawn(laserProjectilePrefab, spawnPos, rotation);
        
        EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(laserProjectilePrefab);
            projectileScript.SetDirection(spreadDirection, laserProjectileSpeed);
        }
        else
        {
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = spreadDirection * laserProjectileSpeed;
            }
        }
    }

    /// <summary>
    /// Apply random spread to the shooting direction
    /// </summary>
    private Vector2 ApplySpread(Vector2 direction, float spread)
    {
        if (spread <= 0f)
            return direction;
        
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float randomSpread = Random.Range(-spread, spread);
        float newAngle = baseAngle + randomSpread;
        
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
