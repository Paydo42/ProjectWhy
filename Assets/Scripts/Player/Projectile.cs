using UnityEngine;
using System.Collections;

public class PlayerProjectile : MonoBehaviour
{
    public float lifetime = 2f;
    public int damage = 1;
    public LayerMask collisionLayers;
    
    private PoolManager poolManager;
    
    void Start()
    {
        // Find pool manager
        poolManager = FindAnyObjectByType<PoolManager>();
        // Start lifetime coroutine
        StartCoroutine(ReturnAfterLifetime());
    }
    
    void OnEnable()
    {
        // Reset any state when the object is reused from pool
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        // Restart lifetime coroutine when object is reused
        StopAllCoroutines();
        StartCoroutine(ReturnAfterLifetime());
    }
    
    IEnumerator ReturnAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);
        ReturnToPool();
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            Debug.Log($"Projectile hit layer: {LayerMask.LayerToName(other.gameObject.layer)}");

            // Enemy handling
            if (other.CompareTag("Enemy"))
            {
                if (other.TryGetComponent<EnemyHealth>(out var enemyHealth))
                {
                    enemyHealth.TakeDamage(damage);
                    Debug.Log($"Dealt {damage} damage to enemy");
                }
                if (other.TryGetComponent<Enemy>(out var enemy))
                {
                    enemy.TakeDamage(damage);
                    Debug.Log($"Dealt {damage} damage to enemy via Enemy component");
                }
            }

            // Return to pool instead of Destroy
            ReturnToPool();
        }
        else
        {
            Debug.Log($"Ignoring collision with: {other.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        }
    }
    
    void ReturnToPool()
    {
        if (poolManager != null)
        {
            poolManager.ReturnObjectToPool(gameObject);
        }
        else
        {
            // Fallback to Destroy if pool manager not found
            Destroy(gameObject);
        }
    }
}