using UnityEngine;

public class EnemyAi : MonoBehaviour
{
    public Enemy_Scriptable_Object enemyStats; // Reference to the enemy stats Scriptable Object
    public GameObject projectilePrefab; // Prefab of the projectile to be fired
    public Transform firePoint;         // Point from where the projectile will be fired
    //public float fireRate = 0.5f;       // Time in seconds between each shot
    private float nextFireTime = 0f;   // Time when the next shot can be fired
    //public float projectileSpeed = 10f; // Speed of the projectile
    //public int damageAmount = 1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time >= nextFireTime && enemyStats != null)
        {
            nextFireTime = Time.time + enemyStats.fireRate;
            Fire();
        }
    }

    
    void OnCollisionStay2D(Collision2D collision)
    {
        HandleDamage(collision.gameObject);
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        HandleDamage(other.gameObject);
    }
    
    private void HandleDamage(GameObject target)
    {
        if (target.CompareTag("Player") && enemyStats != null)
        {
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.ApplyDamage(enemyStats.damageAmount, gameObject);
            }
        }
    }
    private void Fire()
    {
        if (projectilePrefab == null || firePoint == null || enemyStats == null)
        {
            Debug.LogError("EnemyAi: something is missing");
            return;
        }

        GameObject projectileInstance = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb = projectileInstance.GetComponentInChildren<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D is not found");
            Destroy(projectileInstance);
            return;
        }

        Vector2 direction = firePoint.up;
        rb.linearVelocity = direction * enemyStats.projectileSpeed;
    }
}
