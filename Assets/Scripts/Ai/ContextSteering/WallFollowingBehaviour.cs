// Full Path: Assets/Scripts/AI/ContextSteering/WallFollowingBehaviour.cs
using UnityEngine;
using System.Linq;

public class WallFollowingBehaviour : SteeringBehaviour
{
    [Header("Wall Detection")]
    [SerializeField, Tooltip("How far ahead the main feeler checks for walls blocking the path.")]
    private float wallCheckDistance = 1.0f;
    [SerializeField, Tooltip("How far ahead the side feeler checks to detect walls beside the agent.")]
    private float sideFeelerDistance = 0.8f;
    [SerializeField, Tooltip("The angle (degrees) of the side feeler relative to the agent's desired direction.")]
    private float sideFeelerAngle = 45f;
    [SerializeField, Tooltip("The layer(s) considered as walls for following.")]
    private LayerMask wallLayerMask;

    [Header("Following Parameters")]
    [SerializeField, Tooltip("How strongly this behaviour votes when actively following a wall.")]
    private float followStrength = 0.9f;
    [SerializeField, Tooltip("Desired distance to maintain from the wall while following.")]
    private float wallDistance = 0.5f;
    [SerializeField, Tooltip("How strongly to push away from the wall surface (normal).")]
    private float wallAvoidanceStrength = 0.7f;
    [SerializeField, Tooltip("How quickly the agent turns towards the wall tangent. Lower values are slower.")]
    private float tangentTurnSpeed = 4.0f; // Smoothing speed

    [Header("Gizmos")]
    [SerializeField] private bool showGizmo = true;

    // References
    private AIData aiData;
    private Rigidbody2D rb;

    // State & Smoothing
    private Vector2 currentTangent = Vector2.zero; // Smoothed tangent direction
    private bool wasHittingWallAheadLastFrame = false; // Tracks previous frame state

    // Gizmo Cache
    private Vector2 gizmoDesiredDirection = Vector2.up;
    private Vector2 gizmoSideFeelerDirection = Vector2.up;
    private RaycastHit2D gizmoForwardHit;
    private RaycastHit2D gizmoSideHit;
    private Vector2 gizmoRawTangent = Vector2.zero;
    private Vector2 gizmoAvoidanceVector = Vector2.zero;
    private bool gizmoDrawDetails = false;


    private void Awake()
    {
        aiData = GetComponentInParent<AIData>();
        rb = GetComponentInParent<Rigidbody2D>();
        if (aiData == null) Debug.LogError("WallFollowingBehaviour: Missing AIData!", this);
        if (rb == null) Debug.LogError("WallFollowingBehaviour: Missing Rigidbody2D!", this);
        if (wallLayerMask == 0) {
             wallLayerMask = LayerMask.GetMask("Default", "Obstacles"); // Example default
             Debug.LogWarning($"WallFollowingBehaviour on {name}: Wall Layer Mask not set. Assign Obstacle/Wall layer.", this);
         }
    }

    private void OnEnable() { wasHittingWallAheadLastFrame = false; } // Reset state on enable/disable
    private void OnDisable() { wasHittingWallAheadLastFrame = false; }


