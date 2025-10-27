// Full Path: Assets/Scripts/AI/ContextSteering/ContextSolver.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextSolver : MonoBehaviour
{
    [SerializeField]
    private bool showGizmos = true;

    // Gizmo parameters
    float[] interestGizmo = new float[8];
    float[] dangerGizmo = new float[8]; // Added danger cache
    Vector2 resultDirection = Vector2.zero;
    private float gizmoRayLength = 1.5f; // Adjusted length for clarity

    private void Start()
    {
        interestGizmo = new float[8];
        dangerGizmo = new float[8]; // Initialize danger cache
    }

    public Vector2 GetDirectionToMove(List<SteeringBehaviour> behaviours, AIData aiData)
    {
        float[] danger = new float[8];
        float[] interest = new float[8];

        // Loop through each behaviour to get votes
        foreach (SteeringBehaviour behaviour in behaviours)
        {
            // Only process enabled behaviours
            if (behaviour.enabled) {
                 (danger, interest) = behaviour.GetSteering(danger, interest, aiData);
            }
        }

        // --- Store danger votes for Gizmos ---
        System.Array.Copy(danger, dangerGizmo, 8); // Copy danger votes before modification

        // Subtract danger values from interest array
        for (int i = 0; i < 8; i++)
        {
            // Make sure interest doesn't go below zero
            interest[i] = Mathf.Clamp01(interest[i] - danger[i]);
        }

        // Store final interest votes for Gizmos
        System.Array.Copy(interest, interestGizmo, 8);

        // Calculate the average direction based on final interest
        Vector2 outputDirection = Vector2.zero;
        for (int i = 0; i < 8; i++)
        {
            outputDirection += Directions.eightDirections[i] * interest[i];
        }

        // Normalize the final direction vector
        outputDirection.Normalize(); // This makes it a direction with length 1

        resultDirection = outputDirection; // Cache for Gizmos

        // --- ADD LOG ---
        // Log the final calculated direction and the input arrays for debugging
        if (showGizmos) // Only log if gizmos are enabled to reduce spam
        {
            // Debug.Log($"Solver ({gameObject.name}): Final Direction: {resultDirection}\n" +
            //           $"Interest In: [{string.Join(", ", interest)}]\n" +
            //           $"Danger In:   [{string.Join(", ", danger)}]\n" +
            //           $"Interest Out:[{string.Join(", ", interestGizmo)}]\n" +
            //           $"Danger Out:  [{string.Join(", ", dangerGizmo)}]"); // Can be spammy
        }
         // --- END LOG ---

        // Return the selected movement direction
        return resultDirection;
    }


    // Updated Gizmo Drawing
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && showGizmos)
        {
            // --- Draw Danger Rays (Red) ---
            if (dangerGizmo != null)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < dangerGizmo.Length; i++)
                {
                    // Draw rays scaled by danger value
                    Gizmos.DrawRay(transform.position, Directions.eightDirections[i] * dangerGizmo[i] * gizmoRayLength);
                }
            }

            // --- Draw Final Interest Rays (Green) ---
            // These are the interest values *after* danger has been subtracted
            if (interestGizmo != null)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < interestGizmo.Length; i++)
                {
                     // Draw rays scaled by final interest value
                    Gizmos.DrawRay(transform.position, Directions.eightDirections[i] * interestGizmo[i] * gizmoRayLength);
                }
            }

            // --- Draw Final Result Direction (Yellow) ---
             Gizmos.color = Color.blue;
             // Draw the calculated direction vector
             Gizmos.DrawRay(transform.position, resultDirection * gizmoRayLength * 1.5f); // Make final direction slightly longer
        }
    }
}