using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 8f;
    public float currentHealth;
    
    [Header("Heart Sprites")]
    public Sprite fullHeart;
    public Sprite halfHeart;
    public Sprite emptyHeart;

    [Header("UI Manager")]
    public HealthUiManager healthUiManager;
    
    [Header("Damage Settings")]
    public float damageCooldown = 0.5f; // Time between damage instances
    public float invulnerabilityFlashRate = 0.1f; // Flash speed during invulnerability
    public Color damageFlashColor = Color.red;
    
    private float lastDamageTime = -10f; // Initialize to allow immediate damage
    private bool isInvulnerable = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        if (healthUiManager != null)
        {
            healthUiManager.Initialize(this);
        }
    }

    // Public method to take damage with cooldown check
    public void TakeDamage(float amount)
    {
        // Skip if currently invulnerable
        if (isInvulnerable) return;
        
        // Apply damage
        if (Time.time < lastDamageTime + damageCooldown)
        {
            Debug.Log("Damage cooldown active, cannot take damage now.");
            return;
        }
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        
        // Update UI
        if (healthUiManager != null)
        {
            healthUiManager.DrawHearts();
        }
        
        // Start invulnerability period
        StartCoroutine(InvulnerabilityPeriod());
        
        // Handle death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Handles all damage sources with cooldown
    public void ApplyDamage(float amount, GameObject damageSource = null)
    {
        if (CanTakeDamage())
        {
            TakeDamage(amount);
            Debug.Log($"Took {amount} damage from {damageSource?.name ?? "unknown source"}");
        }
    }

    // Check if player can take damage
    public bool CanTakeDamage()
    {
        return !isInvulnerable;
    }

    // Invulnerability period with visual feedback
    private System.Collections.IEnumerator InvulnerabilityPeriod()
    {
        isInvulnerable = true;
        lastDamageTime = Time.time;
        bool flashState = false;
        float endTime = Time.time + damageCooldown;

        // Flash effect during invulnerability
        while (Time.time < endTime)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = flashState ? damageFlashColor : originalColor;
                flashState = !flashState;
            }
            yield return new WaitForSeconds(invulnerabilityFlashRate);
        }

        // Reset visual
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        isInvulnerable = false;
    }

    private void Die()
    {
        GameManager.Instance.GameOver();
        Debug.Log("Player died!");
        // Add death effects, game over screen, etc.
        Destroy(gameObject);
    }
}