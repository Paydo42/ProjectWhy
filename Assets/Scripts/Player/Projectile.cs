using UnityEngine;

public class Projectile : MonoBehaviour 
{
    public float speed = 10f;
    public float lifetime = 2f;
    public float damage = 1f;
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

    public GameObject OriginalPrefab { get; private set; }
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        // When the projectile is activated from the pool, start a timer to deactivate it
        Invoke(nameof(ReturnToPool), lifetime);
    }

    public void Initialize(GameObject prefab)
    {
        OriginalPrefab = prefab;
        // Reset chain state for pooled projectiles
        chainEnabled = false;
        chainsRemaining = 0;
        chainDamageMultiplier = 1f;
        // Reset piercing state
        pierceCount = 0;
        pierceRemaining = 0;
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
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null && other.CompareTag("Enemy"))
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

