using UnityEngine;

/// <summary>
/// Trinity Enemy - A unique enemy with three heads (left, right, and top)
/// 
/// BEHAVIOR:
/// - Can see LEFT, RIGHT, and UP (not down/forward)
/// - Uses raycasts from three eyes to detect the player
/// - Has only 2 states: IDLE (Patrol) and ATTACK (no chase state)
/// - Patrols between 4 corner points (edges of roam area)
/// - When player detected via raycast → Attack
/// - LEFT/RIGHT eyes shoot regular projectiles
/// - TOP eye shoots laser-like projectile
/// - When player lost → Return to Patrol
/// 
/// SETUP IN UNITY:
/// 1. Assign EnemyIdleTrinityPatrol ScriptableObject to EnemyIdleBaseInstance
/// 2. Assign EnemyAttackTrinity ScriptableObject to EnemyAttackBaseInstance
/// 3. Create child GameObjects for LeftEye, RightEye, and ThirdEye transforms
/// 4. Configure layers in the ScriptableObjects (PlayerLayer and ObstacleLayer)
/// </summary>
public class Trinity : Enemy
{
    [Header("=== TRINITY EYE TRANSFORMS ===")]
    [Tooltip("Transform for the left eye raycast origin.")]
    public Transform LeftEye;
    
    [Tooltip("Transform for the right eye raycast origin.")]
    public Transform RightEye;
    
    [Tooltip("Transform for the third (top) eye raycast origin - shoots laser.")]
    public Transform ThirdEye;

    [Header("=== DEBUG GIZMOS ===")]
    [Tooltip("Show patrol points and path in Scene view")]
    public bool showPatrolGizmos = true;
    public Color patrolPointColor = Color.yellow;
    public Color currentTargetColor = Color.green;
    public Color patrolLineColor = Color.cyan;

    // === RUNTIME VARIABLES (used by ScriptableObjects) ===
    
    /// <summary>Last known position where the player was detected</summary>
    [HideInInspector] public Vector3 LastKnownTargetPosition;
    
    /// <summary>Direction where player was last detected (Vector2.left, Vector2.right, or Vector2.up)</summary>
    [HideInInspector] public Vector2 LastDetectedDirection;
    
    /// <summary>Which eye detected the player (0=left, 1=right, 2=third/top)</summary>
    [HideInInspector] public int LastDetectedEyeIndex;

    public override void Awake()
    {
        base.Awake();
        
        // Validate eye transforms
        if (LeftEye == null)
            Debug.LogWarning($"{name}: LeftEye transform not assigned!");
        if (RightEye == null)
            Debug.LogWarning($"{name}: RightEye transform not assigned!");
        if (ThirdEye == null)
            Debug.LogWarning($"{name}: ThirdEye transform not assigned!");
        
        // Validate ScriptableObject assignments
        if (EnemyIdleBaseInstance == null)
            Debug.LogError($"{name}: Missing EnemyIdleBaseInstance! Assign EnemyIdleTrinityPatrol ScriptableObject.");
        if (EnemyAttackBaseInstance == null)
            Debug.LogError($"{name}: Missing EnemyAttackBaseInstance! Assign EnemyAttackTrinity ScriptableObject.");
    }

    private void OnDrawGizmos()
    {
        if (!showPatrolGizmos) return;
        
        // Try to get patrol data from the ScriptableObject
        EnemyIdleTrinityPatrol patrolSO = EnemyIdleBaseInstance as EnemyIdleTrinityPatrol;
        
        if (patrolSO != null && patrolSO.IsPatrolInitialized)
        {
            Vector3[] points = patrolSO.PatrolPoints;
            int currentIndex = patrolSO.CurrentPatrolIndex;
            
            if (points != null && points.Length == 4)
            {
                // Draw all 4 patrol points
                for (int i = 0; i < 4; i++)
                {
                    bool isCurrentTarget = (i == currentIndex);
                    Gizmos.color = isCurrentTarget ? currentTargetColor : patrolPointColor;
                    Gizmos.DrawWireSphere(points[i], isCurrentTarget ? 0.4f : 0.25f);
                    
                    // Draw index number
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(points[i] + Vector3.up * 0.5f, $"P{i}");
                    #endif
                }
                
                // Draw patrol path
                Gizmos.color = patrolLineColor;
                for (int i = 0; i < 4; i++)
                {
                    int nextIndex = (i + 1) % 4;
                    Gizmos.DrawLine(points[i], points[nextIndex]);
                }
            }
        }
        
        // Draw eye directions
        if (LeftEye != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(LeftEye.position, Vector2.left * 2f);
        }
        if (RightEye != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(RightEye.position, Vector2.right * 2f);
        }
        if (ThirdEye != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(ThirdEye.position, Vector2.up * 2f);
        }
    }
}
