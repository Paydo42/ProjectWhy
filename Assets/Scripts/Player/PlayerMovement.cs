using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
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

    void FixedUpdate()
    {
        rb.linearVelocity = movementInput * moveSpeed;
    }

    void Update()
    {
        // Always update firepoint rotation
        if (firePoint != null)
        {
            float angle = Mathf.Atan2(lastMoveDir.y, lastMoveDir.x) * Mathf.Rad2Deg;
            firePoint.rotation = Quaternion.Euler(0f, 0f, angle);
            Debug.DrawRay(firePoint.position, lastMoveDir, Color.red); // Visual direction indicator
        }
    }
}