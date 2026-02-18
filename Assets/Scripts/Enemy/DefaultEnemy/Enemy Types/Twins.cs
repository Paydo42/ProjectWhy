using UnityEngine;

/// <summary>
/// Twins Enemy - A unique enemy with two heads (left and right)
/// 
/// BEHAVIOR:
/// - Can only see LEFT and RIGHT (not forward/backward)
/// - Uses raycasts on both sides to detect the player
/// - Has only 2 states: IDLE (Patrol) and ATTACK (no chase state)
/// - Patrols between 4 corner points (edges of roam area)
/// - When player detected via raycast → Attack
/// - When player lost → Return to Patrol
/// 
/// SETUP IN UNITY:
/// 1. Assign EnemyIdleTwinsPatrol ScriptableObject to EnemyIdleBaseInstance
/// 2. Assign EnemyAttackTwins ScriptableObject to EnemyAttackBaseInstance
/// 3. Create child GameObjects for LeftEye and RightEye transforms
/// 4. Configure layers in the ScriptableObjects (PlayerLayer and ObstacleLayer)
/// </summary>
public class Twins : Enemy
{
    [Header("=== TWINS EYE TRANSFORMS ===")]
    [Tooltip("Transform for the left eye/head raycast origin. Create an empty child GameObject and position it on the left side of the sprite.")]
    public Transform LeftEye;
    
    [Tooltip("Transform for the right eye/head raycast origin. Create an empty child GameObject and position it on the right side of the sprite.")]
    public Transform RightEye;

    [Header("=== DEBUG GIZMOS ===")]
    [Tooltip("Show patrol points and path in Scene view")]
    public bool showPatrolGizmos = true;
    public Color patrolPointColor = Color.yellow;
    public Color currentTargetColor = Color.green;
    public Color patrolLineColor = Color.cyan;

    // === RUNTIME VARIABLES (used by ScriptableObjects) ===
    
    /// <summary>Last known position where the player was detected</summary>
    [HideInInspector] public Vector3 LastKnownTargetPosition;
    
    /// <summary>Direction (left or right) where player was last detected (Vector2.left or Vector2.right)</summary>
    [HideInInspector] public Vector2 LastDetectedDirection;

    public override void Awake()
    {
        base.Awake();
        
        // Validate eye transforms
        if (LeftEye == null)
            Debug.LogWarning($"{name}: LeftEye transform not assigned! Raycasts will use enemy center.");
        if (RightEye == null)
            Debug.LogWarning($"{name}: RightEye transform not assigned! Raycasts will use enemy center.");
        
        // Validate ScriptableObject assignments
        if (EnemyIdleBaseInstance == null)
            Debug.LogError($"{name}: Missing EnemyIdleBaseInstance! Assign EnemyIdleTwinsPatrol ScriptableObject.");
        if (EnemyAttackBaseInstance == null)
            Debug.LogError($"{name}: Missing EnemyAttackBaseInstance! Assign EnemyAttackTwins ScriptableObject.");
    }

    private void OnDrawGizmos()
    {
        if (!showPatrolGizmos) return;
        
        // Try to get patrol data from the ScriptableObject
        EnemyIdleTwinsPatrol patrolSO = EnemyIdleBaseInstance as EnemyIdleTwinsPatrol;
        
        if (patrolSO != null && patrolSO.IsPatrolInitialized)
        {
            Vector3[] points = patrolSO.PatrolPoints;
            int currentIndex = patrolSO.CurrentPatrolIndex;
            
            if (points != null && points.Length == 4)
            {
                // Draw all 4 patrol points
                for (int i = 0; i < 4; i++)
                {
                    // Current target is green, others are yellow
                    bool isCurrentTarget = (i == currentIndex);
                    Gizmos.color = isCurrentTarget ? currentTargetColor : patrolPointColor;
                    
                    // Draw sphere at patrol point
                    float sphereSize = isCurrentTarget ? 0.5f : 0.3f;
                    Gizmos.DrawWireSphere(points[i], sphereSize);
                    
                    // Draw point number
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(points[i] + Vector3.up * 0.6f, $"P{i}");
                    #endif
                }
                
                // Draw lines connecting patrol points (the path)
                Gizmos.color = patrolLineColor;
                for (int i = 0; i < 4; i++)
                {
                    Vector3 from = points[i];
                    Vector3 to = points[(i + 1) % 4];
                    Gizmos.DrawLine(from, to);
                }
                
                // Draw line from enemy to current target
                Gizmos.color = currentTargetColor;
                Gizmos.DrawLine(transform.position, points[currentIndex]);
            }
        }
        
        // Draw eye positions and sight lines
        DrawEyeGizmos();
    }

    private void DrawEyeGizmos()
    {
        EnemyIdleTwinsPatrol patrolSO = EnemyIdleBaseInstance as EnemyIdleTwinsPatrol;
        float sightDist = patrolSO != null ? patrolSO.sightDistance : 7f;
        
        // Left eye - Blue
        Vector3 leftPos = LeftEye != null ? LeftEye.position : transform.position;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(leftPos, 0.15f);
        Gizmos.DrawLine(leftPos, leftPos + Vector3.left * sightDist);
        
        // Right eye - Red
        Vector3 rightPos = RightEye != null ? RightEye.position : transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(rightPos, 0.15f);
        Gizmos.DrawLine(rightPos, rightPos + Vector3.right * sightDist);
    }
}
