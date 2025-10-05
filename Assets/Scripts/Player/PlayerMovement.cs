using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector3 firePointBaseLocalPosition;
    private Rigidbody2D rb;
    private Vector2 movementInput;
    private Animator animator;
    public Vector2 lastMoveDir = Vector2.right;
    public Transform firePoint;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Ensure firePoint is set
         firePointBaseLocalPosition = firePoint.localPosition;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();

        bool isMoving = movementInput.sqrMagnitude > 0.01f;
        animator.SetBool("IsWalking", isMoving);

        if (isMoving)
        {
            animator.SetFloat("InputX", movementInput.x);
            animator.SetFloat("InputY", movementInput.y);
            lastMoveDir = movementInput.normalized;
            animator.SetFloat("LastInputX", lastMoveDir.x);
            animator.SetFloat("LastInputY", lastMoveDir.y);
        }
    }
    public void OnInteract(InputAction.CallbackContext context)
    {
        Debug.Log("Interact action triggered");
        if (context.performed)
        {
            Debug.Log("Interact action performed");
            // Check if an upgrade is in range
            if (Upgrade.upgradeInRange != null)
            {
                Debug.Log("Upgrade selected: " + Upgrade.upgradeInRange.name);
                Upgrade.upgradeInRange.SelectThisUpgrade();
            }
            else
            {
                Debug.Log("No upgrade in range!");
            }
        }
    }
    void FixedUpdate()
    {
        rb.linearVelocity = movementInput * moveSpeed;
    }

    void Update()
    {
        // Always update firepoint rotation
        UpdateFirePointRotation();
        UpdateFirePointPosition();
    }
      private void UpdateFirePointRotation() {
        
    
     if (firePoint != null)
        {
            float angle = Mathf.Atan2(lastMoveDir.y, lastMoveDir.x) * Mathf.Rad2Deg;
            firePoint.rotation = Quaternion.Euler(0f, 0f, angle);
           
            Debug.DrawRay(firePoint.position, lastMoveDir, Color.red); // Visual direction indicator
        }
    }
    private void UpdateFirePointPosition()
    {
        if (firePoint != null)
        {
            float distance = 0.5f; // Distance from player center
            float xPosition;

            // Handle horizontal direction
            if (lastMoveDir.x < 0) // Facing left
            {
                xPosition = lastMoveDir.x * -distance; // Keep firePoint on the left side
            }
            else // Facing right or neutral
            {
                xPosition = lastMoveDir.x * distance; // Keep firePoint on the right side
            }

            // For vertical movement
            float yPosition = lastMoveDir.y * distance;

            Vector3 newPosition = new Vector3(xPosition, yPosition, 0);
            firePoint.localPosition = newPosition;
        }
    }
}