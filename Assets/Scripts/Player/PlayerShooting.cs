using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 0.5f;
    private float nextFireTime = 0f;
    private PlayerMovement playerMovement;

    // --- FIX: Add a boolean to track if the fire button is being held down ---
    private bool isFiring = false;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    // --- FIX: This method now just updates the isFiring state based on button press and release ---
    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.performed) // Called once when the button is pressed
        {
            isFiring = true;
        }
        else if (context.canceled) // Called once when the button is released
        {
            isFiring = false;
        }
    }

    // --- FIX: Added an Update method to handle continuous firing ---
    void Update()
    {
        // If the fire button is being held down AND the fire rate cooldown has passed...
        if (isFiring && Time.time >= nextFireTime)
        {
            // ...update the cooldown timer...
            nextFireTime = Time.time + fireRate;
            // ...and fire a shot.
            Shoot();
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null || firePoint == null || playerMovement == null) return;

        // Spawn a projectile with a default rotation (the projectile will rotate itself)
        GameObject projectileInstance = PoolManager.Instance.Spawn(projectilePrefab, firePoint.position, Quaternion.identity);
        
        Projectile projectile = projectileInstance.GetComponent<Projectile>();
        if (projectile != null)
        {
            // Tell the projectile its original prefab so it can return to the pool
            projectile.Initialize(projectilePrefab);
            
            // Tell the projectile to fire in the player's last known direction.
            // The projectile's Fire() method handles both its rotation and velocity correctly.
            projectile.Fire(playerMovement.lastMoveDir);
        }
    }
}

