using UnityEngine;

public class Erail : MonoBehaviour
{
    [Header("references")]
    [SerializeField] private Transform arrowSpawnPoint;

    [Header("visual settings")]
    [SerializeField] private float rotationOffset = 0f;
    [Header("Audio ")]
    [SerializeField] private AudioClip shootClip;
    private AudioSource audioSource;
    private float DefaultSpreadAngle = 3f;

    private GameObject projectilePrefab;
    private Vector2 shootDirection;
    private float projectileSpeed;
    private GameObject originalPrefab;
    
    // Split attack parameters
    private int projectileCount = 1;
    private float spreadAngle = 20f;
    
    // Chain attack parameters
    private bool chainEnabled = false;
    private int chainCount = 0;
    private float chainRange = 5f;
    private float chainDamageMultiplier = 0.8f;
    
    // Piercing parameters
    private int pierceCount = 0;
    
    // Damage parameters
    private float bonusDamage = 0f;
    
    // Homing parameters
    private bool homingEnabled = false;
    private float homingStrength = 5f;
    private float homingRange = 8f;

    // Bouncing parameters
    private bool bouncingEnabled = false;
    private int bounceCount = 0;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    
    public void Initialize(GameObject bowPrefabRef, GameObject projectilePrefab, Vector2 shootDirection, float projectileSpeed, 
                           int projectileCount = 1, float spreadAngle = 20f,
                           bool chainEnabled = false, int chainCount = 0, float chainRange = 5f, float chainDamageMultiplier = 0.8f,
                           int pierceCount = 0, float bonusDamage = 0f,
                           bool homingEnabled = false, float homingStrength = 5f, float homingRange = 8f,
                           bool bouncingEnabled = false, int bounceCount = 0)
    {
        this.originalPrefab = bowPrefabRef;
        this.projectilePrefab = projectilePrefab;
        this.shootDirection = shootDirection;
        this.projectileSpeed = projectileSpeed;
        this.projectileCount = projectileCount;
        this.spreadAngle = spreadAngle;
        
        // Chain attack settings
        this.chainEnabled = chainEnabled;
        this.chainCount = chainCount;
        this.chainRange = chainRange;
        this.chainDamageMultiplier = chainDamageMultiplier;
        
        // Piercing settings
        this.pierceCount = pierceCount;
        
        // Damage settings
        this.bonusDamage = bonusDamage;
        
        // Homing settings
        this.homingEnabled = homingEnabled;
        this.homingStrength = homingStrength;
        this.homingRange = homingRange;

        // Bouncing settings
        this.bouncingEnabled = bouncingEnabled;
        this.bounceCount = bounceCount;

        // Restart the shoot animation (needed for pooled objects)
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("Bow_Shoot", 0, 0f);
        }
    }
    
    public void TriggerShootEvent()
    {
        if (projectilePrefab == null || arrowSpawnPoint == null) return;

        if (audioSource != null && shootClip != null)
        {
            audioSource.PlayOneShot(shootClip);
        }

        float baseAngle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
        
        if (projectileCount == 1)
        {
            // Single projectile with small default spread
            float offset = Random.Range(-DefaultSpreadAngle, DefaultSpreadAngle);
            float finalAngle = baseAngle + offset;
            Vector2 dir = new(Mathf.Cos(finalAngle * Mathf.Deg2Rad), Mathf.Sin(finalAngle * Mathf.Deg2Rad));
            SpawnProjectile(dir, finalAngle);
        }
        else
        {
            // Multiple projectiles with spread
            float angleStep = spreadAngle / (projectileCount - 1);
            float startAngle = baseAngle - (spreadAngle / 2f);
            
            for (int i = 0; i < projectileCount; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                Vector2 direction = new Vector2(
                    Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                    Mathf.Sin(currentAngle * Mathf.Deg2Rad)
                );
                SpawnProjectile(direction, currentAngle);
            }
        }
    }
    
    private void SpawnProjectile(Vector2 direction, float angle)
    {
        Quaternion arrowRotation = Quaternion.Euler(0, 0, angle + rotationOffset);
        GameObject arrowObj = PoolManager.Instance.Spawn(projectilePrefab, arrowSpawnPoint.position, arrowRotation);

        Projectile projectile = arrowObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(projectilePrefab);
            projectile.SetDirection(direction, projectileSpeed);
            
            // Apply bonus damage
            if (bonusDamage > 0)
            {
                projectile.AddBonusDamage(bonusDamage);
            }
            
            // Apply chain attack if enabled
            if (chainEnabled && chainCount > 0)
            {
                projectile.SetChainAttack(chainCount, chainRange, chainDamageMultiplier);
            }
            
            // Apply piercing if enabled
            if (pierceCount > 0)
            {
                projectile.SetPiercing(pierceCount);
            }
            
            // Apply homing if enabled
            if (homingEnabled)
            {
                projectile.SetHoming(homingStrength, homingRange);
            }
            
            // Apply bouncing if enabled
            if (bouncingEnabled && bounceCount > 0)
            {
                projectile.SetBouncing(bounceCount);
            }
        }
    }
    public void DisableErail()
    {
       if (PoolManager.Instance != null && originalPrefab != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject, originalPrefab);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

}
