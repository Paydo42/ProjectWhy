using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    public float lifetime = 2f;
    public int damage = 1;
    public LayerMask collisionLayers; // Set in Inspector: Include enemies and obstacles
    
    void Start()
    {
        Destroy(gameObject, lifetime);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the collided object is in our designated layers
        // Delete this code after testing
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

            // Destroy projectile on any valid collision
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"Ignoring collision with: {other.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        }
    }
}