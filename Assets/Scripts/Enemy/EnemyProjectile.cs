using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    public float lifetime = 3f;
    public int damage = 1;

    // This property will store a reference to the prefab it was spawned from
    public GameObject OriginalPrefab { get; private set; }
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // OnEnable is called every time an object is activated (perfect for pooling)
    void OnEnable()
    {
        // When the projectile is activated, start a timer to deactivate it
        // This prevents projectiles from flying forever if they don't hit anything
        Invoke(nameof(ReturnToPool), lifetime);
    }

    // --- NEW METHOD (Fixes the 'Initialize' error) ---
    // This is called by the enemy to tell the projectile what its original prefab is
    public void Initialize(GameObject prefab)
    {
        OriginalPrefab = prefab;
    }

    // --- NEW METHOD (Fixes the 'SetDirection' error) ---
    // This is called by the enemy to set the projectile's velocity
    public void SetDirection(Vector2 direction, float speed)
    {
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            
            // Instead of Destroy(gameObject), we return it to the pool
            ReturnToPool();
        }
        // Optional: Add logic for hitting walls
        else if (other.CompareTag("Wall")) // Make sure your wall objects have a "Wall" tag
        {
            ReturnToPool();
        }
    }

    // --- NEW METHOD (For returning to the object pool) ---
    private void ReturnToPool()
    {
        // This stops the Invoke timer in case the projectile hit something before its lifetime was up
        CancelInvoke();

        if (PoolManager.Instance != null && OriginalPrefab != null)
        {
            // Tell the PoolManager to take this object back
            PoolManager.Instance.ReturnToPool(gameObject, OriginalPrefab);
        }
        else
        {
            // A fallback just in case something goes wrong or the pool doesn't exist
            gameObject.SetActive(false);
        }
    }
}
