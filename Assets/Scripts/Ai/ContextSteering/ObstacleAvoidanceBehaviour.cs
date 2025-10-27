// Full Path: Assets/Scripts/AI/ContextSteering/ObstacleAvoidanceBehaviour.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleAvoidanceBehaviour : SteeringBehaviour
{
    [SerializeField]
    private float radius = 2f; // How far ahead to look for obstacles relevant to danger calculation
    [SerializeField]
    private float agentColliderSize = 0.6f; // Agent's own size, used for max weight calculation

    [SerializeField]
    private bool showGizmo = true; // Flag to control gizmo drawing

    //gizmo parameters
    float[] dangersResultTemp = null; // Cache for drawing danger rays

    public override (float[] danger, float[] interest) GetSteering(float[] danger, float[] interest, AIData aiData)
    {
        // If no obstacles were detected, return unchanged arrays
        if(aiData.obstacles == null || aiData.obstacles.Length == 0)
        {
            // Ensure temp array is cleared if no danger, so gizmos don't show old data
            if(dangersResultTemp == null) dangersResultTemp = new float[danger.Length];
            System.Array.Clear(dangersResultTemp, 0, dangersResultTemp.Length);
            return (danger, interest);
        }

        // Loop through each detected obstacle
        foreach (Collider2D obstacleCollider in aiData.obstacles)
        {
            // Find the closest point on the obstacle to the agent's current position
            Vector2 directionToObstacle = obstacleCollider.ClosestPoint(transform.position) - (Vector2)transform.position;
            float distanceToObstacle = directionToObstacle.magnitude;

            // Calculate weight: 1 if touching/overlapping, decreasing to 0 at 'radius' distance
            float weight = 0;
            if (distanceToObstacle <= agentColliderSize) {
                weight = 1; // Max danger if overlapping
            } else if (distanceToObstacle < radius) {
                // Danger decreases linearly from 1 (at agentColliderSize) to 0 (at radius)
                weight = (radius - distanceToObstacle) / (radius - agentColliderSize); // Adjusted calculation
                
            }
            // If distanceToObstacle >= radius, weight remains 0

            // Ensure weight doesn't go negative due to float inaccuracies
             weight = Mathf.Clamp01(weight);

            // Normalize the direction vector towards the obstacle
            Vector2 directionToObstacleNormalized = directionToObstacle.normalized;

            // Vote against directions pointing towards this obstacle
            for (int i = 0; i < Directions.eightDirections.Count; i++)
            {
                // Calculate alignment between this direction and the direction TO the obstacle
                float result = Vector2.Dot(directionToObstacleNormalized, Directions.eightDirections[i]);

                // Only consider directions pointing towards the obstacle (dot > 0)
                if (result > 0) {
                    // Apply the distance weight to the alignment score
                    float valueToPutIn = result * weight;

                    // If this vote is stronger (more dangerous) than the existing vote for this direction, update it
                    // This ensures we react to the most immediate danger in any given direction
                    if (valueToPutIn > danger[i])
                    {
                        danger[i] = valueToPutIn;
                    }
                }
            }
        }
        // Cache the results for gizmo drawing
        dangersResultTemp = danger;
        return (danger, interest); // Return the modified danger array
    }

    // --- Gizmo Drawing ---
    private void OnDrawGizmos()
    {
        // Use the showGizmo flag to control drawing
        if (showGizmo == false || !Application.isPlaying) // Also check if game is playing
            return;

        // Draw the danger rays
        if (dangersResultTemp != null)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < dangersResultTemp.Length; i++)
            {
                Gizmos.DrawRay(
                    transform.position,
                    Directions.eightDirections[i] * dangersResultTemp[i] * 2 // Multiplied length for visibility
                    );
            }
        }

        // Draw the detection radius used for weighting danger
        Gizmos.color = new Color(1, 0, 0, 0.1f); // Transparent red
        Gizmos.DrawWireSphere(transform.position, radius);
        // Draw the agent collider size used for max weight
         Gizmos.color = new Color(1, 0.5f, 0, 0.1f); // Transparent orange
        Gizmos.DrawWireSphere(transform.position, agentColliderSize);

    }
}