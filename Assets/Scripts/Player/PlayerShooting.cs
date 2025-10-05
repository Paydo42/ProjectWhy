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

    private bool isShooting = false;

    // Input callback
    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isShooting = true;
        }
        else if (context.canceled)
        {
            isShooting = false;
        }
    }

    private void Update()
    {
        if (isShooting && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Debug.Log("Shoot command received");
            PerformShoot();
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
        float spread= 0.1f;
        // Create projectile
        GameObject projectileInstance = Instantiate(
            projectilePrefab,
            firePoint.position + new Vector3(Random.Range(-spread, spread), Random.Range(-spread, spread), 0),
            firePoint.rotation
        );
       Debug.Log($"Projectile spawned at position: {firePoint.position}, rotation: {firePoint.rotation.eulerAngles}");

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
            shootDirection = Vector2.right; // Default direction if no movement
            Debug.Log("Using default shoot direction");
        }
    
        // Apply velocity
        projectileRigidbody.linearVelocity = shootDirection.normalized * projectileSpeed;
        
    // Calculate the angle for the shoot direction
        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;

// Combine the prefab's original rotation with the calculated rotation
        Quaternion baseRotation = projectilePrefab.transform.rotation;
        Quaternion shootRotation = Quaternion.Euler(0f, 0f, angle);
        projectileInstance.transform.rotation = baseRotation * shootRotation;

    Debug.Log($"Projectile velocity: {projectileRigidbody.linearVelocity}, Rotation: {projectileInstance.transform.rotation.eulerAngles}°");
    Debug.Log($"Projectile velocity: {projectileRigidbody.linearVelocity}, Rotation: {angle}°");}
}