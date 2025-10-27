// Full Path: Assets/Scripts/AI/ContextSteering/CircleTargetBehaviour.cs
using UnityEngine;

public class CircleTargetBehaviour : SteeringBehaviour
{
    [Header("Circling Parameters")]
    [SerializeField, Tooltip("The ideal distance to keep from the target while circling.")]
    private float preferredShootingRange = 5f;

    [SerializeField, Tooltip("Minimum distance. If closer than this, the enemy will move away.")]
    private float minimumDistance = 4f; // Renamed from rangeDeadZone for clarity

    [SerializeField, Tooltip("Should the agent circle clockwise? Can be changed dynamically.")]
    public bool clockwise = true;

    [Header("Gizmos")]
    [SerializeField]
    private bool showGizmo = true;

    private AIData aiData;

    private void Awake() {
        aiData = GetComponentInParent<AIData>();
        if (aiData == null) {
            Debug.LogError("CircleTargetBehaviour could not find AIData component!", this);
        }
    }

    public void SetRandomDirection()
    {
        clockwise = (Random.value > 0.5f);
    }

    public override (float[] danger, float[] interest) GetSteering(float[] danger, float[] interest, AIData _)
    {
        if (aiData == null || aiData.currentTarget == null)
        {
            // Clear interest if no target
            System.Array.Clear(interest, 0, interest.Length);
            return (danger, interest);
        }

        Vector2 position = transform.position;
        Vector2 targetPosition = aiData.currentTarget.position;
        Vector2 directionToTarget = targetPosition - position;
        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget < 0.01f) {
            System.Array.Clear(interest, 0, interest.Length);
            return (danger, interest); // Avoid division by zero
        }

        // --- 1. Modified Range Correction Vector ---
        Vector2 rangeCorrectionVector = Vector2.zero;
        // ONLY move away if too close. Do NOT move closer if too far.
        if (distanceToTarget < minimumDistance)
        {
            rangeCorrectionVector = -directionToTarget.normalized; // Move away
        }
        // --- End Modification ---

        // --- 2. Calculate Circling Vector ---
        Vector2 circleVector = new Vector2(directionToTarget.y, -directionToTarget.x).normalized;
        if (!clockwise)
        {
            circleVector *= -1;
        }

        // --- 3. Combine the Vectors ---
        // Combine pure circling with moving away if too close
        Vector2 desiredDirection = (circleVector + rangeCorrectionVector).normalized; // Equal weight? Adjust if needed.

        // --- 4. Calculate 'Interest' Votes ---
        System.Array.Clear(interest, 0, interest.Length); // Clear previous votes
        for (int i = 0; i < interest.Length; i++)
        {
            float result = Vector2.Dot(desiredDirection, Directions.eightDirections[i]);
            if (result > 0)
            {
                // Set interest directly, no need to check against previous (we cleared it)
                interest[i] = result;
            }
        }

        return (danger, interest);
    }

    private void OnDrawGizmosSelected() {
        if (!showGizmo || !Application.isPlaying || aiData == null || aiData.currentTarget == null) return;

        // Draw preferred range (informational) and minimum distance (active push-away zone)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(aiData.currentTarget.position, preferredShootingRange);
        Gizmos.color = Color.yellow; // Minimum distance color
        Gizmos.DrawWireSphere(aiData.currentTarget.position, minimumDistance);
    }
}