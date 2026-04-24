using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    public bool isLocked = false;
    public Animator doorAnimator;
    public Collider2D doorCollider;

    [Header("Teleport")]
    [Tooltip("Place an empty GameObject on the other side of the door, inside the destination chamber. The player will be teleported here when passing through.")]
    public Transform exitPoint;

    void Start()
    {
        if (doorAnimator == null) doorAnimator = GetComponent<Animator>();
        if (doorCollider == null) doorCollider = GetComponent<Collider2D>();
    }

    public void CloseAndLock()
    {
        isLocked = true;
        if (doorAnimator != null) doorAnimator.SetBool("IsOpen", false);
        if (doorCollider != null) doorCollider.isTrigger = false; // Solid block
    }

    public void Open()
    {
        isLocked = false;
        if (doorAnimator != null) doorAnimator.SetBool("IsOpen", true);
        if (doorCollider != null) doorCollider.isTrigger = true; // Player can pass
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (isLocked || exitPoint == null) return;
        if (!other.CompareTag("Player")) return;

        // Check which side the player exited from
        Vector2 doorToExit = (exitPoint.position - transform.position).normalized;
        Vector2 doorToPlayer = (other.transform.position - transform.position).normalized;

        // Player exited on the exit point side → they walked through the door
        if (Vector2.Dot(doorToExit, doorToPlayer) > 0)
        {
            other.transform.position = exitPoint.position;
        }
        // Otherwise they backed out the way they came → do nothing
    }
}