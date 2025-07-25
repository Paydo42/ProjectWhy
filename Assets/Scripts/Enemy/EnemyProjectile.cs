using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float lifetime = 2f;
    public int damage = 1;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log($"Collision detected with: {other.name}");
        if (other.CompareTag("Player"))
        {
            //Debug.Log("Collision with Player detected!");
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log("PlayerHealth component found, applying damage...");
                playerHealth.TakeDamage(damage);
            }
            else
            {
                Debug.LogError("PlayerHealth component not found on Player!");
            }
            Destroy(gameObject);
        }
    }

}
