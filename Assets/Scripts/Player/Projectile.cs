using UnityEngine;

public class Projectile : MonoBehaviour 
{
    public float speed = 10f;
    public float lifetime = 2f;
    public float damage = 1f;
    private float baseDamage; // Store base damage to reset on pool reuse
    [Header("Audio")]
    public AudioClip hitSound;
    
    [Header("Chain Attack")]
    [SerializeField] private float chainRange = 5f;
    [SerializeField] private LayerMask enemyLayer;
    private bool chainEnabled = false;
    private int chainsRemaining = 0;
    private float chainDamageMultiplier = 1f;
    
    [Header("Piercing")]
    private int pierceCount = 0; // How many enemies this projectile can pierce through
    private int pierceRemaining = 0;
    
    [Header("Homing")]
    private bool homingEnabled = false;
    private float homingStrength = 5f;   // How fast the projectile turns toward the target
    private float homingRange = 8f;      // Detection radius for finding a target
    private Transform homingTarget;

    [Header("Bouncing")]
    private bool bouncingEnabled = false;
    private int bouncesRemaining = 0;

    public GameObject OriginalPrefab { get; private set; }
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseDamage = damage; // Store the base damage from prefab
    }

    void OnEnable()
    {
        // When the projectile is activated from the pool, start a timer to deactivate it
        Invoke(nameof(ReturnToPool), lifetime);
    }

    public void Initialize(GameObject prefab)
    {
        OriginalPrefab = prefab;
        // Reset damage to base value (prevents stacking on pooled projectiles)
        damage = baseDamage;
        // Reset chain state for pooled projectiles
        chainEnabled = false;
        chainsRemaining = 0;
        chainDamageMultiplier = 1f;
        // Reset piercing state
        pierceCount = 0;
        pierceRemaining = 0;
        // Reset homing state
        homingEnabled = false;
        homingTarget = null;
        // Reset bouncing state
        bouncingEnabled = false;
        bouncesRemaining = 0;
    }
    
    /// <summary>
    /// Enable chain attack for this projectile
    /// </summary>
    public void SetChainAttack(int chainCount, float range, float damageMultiplier = 0.8f)
    {
        chainEnabled = true;
        chainsRemaining = chainCount;
        chainRange = range;
        chainDamageMultiplier = damageMultiplier;
    }
    
    /// <summary>
    /// Enable piercing for this projectile
    /// </summary>
    public void SetPiercing(int count)
    {
        pierceCount = count;
        pierceRemaining = count;
    }
    
    /// <summary>
    /// Add bonus damage to this projectile
    /// </summary>
    public void AddBonusDamage(float bonus)
    {
        damage += bonus;
    }
   
    public void SetHoming(float strength, float range)
    {
        homingEnabled = true;
        homingStrength = strength;
        homingRange = range;
        homingTarget = null; // Will be found in Update
        Debug.Log($"Homing enabled on projectile! Strength: {homingStrength}, Range: {homingRange}");
    }

    public void SetBouncing(int bounceCount)
    {
        bouncingEnabled = true;
        bouncesRemaining = bounceCount;
    }

    void Update()
    {
        if (!homingEnabled || rb == null) return;
        
        // Find or re-acquire a target
        if (homingTarget == null || !homingTarget.gameObject.activeInHierarchy)
        {
            homingTarget = FindNearestEnemy();
        }
        
        if (homingTarget != null)
        {
            Vector2 direction = ((Vector2)homingTarget.position - (Vector2)transform.position).normalized;
            Vector2 currentVelocity = rb.linearVelocity;
            float currentSpeed = currentVelocity.magnitude;
            
            // Smoothly steer toward the target
            Vector2 desiredVelocity = direction * currentSpeed;
            Vector2 steering = Vector2.Lerp(currentVelocity, desiredVelocity, homingStrength * Time.deltaTime);
            rb.linearVelocity = steering.normalized * currentSpeed;
            
            // Rotate sprite to face movement direction
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
    
    private Transform FindNearestEnemy()
    {
        Transform closest = null;
        float closestDist = float.MaxValue;

        // Standard tagged enemies.
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in enemies)
        {
            if (!enemyObj.activeInHierarchy) continue;
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy == null || enemy.CurrentHealth <= 0) continue;

            float dist = Vector2.Distance(transform.position, enemyObj.transform.position);
            if (dist > homingRange) continue;

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemyObj.transform;
            }
        }

        // Bosses (BossBase) — not tagged "Enemy", but are valid homing targets.
        BossBase[] bosses = Object.FindObjectsByType<BossBase>(FindObjectsSortMode.None);
        foreach (BossBase boss in bosses)
        {
            if (boss == null || !boss.gameObject.activeInHierarchy) continue;
            if (boss.CurrentHealth <= 0) continue;

            float dist = Vector2.Distance(transform.position, boss.transform.position);
            if (dist > homingRange) continue;

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = boss.transform;
            }
        }

        return closest;
    }

    // --- THE MAIN FIX ---
    // This new function handles both movement and rotation.
    public void SetDirection(Vector2 direction, float speed)
    {
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
            // Optional: Rotate sprite to face direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Bounce off walls
        if (other.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            if (bouncingEnabled && bouncesRemaining > 0)
            {
                BounceOffWall();
                return;
            }
            ReturnToPool();
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null && (other.CompareTag("Enemy") || other.CompareTag("Boss")))
        {
            damageable.TakeDamage(damage);
            Enemy enemyScript = other.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.ApplyKnockback(transform.position, 5f);
            }
            
            // Chain attack logic
            if (chainEnabled && chainsRemaining > 0)
            {
                TryChainToNextEnemy(other.transform);
            }
            
            // Piercing logic - don't return to pool if we can still pierce
            if (pierceRemaining > 0)
            {
                pierceRemaining--;
                if (hitSound != null)
                {
                    AudioSource.PlayClipAtPoint(hitSound, transform.position);
                }
                return; // Don't destroy, continue through
            }
        }
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
        // Return to the pool immediately upon hitting anything
        ReturnToPool();
    }

    private void BounceOffWall()
    {
        bouncesRemaining--;
        Vector2 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;

        // Raycast backward along velocity to find the wall normal
        int wallLayer = LayerMask.GetMask("Walls");
        RaycastHit2D hit = Physics2D.Raycast(
            (Vector2)transform.position - velocity.normalized * 0.5f,
            velocity.normalized, 1.5f, wallLayer);

        Vector2 reflected;
        if (hit.collider != null)
        {
            reflected = Vector2.Reflect(velocity, hit.normal).normalized * speed;
        }
        else
        {
            // Fallback: reverse direction
            reflected = -velocity;
        }

        rb.linearVelocity = reflected;
        float angle = Mathf.Atan2(reflected.y, reflected.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
    
    private void TryChainToNextEnemy(Transform hitEnemy)
    {
        // Find all enemies in range
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(hitEnemy.position, chainRange, enemyLayer);
        
        Transform closestEnemy = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider2D col in nearbyEnemies)
        {
            // Skip the enemy we just hit
            if (col.transform == hitEnemy) continue;
            
            // Skip dead enemies
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy == null || enemy.CurrentHealth <= 0) continue;
            
            float distance = Vector2.Distance(hitEnemy.position, col.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = col.transform;
            }
        }
        
        if (closestEnemy != null)
        {
            // Spawn chain projectile towards next enemy
            SpawnChainProjectile(hitEnemy.position, closestEnemy.position);
        }
    }
    
    private void SpawnChainProjectile(Vector3 fromPosition, Vector3 toPosition)
    {
        if (OriginalPrefab == null || PoolManager.Instance == null) return;
        
        Vector2 direction = (toPosition - fromPosition).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        GameObject chainProjectile = PoolManager.Instance.Spawn(OriginalPrefab, fromPosition, Quaternion.Euler(0, 0, angle));
        
        Projectile proj = chainProjectile.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(OriginalPrefab);
            proj.damage = damage * chainDamageMultiplier; // Reduced damage for chain hits
            proj.SetDirection(direction, speed * 1.5f); // Faster chain projectile
            proj.SetChainAttack(chainsRemaining - 1, chainRange, chainDamageMultiplier);
        }
    }

    private void ReturnToPool()
    {
        // Stop the lifetime timer in case we hit something early
        CancelInvoke();

        if (PoolManager.Instance != null && OriginalPrefab != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject, OriginalPrefab);
        }
        else
        {
            // Fallback in case the pool manager is not available
            gameObject.SetActive(false);
        }
    }
}

