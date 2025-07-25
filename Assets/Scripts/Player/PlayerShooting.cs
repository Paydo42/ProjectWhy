using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 0.5f;
    private float nextFireTime = 0f;
    public float projectileSpeed = 10f;
    public float timeReward = 5f;
    public PlayerMovement movement;

    // Input callback
    public void OnShoot(InputAction.CallbackContext context)
    {
        // Only trigger on press down
        if (!context.performed) return;

        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Debug.Log("Shoot command received");
            PerformShoot();
           
        }
        else
        {
            Debug.Log($"Fire rate cooldown: {nextFireTime - Time.time} seconds remaining");
        }
    }

    private void PerformShoot()
    {
        GameEvents.TriggerPlayerShot(timeReward); // Notify game events
        if (projectilePrefab == null || firePoint == null || movement == null)
        {
            Debug.LogError("PlayerShooting: Missing references!");
            return;
        }

        // Create projectile
        GameObject projectileInstance = Instantiate(
            projectilePrefab,
            firePoint.position,
            firePoint.rotation
        );
        Debug.Log($"Projectile spawned at {firePoint.position}");

        // Get rigidbody - now checking root object first
        Rigidbody2D projectileRigidbody = projectileInstance.GetComponent<Rigidbody2D>();
        if (projectileRigidbody == null)
        {
            projectileRigidbody = projectileInstance.GetComponentInChildren<Rigidbody2D>();
        }

        if (projectileRigidbody == null)
        {
            Debug.LogError("Rigidbody2D not found on projectile!");
            Destroy(projectileInstance);
            return;
        }

        // Calculate direction
        Vector2 shootDirection = movement.lastMoveDir;
        if (shootDirection.sqrMagnitude < 0.01f)
        {
            shootDirection = Vector2.down; // Default direction if no movement
            Debug.Log("Using default shoot direction");
        }

        // Apply velocity
        projectileRigidbody.linearVelocity = shootDirection.normalized * projectileSpeed;
         
        Debug.Log($"Projectile velocity: {projectileRigidbody.linearVelocity}");
    }
}