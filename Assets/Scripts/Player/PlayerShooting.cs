
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
    

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
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
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        Quaternion erailRotation = Quaternion.Euler(0, 0, angle);
        GameObject erailObj = PoolManager.Instance.Spawn(erailPrefab, erailSpawnPoint.position, erailRotation);

        Erail erailScript = erailObj.GetComponent<Erail>();
        if (erailScript != null)
        {
            erailScript.Initialize(erailPrefab, projectilePrefab, aimDirection, projectileSpeed);
        }
    }

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

