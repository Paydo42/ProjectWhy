
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
   [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private GameObject erailPrefab;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform erailSpawnPoint; 

    [Header("Settings")]
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float projectileSpeed = 12f;
    private float nextFireTime = 0f;
    
    [Header("Split Attack")]
    [SerializeField] private int projectileCount = 1; // Number of projectiles per shot
    [SerializeField] private float spreadAngle = 30f; // Total angle spread between projectiles
    
    [Header("Chain Attack")]
    private bool chainAttackEnabled = false;
    private int chainCount = 0;
    private float chainRange = 5f;
    private float chainDamageMultiplier = 0.8f;
    
    [Header("Piercing")]
    private int pierceCount = 0;
    
    [Header("BonusDamage")]
    private float bonusDamage = 0f;
    
    [Header("Audio  ")]

    [SerializeField] private AudioClip erailSpawnClip;
    private AudioSource audioSource;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        audioSource = GetComponent<AudioSource>();
    }

    // --- FIX: This method now just updates the isFiring state based on button press and release ---
    public void OnFire(InputAction.CallbackContext context)
    {
       if (context.performed && Time.time >= nextFireTime)
        {
            SummonErail();
        }
    }   
     private void SummonErail()
    {
       nextFireTime = Time.time + fireRate;
       Vector2 aimDirection = playerMovement.lastMoveDir;
       if (aimDirection == Vector2.zero)
        {
            aimDirection = Vector2.right; // Default direction if none
        }

        // Play sound once
        if (audioSource != null && erailSpawnClip != null)
        {
            audioSource.PlayOneShot(erailSpawnClip);
        }

        // Spawn single Erail - it handles the split attack internally
        float baseAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        Quaternion erailRotation = Quaternion.Euler(0, 0, baseAngle);
        GameObject erailObj = PoolManager.Instance.Spawn(erailPrefab, erailSpawnPoint.position, erailRotation);
        
        Erail erailScript = erailObj.GetComponent<Erail>();
        if (erailScript != null)
        {
            erailScript.Initialize(erailPrefab, projectilePrefab, aimDirection, projectileSpeed, projectileCount, spreadAngle,
                                   chainAttackEnabled, chainCount, chainRange, chainDamageMultiplier, pierceCount, bonusDamage);
        }
    }
    
    // Public methods for upgrades to modify split attack
    public void SetProjectileCount(int count)
    {
        projectileCount = count;
    }
    
    public void AddProjectileCount(int amount)
    {
        projectileCount += amount;
    }
    
    public void SetSpreadAngle(float angle)
    {
        spreadAngle = angle;
    }
    
    public int GetProjectileCount() => projectileCount;
    public float GetSpreadAngle() => spreadAngle;
    
    // Chain attack methods
    public void EnableChainAttack(int count, float range, float damageMultiplier)
    {
        chainAttackEnabled = true;
        chainCount = count;
        chainRange = range;
        chainDamageMultiplier = damageMultiplier;
    }
    
    public void DisableChainAttack()
    {
        chainAttackEnabled = false;
        chainCount = 0;
    }
    
    public bool IsChainAttackEnabled() => chainAttackEnabled;
    public int GetChainCount() => chainCount;
    
    // Fire rate methods
    public void IncreaseFireRate(float multiplier)
    {
        // Reduce fire rate delay (lower = faster shooting)
        fireRate *= (1f - multiplier);
        if (fireRate < 0.1f) fireRate = 0.1f; // Minimum cap
        Debug.Log($"Fire rate improved! Delay between shots: {fireRate}s");
    }
    
    public void SetFireRate(float rate)
    {
        fireRate = rate;
    }
    
    public float GetFireRate() => fireRate;
    
    // Piercing methods
    public void SetPierceCount(int count)
    {
        pierceCount = count;
    }
    
    public void AddPierceCount(int amount)
    {
        pierceCount += amount;
    }
    
    public int GetPierceCount() => pierceCount;
    
    // Damage methods
    public void AddBonusDamage(float amount)
    {
        bonusDamage += amount;
        Debug.Log($"Damage increased! Bonus damage now: +{bonusDamage}");
    }
    
    public void SetBonusDamage(float amount)
    {
        bonusDamage = amount;
    }
    
    public float GetBonusDamage() => bonusDamage;

    // --- FIX: Added an Update method to handle continuous firing ---
  

 /* public void SpawnProjectile()
    {
        if (projectilePrefab == null || erailSpawnPoint == null || playerMovement == null) return;

        // Spawn a projectile with a default rotation (the projectile will rotate itself)
        Vector2 shootDirection = playerMovement.lastMoveDir;    
        if (shootDirection == Vector2.zero)
        {
            shootDirection = Vector2.right; // Default direction if none
        }
        GameObject projectileInstance = PoolManager.Instance.Spawn(projectilePrefab, erailSpawnPoint.position, Quaternion.identity);
        
        Projectile projectile = projectileInstance.GetComponent<Projectile>();
      if (projectile != null)
        {
            projectile.Initialize(projectilePrefab); // Important for return to pool
            projectile.SetDirection(shootDirection, projectileSpeed);
        }
    }*/
}

