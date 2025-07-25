using UnityEngine;
using System.Collections;

public class JumpPatrolEnemy : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float jumpHeight = 3f;
    public float jumpDuration = 1f;
    public float waitTime = 1.5f;

    [Header("Visual Settings")]
    public float squashAmount = 0.7f;
    public float squashDuration = 0.2f;
    public ParticleSystem jumpParticles;

    private int currentPointIndex = 0;
    private bool isJumping = false;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private Animator animator;
    public int damageAmount = 1; // Damage dealt to player on contact

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        originalScale = transform.localScale;

        // Start at first patrol point
        if (patrolPoints.Length > 0)
        {
            transform.position = patrolPoints[0].position;
            StartCoroutine(PatrolRoutine());
        }
        else
        {
            Debug.LogError("No patrol points assigned!");
        }
    }

    IEnumerator PatrolRoutine()
    {
        while (true)
        {
            // Wait at current point
            yield return new WaitForSeconds(waitTime);

            // Move to next patrol point
            if (!isJumping)
            {
                StartCoroutine(JumpToNextPoint());
            }

            // Wait until jump completes
            while (isJumping)
            {
                yield return null;
            }
        }
    }

    IEnumerator JumpToNextPoint()
    {
        isJumping = true;

        // Calculate next point
        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        Vector3 targetPosition = patrolPoints[currentPointIndex].position;

        // Determine jump direction for sprite flipping
        if (targetPosition.x > transform.position.x)
        {
            spriteRenderer.flipX = false;
        }
        else if (targetPosition.x < transform.position.x)
        {
            spriteRenderer.flipX = true;
        }

        // Jump preparation (squash)
        if (animator != null)
        {
            animator.SetTrigger("JumpPrepare");
        }
        else
        {
            StartCoroutine(SquashEffect(squashAmount));
        }
        yield return new WaitForSeconds(squashDuration);

        // Play jump particles
        if (jumpParticles != null)
        {
            jumpParticles.Play();
        }

        // Jump animation
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }

        // Perform jump
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < jumpDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / jumpDuration;

            // Calculate vertical position (parabola)
            float vertical = jumpHeight * Mathf.Sin(t * Mathf.PI);

            // Move horizontally and vertically
            transform.position = Vector3.Lerp(startPosition, targetPosition, t) + Vector3.up * vertical;

            yield return null;
        }

        // Ensure exact position
        transform.position = targetPosition;

        // Landing effect
        if (animator != null)
        {
            animator.SetTrigger("Land");
        }
        else
        {
            StartCoroutine(SquashEffect(squashAmount));
        }
        yield return new WaitForSeconds(squashDuration);

        isJumping = false;
    }

    IEnumerator SquashEffect(float amount)
    {
        Vector3 targetScale = new Vector3(
            originalScale.x * (1f + amount),
            originalScale.y * (1f - amount),
            originalScale.z
        );

        // Squash down
        float elapsed = 0f;
        while (elapsed < squashDuration / 2f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / (squashDuration / 2f));
            yield return null;
        }

        // Return to normal
        elapsed = 0f;
        while (elapsed < squashDuration / 2f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / (squashDuration / 2f));
            yield return null;
        }

        transform.localScale = originalScale;
    }

    // Visualize patrol points in editor
    void OnDrawGizmosSelected()
    {
        if (patrolPoints == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] != null)
            {
                Gizmos.DrawSphere(patrolPoints[i].position, 0.2f);
                if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                }
            }
        }

        // Connect last to first
        if (patrolPoints.Length > 1 && patrolPoints[0] != null && patrolPoints[patrolPoints.Length - 1] != null)
        {
            Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].position, patrolPoints[0].position);
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
        if (target.CompareTag("Player"))
        {
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.ApplyDamage(damageAmount, gameObject);
            }
        }
    }
}