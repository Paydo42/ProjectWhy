using UnityEngine;

public class Projectile : MonoBehaviour 
{
    public float speed = 10f;
    public float lifetime = 2f;
    public float damage = 1f;

    public GameObject OriginalPrefab { get; private set; }
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        // When the projectile is activated from the pool, start a timer to deactivate it
        Invoke(nameof(ReturnToPool), lifetime);
    }

    public void Initialize(GameObject prefab)
    {
        OriginalPrefab = prefab;
    }

    // --- THE MAIN FIX ---
    // This new function handles both movement and rotation.
    public void Fire(Vector2 direction)
    {
        Debug.Log($"Projectile received direction: {direction}");
        if (rb != null)
        {
            // 1. Set the movement velocity based on the direction
            rb.linearVelocity = direction.normalized * speed;

            // 2. Calculate the angle from the direction vector
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Debug.Log($"Calculated Angle: {angle}");
            // 3. Set the rotation of the arrow's sprite to match the angle
             transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null && other.CompareTag("Enemy"))
        {
            damageable.TakeDamage(damage);
        }

        // Return to the pool immediately upon hitting anything
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        // Stop the lifetime timer in case we hit something early
        CancelInvoke();

        if (PoolManager.Instance != null && OriginalPrefab != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject, OriginalPrefab);
        }
        else
        {
            // Fallback in case the pool manager is not available
            gameObject.SetActive(false);
        }
    }
}

