// Full Path: Assets/Scripts/AI/AgentMover.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(ContextSolver), typeof(AIData))]
public class AgentMover : MonoBehaviour
{
    private Rigidbody2D rb;
    private ContextSolver contextSolver;
    private AIData aiData;
    private List<SteeringBehaviour> steeringBehaviours;

    [SerializeField]
    private float moveSpeed = 3f;

    // Public property controlled by the State Machine
    public bool canMove = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        contextSolver = GetComponent<ContextSolver>();
        aiData = GetComponent<AIData>();

        // Get all steering behaviours attached to this object once
        steeringBehaviours = new List<SteeringBehaviour>(GetComponents<SteeringBehaviour>());

        // Basic Rigidbody setup check
        if(rb == null) {
            Debug.LogError($"AgentMover on {gameObject.name} requires a Rigidbody2D!", this);
        } else {
             // Ensure Rigidbody settings are appropriate for top-down (e.g., Gravity Scale = 0)
             if (rb.gravityScale != 0) {
                 Debug.LogWarning($"Rigidbody2D on {gameObject.name} has Gravity Scale != 0. Recommended to set to 0 for top-down.", this);
                 // rb.gravityScale = 0; // Optionally force it
             }
             rb.freezeRotation = true; // Usually want this for top-down sprite characters
        }
    }

    private void FixedUpdate()
    {
        // Check Rigidbody exists before using
        if (rb == null) return;

        if (canMove)
        {
            // Get the best direction from the solver
            Vector2 moveDirection = contextSolver.GetDirectionToMove(steeringBehaviours, aiData);

            // --- USE linearVelocity ---
            // Apply the movement using linearVelocity
            rb.linearVelocity = moveDirection * moveSpeed;
            // --- END CHANGE ---

            // Optional: Log velocity if needed for debugging
            // Debug.Log($"AgentMover ({gameObject.name}): Target Vel: {moveDirection * moveSpeed}, Actual Vel: {rb.linearVelocity}");
        }
        else
        {
            // If state machine says stop, stop immediately
            rb.linearVelocity = Vector2.zero; // Use linearVelocity here too
        }
    }
}