    public override (float[] danger, float[] interest) GetSteering(float[] danger, float[] interest, AIData _)
    {
        // Reset gizmo vectors
        gizmoRawTangent = Vector2.zero;
        gizmoAvoidanceVector = Vector2.zero;
        gizmoDrawDetails = false;

        // Determine Desired Direction (Towards Target)
        gizmoDesiredDirection = DetermineDesiredDirection();

        // Feeler Raycasts
        Vector2 origin = transform.position;
        gizmoForwardHit = Physics2D.Raycast(origin, gizmoDesiredDirection, wallCheckDistance, wallLayerMask);
        Quaternion rotation = Quaternion.Euler(0, 0, -sideFeelerAngle); // Check relative right
        gizmoSideFeelerDirection = rotation * gizmoDesiredDirection;
        gizmoSideHit = Physics2D.Raycast(origin, gizmoSideFeelerDirection, sideFeelerDistance, wallLayerMask);

        // Logic
        bool wallDetectedAheadThisFrame = gizmoForwardHit.collider != null;
        bool wallDetectedBeside = gizmoSideHit.collider != null;

        Vector2 finalFollowDirection;

        if (wallDetectedAheadThisFrame)
        {
            gizmoDrawDetails = true;
            Vector2 wallNormal = gizmoForwardHit.normal;
            float distanceToWall = gizmoForwardHit.distance;
            Vector2 calculatedTangent = new Vector2(-wallNormal.y, wallNormal.x).normalized;
            gizmoRawTangent = calculatedTangent;

            // --- Corrected Tangent Smoothing ---
            // Use the state from the *previous* frame (`wasHittingWallAheadLastFrame`)
            // to decide whether to reset or smooth the tangent.
            if (!wasHittingWallAheadLastFrame)
            {
                // Just hit the wall, reset tangent immediately
                currentTangent = calculatedTangent;
                Debug.Log($"WallFollow ({gameObject.name}): FWD HIT - STARTED Following. Tangent={currentTangent.ToString("F2")} (Normal={wallNormal.ToString("F2")})");
            }
            else // If we WERE hitting the wall last frame...
            {
                // ...smoothly interpolate towards the calculated tangent for this frame.
                currentTangent = Vector2.Lerp(currentTangent, calculatedTangent, Time.deltaTime * tangentTurnSpeed).normalized;
                // Keep minimal logs now, uncomment if needed:
                // Debug.Log($"WallFollow ({gameObject.name}): FWD HIT - CONTINUING Follow. Smoothing towards {calculatedTangent.ToString("F2")}. Current={currentTangent.ToString("F2")}");
            }
            finalFollowDirection = currentTangent;
            // --- End Smoothing ---

            // Calculate avoidance push away from wall normal
            gizmoAvoidanceVector = Vector2.zero;
            if (distanceToWall < wallDistance) {
                gizmoAvoidanceVector = wallNormal * wallAvoidanceStrength * (1f - (distanceToWall / wallDistance));
            }
            // Combine smoothed tangent and avoidance
            Vector2 combinedDirection = (finalFollowDirection + gizmoAvoidanceVector).normalized;

            // Voting (Override Interest) - Make wall following dominant when blocked ahead
            System.Array.Clear(interest, 0, interest.Length);
            for (int i = 0; i < interest.Length; i++) {
                float result = Vector2.Dot(combinedDirection, Directions.eightDirections[i]);
                if (result > 0) interest[i] = result * followStrength;
            }
             // Add Danger INTO Wall - Discourage turning back
             Vector2 wallDir = -wallNormal;
             for (int i = 0; i < danger.Length; i++) {
                 float result = Vector2.Dot(wallDir, Directions.eightDirections[i]);
                  if (result > 0) danger[i] = Mathf.Max(danger[i], result * 0.2f);
             }
        }
        else // If path ahead is clear
        {
             // No need to actively follow wall tangent
             finalFollowDirection = gizmoDesiredDirection; // Aim for original target

             // Handle side wall detection (gentle nudge)
             if (wallDetectedBeside)
             {
                 gizmoDrawDetails = true;
                 Vector2 wallNormal = gizmoSideHit.normal;
                 float distanceToWall = gizmoSideHit.distance;
                 gizmoAvoidanceVector = Vector2.zero;
                 if(distanceToWall < wallDistance) {
                      gizmoAvoidanceVector = wallNormal * wallAvoidanceStrength * (1f - (distanceToWall / wallDistance)) * 0.5f;
                 }
                 Vector2 combinedDirection = (finalFollowDirection + gizmoAvoidanceVector).normalized;
                 // gizmoRawTangent = finalFollowDirection; // Not really a tangent here

                 // Voting (Blend for Side Hit) - Gently influence Seek
                 for (int i = 0; i < interest.Length; i++) {
                     float result = Vector2.Dot(combinedDirection, Directions.eightDirections[i]);
                     if (result > 0) interest[i] = Mathf.Max(interest[i], result * followStrength * 0.3f);
                 }
                  // Add slight danger into side wall
                  Vector2 wallDir = -wallNormal;
                 for (int i = 0; i < danger.Length; i++) {
                     float result = Vector2.Dot(wallDir, Directions.eightDirections[i]);
                      if (result > 0) danger[i] = Mathf.Max(danger[i], result * 0.1f);
                 }
             }
             // If neither feeler hits, do nothing specific for wall following.
        }

        // --- UPDATE MEMORY FOR NEXT FRAME ---
        // THIS MUST BE THE LAST STEP before returning
        // Set the flag based on whether the wall was detected *this* frame
        wasHittingWallAheadLastFrame = wallDetectedAheadThisFrame;
        // --- END UPDATE ---

        return (danger, interest);
    }

     // Helper to determine the 'forward' direction for feelers
    private Vector2 DetermineDesiredDirection() {
        Vector2 desiredDirection = transform.up; // Default
        if (aiData != null && aiData.currentTarget != null) {
            Vector2 directionToTarget = (aiData.currentTarget.position - transform.position);
            if(directionToTarget.sqrMagnitude > 0.01f) desiredDirection = directionToTarget.normalized;
        } else if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f) { // Using linearVelocity
             desiredDirection = rb.linearVelocity.normalized;
        }
        return (desiredDirection == Vector2.zero) ? transform.up : desiredDirection;
    }


    // Gizmo Drawing Method
    private void OnDrawGizmosSelected()
    {
        if (showGizmo == false || !Application.isPlaying) return;
        Vector2 origin = transform.position;

        Gizmos.color = Color.white; Gizmos.DrawLine(origin, origin + gizmoDesiredDirection * 0.5f); // Desired Dir
        Gizmos.color = gizmoForwardHit.collider != null ? Color.red : Color.gray; Gizmos.DrawLine(origin, origin + gizmoDesiredDirection * wallCheckDistance); // Fwd Feeler
        Gizmos.color = gizmoSideHit.collider != null ? Color.red : Color.gray; Gizmos.DrawLine(origin, origin + gizmoSideFeelerDirection * sideFeelerDistance); // Side Feeler

        if (gizmoDrawDetails) {
            RaycastHit2D hit = gizmoForwardHit.collider != null ? gizmoForwardHit : gizmoSideHit;
            if(hit.collider != null) { Gizmos.color = Color.blue; Gizmos.DrawRay(hit.point, hit.normal * 0.5f); } // Wall Normal

            if (gizmoRawTangent != Vector2.zero && gizmoForwardHit.collider != null) {
                Gizmos.color = Color.Lerp(Color.magenta, Color.black, 0.5f); Gizmos.DrawRay(origin, gizmoRawTangent * 1.0f); // Raw Tangent
            }
            // Draw Current Tangent only when Fwd Hit (using wasHittingWall...)
            if (wasHittingWallAheadLastFrame && gizmoForwardHit.collider != null && currentTangent != Vector2.zero) {
                 Gizmos.color = Color.magenta; Gizmos.DrawRay(origin, currentTangent * 1.5f); // Smoothed/Current Tangent
            }

            if (gizmoAvoidanceVector != Vector2.zero) { Gizmos.color = Color.yellow; Gizmos.DrawRay(origin, gizmoAvoidanceVector * 1.5f); } // Avoid Push
        }
     }
}