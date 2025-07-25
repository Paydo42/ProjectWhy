using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public Enemy_Scriptable_Object enemyStats; // Reference to the enemy stats Scriptable Object
    //public float maxHealth = 3f;
    private float currentHealth;
    public float timeReward = 5f; // Time reward for defeating the enemy
    
    //[Header("Visual Feedback")]
    //public Color damageColor = Color.red;
    //public float flashDuration = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        currentHealth = enemyStats.maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.color = enemyStats.enemyColor; // Set initial color from stats
        }
        
        // Ensure collider exists
        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning("EnemyHealth: No Collider2D found on the enemy. Please add one for collision detection.");
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        StartCoroutine(DamageFlash());
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = enemyStats.damageFlashColor;
            yield return new WaitForSeconds(enemyStats.damageFlashDuration);
            spriteRenderer.color = originalColor;
        }
    }

    private void Die()
    {
        // Trigger time reward event
        GameEvents.TriggerEnemyKilled(timeReward);
        // Add death effects, animations, etc.
        Destroy(gameObject);
       // GameManager.Instance.AddTime(timeReward); // Reward player with time
        Debug.Log($"Enemy defeated! Rewarded {timeReward} seconds.");
    }
}