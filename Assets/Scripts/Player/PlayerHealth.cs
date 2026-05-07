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

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Hit Sound")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField, Range(0f, 1f)] private float hitVolume = 1f;

    [Header("Invulnerability Layer")]
    [Tooltip("Layer the player is moved to during i-frames. Configure the Physics 2D matrix so this layer does NOT collide with the Enemy layer.")]
    [SerializeField] private string invulnerableLayerName = "PlayerInvulnerable";

    private float lastDamageTime = -10f; // Initialize to allow immediate damage
    private bool isInvulnerable = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private int defaultLayer;
    private int invulnerableLayer = -1;
    private Rigidbody2D rb;
    private AudioSource audioSource;

    public bool IsKnockedBack { get; private set; }

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        defaultLayer = gameObject.layer;
        invulnerableLayer = LayerMask.NameToLayer(invulnerableLayerName);
        if (invulnerableLayer == -1)
        {
            Debug.LogWarning($"PlayerHealth: layer '{invulnerableLayerName}' does not exist. " +
                             "Create it and disable its collision with the Enemy layer in Project Settings → Physics 2D.", this);
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (healthUiManager != null)
        {
            healthUiManager.Initialize(this);
        }
    }

    // Public method to take damage with cooldown check.
    // Pass sourcePosition to trigger knockback away from that point.
    public void TakeDamage(float amount, Vector3? sourcePosition = null)
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

        // Hit feedback
        PlayHitSound();
        if (sourcePosition.HasValue)
            StartCoroutine(KnockbackRoutine(sourcePosition.Value));

        // Start invulnerability period
        StartCoroutine(InvulnerabilityPeriod());

        // Handle death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
    if (collision.collider.CompareTag("Enemy"))
        {
        TakeDamage(1f, collision.transform.position);
        Debug.Log("Player took damage from Enemy collision!");
        }
    }
    // Handles all damage sources with cooldown
    public void ApplyDamage(float amount, GameObject damageSource = null)
    {
        if (CanTakeDamage())
        {
            TakeDamage(amount, damageSource != null ? damageSource.transform.position : (Vector3?)null);
            Debug.Log($"Took {amount} damage from {damageSource?.name ?? "unknown source"}");
        }
    }

    private void PlayHitSound()
    {
        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound, hitVolume);
    }

    private System.Collections.IEnumerator KnockbackRoutine(Vector3 sourcePosition)
    {
        if (rb == null) yield break;

        Vector2 direction = ((Vector2)transform.position - (Vector2)sourcePosition).normalized;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;

        IsKnockedBack = true;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        rb.linearVelocity = Vector2.zero;
        IsKnockedBack = false;
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

        // Swap to a layer whose physics-matrix entry with the Enemy layer is unchecked,
        // so the player can walk through enemies while invulnerable but still collides with walls.
        if (invulnerableLayer != -1)
            gameObject.layer = invulnerableLayer;

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

        if (invulnerableLayer != -1)
            gameObject.layer = defaultLayer;

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