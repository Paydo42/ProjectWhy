// Full Path: Assets/Scripts/AI/AgentMover.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Removed ContextSolver and AIData requirements as they are no longer directly used for movement direction
[RequireComponent(typeof(Rigidbody2D))]
public class AgentMover : MonoBehaviour
{
    private Rigidbody2D rb;
    // Removed contextSolver, aiData, steeringBehaviours references

    [SerializeField]
    public float moveSpeed = 3f;
    [SerializeField, Tooltip("How close the agent needs to be to a waypoint to move to the next one.")]
    private float waypointReachedDistance = 0.1f; // Threshold for reaching a waypoint

    // Public property controlled by the State Machine
    public bool canMove = true;

    // Pathfinding related variables
    private List<Vector3> currentPath; // Stores the path points received from A*
    private int currentWaypointIndex = 0; // Index of the next waypoint in the path
    public bool isFollowingPath = false; // Flag to indicate if currently following a path

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

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

     // Public method for the State Machine to assign a new path
     public void SetPath(List<Node> newPathNodes)
     {
         if (newPathNodes != null && newPathNodes.Count > 0)
         {
             // Convert Node list to Vector3 positions
             currentPath = new List<Vector3>();
             foreach (Node node in newPathNodes)
             {
                 currentPath.Add(node.transform.position);
             }

             currentWaypointIndex = 0; // Start from the beginning of the new path
             isFollowingPath = true;
             // Debug.Log($"AgentMover ({gameObject.name}): New path set with {currentPath.Count} points.");
         }
         else
         {
             // Clear path if null or empty path is received
             StopMovement();
             // Debug.LogWarning($"AgentMover ({gameObject.name}): Received null or empty path.");
         }
     }

     // Helper method to stop movement and clear the path
     public void StopMovement()
     {
          if (rb != null)
          {
              rb.linearVelocity = Vector2.zero;
          }
         currentPath = null;
         currentWaypointIndex = 0;
         isFollowingPath = false;
         // Debug.Log($"AgentMover ({gameObject.name}): Movement stopped, path cleared.");
     }


    private void FixedUpdate()
    {
        // Check Rigidbody exists before using
        if (rb == null) return;

        if (canMove && isFollowingPath && currentPath != null && currentWaypointIndex < currentPath.Count)
        {
            // Get the position of the current waypoint
            Vector3 targetWaypoint = currentPath[currentWaypointIndex];

            // Calculate direction towards the waypoint
            Vector2 moveDirection = ((Vector2)targetWaypoint - rb.position).normalized;

            // Apply movement using linearVelocity
            rb.linearVelocity = moveDirection * moveSpeed;

            // Check if we've reached the current waypoint
            if (Vector2.Distance(rb.position, targetWaypoint) < waypointReachedDistance)
            {
                currentWaypointIndex++; // Move to the next waypoint

                // Check if we've reached the end of the path
                if (currentWaypointIndex >= currentPath.Count)
                {
                    StopMovement(); // Stop moving once the final destination is reached
                    // Optionally, you might want the state machine to decide what happens next,
                    // rather than stopping automatically here. You could add an event or flag.
                    // Debug.Log($"AgentMover ({gameObject.name}): Reached end of path.");
                }
                 // else {
                 //     Debug.Log($"AgentMover ({gameObject.name}): Reached waypoint {currentWaypointIndex-1}, moving to {currentWaypointIndex}");
                 // }
            }
        }
        else if (!canMove || !isFollowingPath) // If not allowed to move OR not following a path
        {
            // Stop immediately if state machine says stop, or if no path is being followed
            rb.linearVelocity = Vector2.zero;
        }
         // Optional: Add debug velocity logging if needed
         // else if (canMove && isFollowingPath) {
         //     Debug.Log($"AgentMover ({gameObject.name}): Waiting for path update or finished path.");
         // }
    }

     // Optional Gizmos to draw the current path
     void OnDrawGizmos()
     {
         if (isFollowingPath && currentPath != null && currentPath.Count > 0)
         {
             Gizmos.color = Color.cyan;
             // Draw lines connecting waypoints
             for (int i = 0; i < currentPath.Count - 1; i++)
             {
                 Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
             }
             // Draw spheres at waypoints (highlight current target)
             for (int i = 0; i < currentPath.Count; i++)
             {
                 Gizmos.color = (i == currentWaypointIndex) ? Color.magenta : Color.cyan * 0.5f;
                 Gizmos.DrawSphere(currentPath[i], 0.15f);
             }
         }
     }
}