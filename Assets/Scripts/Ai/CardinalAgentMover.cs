using UnityEngine;

/// <summary>
/// Simple 4-way (cardinal) movement driver. An alternative to AgentMover for enemies
/// that must move strictly up/down/left/right (e.g. Charger). No pathfinding —
/// caller sets a cardinal direction and speed, this just drives the Rigidbody2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CardinalAgentMover : MonoBehaviour
{
    [SerializeField] public float moveSpeed = 3f;
    public bool canMove = true;

    private Rigidbody2D rb;
    private Vector2 currentDirection = Vector2.zero;

    public Vector2 CurrentDirection => currentDirection;
    public bool IsMoving => canMove && currentDirection.sqrMagnitude > 0.01f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }
    }

    /// <summary>Set the movement direction. Input is snapped to the nearest cardinal.</summary>
    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            currentDirection = Vector2.zero;
            return;
        }
        currentDirection = SnapToCardinal(direction);
    }

    public void StopMovement()
    {
        currentDirection = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (canMove && currentDirection.sqrMagnitude > 0.01f)
            rb.linearVelocity = currentDirection * moveSpeed;
        else
            rb.linearVelocity = Vector2.zero;
    }

    public static Vector2 SnapToCardinal(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            return dir.x >= 0 ? Vector2.right : Vector2.left;
        else
            return dir.y >= 0 ? Vector2.up : Vector2.down;
    }
}
