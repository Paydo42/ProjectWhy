// Full Path: Assets/Scripts/Ai/AgentMover.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AgentMover : MonoBehaviour
{
    // ... (all variables and Awake method are correct) ...
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    [Header("Pathfinding")]
    [SerializeField]
    public float moveSpeed = 3f;
    [SerializeField, Tooltip("How close the agent needs to be to a waypoint to move to the next one.")]
    private float waypointReachedDistance = 0.1f;
    [SerializeField, Tooltip("How quickly the agent turns. Higher values are snappier.")]
    private float turnSpeed = 10f;

    [Header("Local Avoidance")]
    [SerializeField, Tooltip("The LayerMask that contains other enemies.")]
    private LayerMask avoidanceLayer; 
    [SerializeField, Tooltip("How far to look for other enemies.")]
    private float avoidanceRadius = 1.0f;
    [SerializeField, Tooltip("How strongly to push away from other enemies.")]
    private float avoidanceWeight = 2.0f;

    public bool canMove = true;

    private List<Vector3> currentPath;
    private int currentWaypointIndex = 0;
    public bool isFollowingPath = false;
    
    private Collider2D[] avoidanceBuffer = new Collider2D[10];
    
    private ContactFilter2D avoidanceFilter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (rb == null) {
            Debug.LogError($"AgentMover on {gameObject.name} requires a Rigidbody2D!", this);
        } else {
            if (rb.gravityScale != 0) {
                Debug.LogWarning($"Rigidbody2D on {gameObject.name} has Gravity Scale != 0. Recommended to set to 0 for top-down.", this);
            }
            rb.freezeRotation = true;
        }

        avoidanceFilter = new ContactFilter2D();
        avoidanceFilter.SetLayerMask(avoidanceLayer);
        avoidanceFilter.useLayerMask = true;
    }

    // --- THIS IS THE FIX ---
    public void SetPath(List<Node> newPathNodes)
    {
        if (newPathNodes != null && newPathNodes.Count > 0)
        {
            // 1. A valid path was found. Create it.
            currentPath = new List<Vector3>();
            foreach (Node node in newPathNodes)
            {
                currentPath.Add(node.transform.position);
            }
            currentWaypointIndex = 0;
            isFollowingPath = true;

            // 2. NOW, check if we need to skip the first node.
            // This logic must be *inside* the successful path check.
            if (currentPath.Count > 1 && Vector2.Distance(rb.position, currentPath[0]) < waypointReachedDistance * 2f)
            {
                currentWaypointIndex = 1;
            }
        }
        else // This 'else' is now correctly paired with the first 'if'
        {
            // 3. The path was null or empty. Stop moving.
            StopMovement();
            Debug.LogWarning($"Agent {gameObject.name} received an invalid or empty path.");
        }
    }
    // --- END FIX ---

    public void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        currentPath = null;
        currentWaypointIndex = 0;
        isFollowingPath = false;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (canMove && isFollowingPath && currentPath != null && currentWaypointIndex < currentPath.Count)
        {
            Vector3 targetWaypoint = currentPath[currentWaypointIndex];
            Vector2 pathDirection = ((Vector2)targetWaypoint - rb.position).normalized;

            Vector2 avoidanceDirection = CalculateAvoidance();
            
            Vector2 moveDirection = (pathDirection + (avoidanceDirection * avoidanceWeight)).normalized;
            Vector2 targetVelocity = moveDirection * moveSpeed;

          
          
            rb.linearVelocity = Vector2.Lerp(
                rb.linearVelocity,
                targetVelocity,
                Time.fixedDeltaTime * turnSpeed
            );

            if (spriteRenderer != null && Mathf.Abs(pathDirection.x) > 0.1f)
            {
                  // flipX is true when facing LEFT, false when facing RIGHT
                  spriteRenderer.flipX = pathDirection.x < 0;
            }
            if (Vector2.Distance(rb.position, targetWaypoint) < waypointReachedDistance)
            {
                currentWaypointIndex++;

                if (currentWaypointIndex >= currentPath.Count)
                {
                    StopMovement();
                    Debug.Log($"Agent {gameObject.name} reached the end of its path.");
                }
            }
        }
        else if (!canMove || !isFollowingPath)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * turnSpeed);
        }
    }

    private Vector2 CalculateAvoidance()
    {
        Vector2 avoidanceVector = Vector2.zero;
        int hitCount = Physics2D.OverlapCircle(rb.position, avoidanceRadius, avoidanceFilter, avoidanceBuffer);
        
        if (hitCount > 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D other = avoidanceBuffer[i];
                
                if (other.attachedRigidbody == rb) continue; 

                Vector2 awayFromOther = rb.position - (Vector2)other.transform.position;
                
                float weight = 1.0f / (awayFromOther.magnitude + 0.01f);

                avoidanceVector += awayFromOther.normalized * weight;
                Debug.DrawRay(rb.position, awayFromOther.normalized * weight, Color.yellow);
            }
            
            if (avoidanceVector != Vector2.zero)
            {
                avoidanceVector.Normalize();
            }
        }

        return avoidanceVector;
    }

    void OnDrawGizmos()
    {
        if (isFollowingPath && currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
            for (int i = 0; i < currentPath.Count; i++)
            {
                Gizmos.color = (i == currentWaypointIndex) ? Color.magenta : Color.cyan * 0.5f;
                Gizmos.DrawSphere(currentPath[i], 0.15f);
            }
        }

        if (rb != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f); // Orange
            Gizmos.DrawWireSphere(rb.position, avoidanceRadius);
        }
    }
}