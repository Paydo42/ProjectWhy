// Full Path: Assets/Scripts/AI/ContextSteering/SeekBehaviour.cs
using UnityEngine;
using System.Collections.Generic; // Added for clarity, though Linq imports it
using System.Linq; // Ensure Linq is included for OrderBy

public class SeekBehaviour : SteeringBehaviour
{
    [SerializeField]
    private float targetReachedThreshold = 0.5f;

    [SerializeField]
    private bool showGizmo = true; // Flag to control gizmo drawing

    // State variable: Tracks if the agent needs to find a new target
    private bool reachedLastTarget = true;
    private Vector2 targetPositionCached;
    private float[] interestsTemp; // For gizmo drawing

    public override (float[] danger, float[] interest) GetSteering(float[] danger, float[] interest, AIData aiData)
    {
        // --- 1. Target Selection ---
        // If we reached the last target OR don't have a valid current one, find a new target.
        if (reachedLastTarget || aiData.currentTarget == null)
        {
            // --- ADD LOG ---
            Debug.Log($"SeekBehaviour ({gameObject.name}): Attempting to find target...");
            // --- END LOG ---

            // Check if there are any available targets in AIData
            if (aiData.targets == null || aiData.targets.Count == 0)
            {
                // --- ADD LOG ---
                Debug.LogWarning($"SeekBehaviour ({gameObject.name}): No targets available in AIData!");
                // --- END LOG ---
                aiData.currentTarget = null; // Ensure current target is null
                reachedLastTarget = true;    // Remain in 'reached' state
                // Clear interest array as there's nothing to seek
                System.Array.Clear(interest, 0, interest.Length);
                interestsTemp = interest; // Update gizmo cache
                return (danger, interest);
            }
            else
            {
                // Find the closest target among available ones
                reachedLastTarget = false; // We now have a target, so we haven't reached it yet
                aiData.currentTarget = aiData.targets
                    .Where(t => t != null) // Filter out any null targets in the list
                    .OrderBy(target => Vector2.Distance(target.position, transform.position))
                    .FirstOrDefault(); // Get the closest non-null target

                // --- ADD LOG ---
                if (aiData.currentTarget != null) {
                    Debug.Log($"SeekBehaviour ({gameObject.name}): New target selected: {aiData.currentTarget.name}");
                } else {
                     // This could happen if aiData.targets contained only null entries
                     Debug.LogError($"SeekBehaviour ({gameObject.name}): Failed to select a valid target (list might contain only nulls)!");
                     reachedLastTarget = true;
                     System.Array.Clear(interest, 0, interest.Length);
                     interestsTemp = interest;
                     return (danger, interest);
                }
                // --- END LOG ---
            }
        }

        // --- 2. Cache Target Position ---
        // Store the target's position in case it gets destroyed or removed mid-seek
        // Only update cache if the currentTarget is valid and still in the targets list
        // If currentTarget becomes invalid, we'll keep seeking the last known position.
        if (aiData.currentTarget != null && aiData.targets != null && aiData.targets.Contains(aiData.currentTarget))
            targetPositionCached = aiData.currentTarget.position;
        // Need to handle case where targetPositionCached hasn't been set yet if target becomes invalid immediately
        else if (aiData.currentTarget == null && targetPositionCached == default(Vector2)) {
             // If we lost target immediately and have no cache, we can't seek
             Debug.LogWarning($"SeekBehaviour ({gameObject.name}): Lost target immediately and no cached position.", this);
             reachedLastTarget = true; // Go back to finding a target next frame
             System.Array.Clear(interest, 0, interest.Length);
             interestsTemp = interest;
             return (danger, interest);
        }


        // --- 3. Check if Target Reached ---
        float distanceToTarget = Vector2.Distance(transform.position, targetPositionCached);
        // --- ADD LOG ---
        // This log can be very frequent, uncomment only if specifically needed:
        // Debug.Log($"SeekBehaviour ({gameObject.name}): Distance to target = {distanceToTarget} (Threshold: {targetReachedThreshold})");
        // --- END LOG ---
        if (distanceToTarget < targetReachedThreshold)
        {
            // --- ADD LOG ---
            Debug.LogWarning($"SeekBehaviour ({gameObject.name}): Target considered REACHED! Distance: {distanceToTarget} < Threshold: {targetReachedThreshold}");
            // --- END LOG ---
            reachedLastTarget = true; // Mark as reached so we find a new target next time
            aiData.currentTarget = null; // Clear current target reference in AIData
            // Clear interest array, as we've reached the goal
            System.Array.Clear(interest, 0, interest.Length);
            interestsTemp = interest; // Update gizmo cache
            return (danger, interest);
        }

        // --- 4. Calculate Interest Directions ---
        // If we haven't reached the target, calculate votes towards the cached position
        Vector2 directionToTarget = (targetPositionCached - (Vector2)transform.position);
        bool interestCalculated = false; // Flag to check if any interest was generated this frame

        // Clear previous frame's interest votes before calculating new ones
        System.Array.Clear(interest, 0, interest.Length);

        // Avoid calculations if direction is zero (should be caught by distance check, but safe)
        if (directionToTarget.sqrMagnitude > 0.001f)
        {
            Vector2 directionToTargetNormalized = directionToTarget.normalized;
            for (int i = 0; i < interest.Length; i++)
            {
                // Calculate alignment between the target direction and one of the 8 fixed directions
                float result = Vector2.Dot(directionToTargetNormalized, Directions.eightDirections[i]);

                // Vote only for directions generally pointing towards the target (dot > 0)
                if (result > 0)
                {
                    // Set the interest vote for this direction (no need to check previous value since array was cleared)
                    interest[i] = result;
                    interestCalculated = true; // Mark that we generated at least one interest vote
                }
            }
        } else {
             Debug.LogWarning($"SeekBehaviour ({gameObject.name}): directionToTarget is near zero, skipping interest calculation.");
        }


        // --- ADD LOG ---
        // Log a warning if, after all calculations, no direction received any interest vote
        if (!interestCalculated && !reachedLastTarget) { // Only warn if we *should* be seeking
            Debug.LogWarning($"SeekBehaviour ({gameObject.name}): No interest votes generated! directionToTarget: {directionToTarget}");
        } else if (interestCalculated) {
            // Optional log for success (can be spammy):
            // Debug.Log($"SeekBehaviour ({gameObject.name}): Interest votes generated.");
        }
        // --- END LOG ---

        // Cache the calculated interest array for gizmo drawing
        interestsTemp = interest;
        return (danger, interest); // Return modified interest array (danger is untouched)
    }

    // --- Gizmo Drawing ---
    private void OnDrawGizmos()
    {
        // Use the showGizmo flag to control drawing
        if (showGizmo == false || !Application.isPlaying) // Also check if game is playing
            return;

        // Draw cached target position sphere
        Gizmos.color = reachedLastTarget ? Color.gray : Color.blue; // Gray if reached, blue if seeking
        Gizmos.DrawSphere(targetPositionCached, 0.2f);

        // Draw interest rays based on the cached interestsTemp array
        if (interestsTemp != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < interestsTemp.Length; i++)
            {
                // Draw ray from agent's position in the direction, length scaled by interest value
                Gizmos.DrawRay(transform.position, Directions.eightDirections[i] * interestsTemp[i] * 2); // Multiplied length for visibility
            }
        }
    }
}