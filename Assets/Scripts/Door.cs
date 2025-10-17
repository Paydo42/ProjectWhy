using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    public bool isLocked = false;
    public Animator doorAnimator;
    public Collider2D doorCollider;

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
}