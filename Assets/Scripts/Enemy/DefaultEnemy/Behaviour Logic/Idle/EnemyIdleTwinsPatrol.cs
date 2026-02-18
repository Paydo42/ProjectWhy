// Full Path: Assets/Scripts/Enemy/DefaultEnemy/Behaviour Logic/Idle/EnemyIdleTwinsPatrol.cs
using UnityEngine;

/// <summary>
/// Twins-specific Idle behavior: 4-point rectangular patrol with left/right raycast detection.
/// 
/// Unlike other enemies, Twins:
/// - Can ONLY see LEFT and RIGHT (not forward/backward)
/// - Transitions directly to ATTACK when player detected (skips Chase)
/// - Patrols 4 corner points at the edges of the current room
/// </summary>
[CreateAssetMenu(fileName = "Idle_TwinsPatrol", menuName = "Enemy Logic/Idle Logic/Twins Patrol")]
public class EnemyIdleTwinsPatrol : EnemyIdleSOBase
{
    [Header("=== TWINS VISION SETTINGS ===")]
    [Tooltip("How far the twins can see on each side")]
    public float sightDistance = 7f;
    
    [Tooltip("Layer mask for detecting the player")]
    public LayerMask playerLayer;
    
    [Tooltip("Layer mask for obstacles that block vision")]
    public LayerMask obstacleLayer;

    [Header("=== PATROL SETTINGS ===")]
    [Tooltip("How long to wait at each patrol point")]
    public float waitTimeAtPoint = 1.5f;
    
    [Tooltip("How close to a patrol point before considering it reached")]
    public float patrolPointReachThreshold = 0.3f;
    
    [Tooltip("Padding from room edges (to avoid patrolling right at the walls)")]
    public float roomEdgePadding = 1f;

    [Header("=== DEBUG ===")]
    public bool showDebugRays = true;

    // Runtime variables
    private Vector3[] patrolPoints = new Vector3[4];
    private int currentPatrolIndex = 0;
    private Vector3 patrolOrigin;
    private float waitTimer = 0f;
    private bool isWaitingAtPoint = false;
    private bool patrolInitialized = false;
    private bool hasRequestedPath = false; // Track if we've already requested a path to current point
    
    // References to eye transforms (from Twins class)
    private Transform leftEye;
    private Transform rightEye;
    private Twins twinsEnemy;

    // === PUBLIC GETTERS FOR GIZMO DRAWING ===
    public Vector3[] PatrolPoints => patrolPoints;
    public int CurrentPatrolIndex => currentPatrolIndex;
    public bool IsPatrolInitialized => patrolInitialized;
    public bool IsWaiting => isWaitingAtPoint;

    public override void Initialize(GameObject gameObject, Enemy enemy)
    {
        base.Initialize(gameObject, enemy);
        
        // Try to get Twins-specific references
        twinsEnemy = enemy as Twins;
        if (twinsEnemy != null)
        {
            leftEye = twinsEnemy.LeftEye;
            rightEye = twinsEnemy.RightEye;
        }
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        
        // Initialize patrol on first enter
        if (!patrolInitialized)
        {
            patrolOrigin = transform.position;
            InitializePatrolPoints();
            patrolInitialized = true;
        }
        
        isWaitingAtPoint = false;
        waitTimer = 0f;
        hasRequestedPath = false;
        
        // Find closest point and start moving
        currentPatrolIndex = GetClosestPatrolPointIndex();
        MoveToCurrentPatrolPoint();
        
        Debug.Log($"[{enemy.name}] Twins Patrol started at point {currentPatrolIndex}");
    }

    public override void DoFrameUpdateLogic()
    {
        // DON'T call base - we override the aggro check completely
        // base.DoFrameUpdateLogic() would check IsAggroed and go to Chase
        
        // === TWINS SPECIAL VISION: Check LEFT and RIGHT only ===
        if (CanSeePlayerLeftOrRight(out Vector2 detectedDirection))
        {
            Debug.Log($"[{enemy.name}] Detected player on {(detectedDirection.x > 0 ? "RIGHT" : "LEFT")} side!");
            
            // Store info for attack state (if Twins class)
            if (twinsEnemy != null)
            {
                twinsEnemy.LastKnownTargetPosition = playerTransform.position;
                twinsEnemy.LastDetectedDirection = detectedDirection;
            }
            
            // Go directly to ATTACK (skip Chase!)
            enemy.stateMachine.ChangeState(enemy.AttackState);
            return;
        }
        
        // === PATROL LOGIC ===
        UpdatePatrol();
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        enemy.StopPathfinding();
    }

    // ==========================================
    // VISION SYSTEM (LEFT & RIGHT RAYCASTS)
    // ==========================================

    /// <summary>
    /// Check if player is visible from left or right side
    /// </summary>
    private bool CanSeePlayerLeftOrRight(out Vector2 detectedDirection)
    {
        detectedDirection = Vector2.zero;
        
        // Check LEFT
        if (RaycastForPlayer(Vector2.left, leftEye))
        {
            detectedDirection = Vector2.left;
            return true;
        }
        
        // Check RIGHT
        if (RaycastForPlayer(Vector2.right, rightEye))
        {
            detectedDirection = Vector2.right;
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Raycast in a direction to detect the player
    /// </summary>
    private bool RaycastForPlayer(Vector2 direction, Transform eyeTransform)
    {
        Vector2 rayOrigin = eyeTransform != null ? 
            (Vector2)eyeTransform.position : 
            (Vector2)transform.position;
        
        // Check for obstacles first
        RaycastHit2D obstacleHit = Physics2D.Raycast(rayOrigin, direction, sightDistance, obstacleLayer);
        float effectiveDistance = obstacleHit.collider != null ? obstacleHit.distance : sightDistance;
        
        // Raycast for player within effective distance
        RaycastHit2D playerHit = Physics2D.Raycast(rayOrigin, direction, effectiveDistance, playerLayer);
        
        // Debug visualization
        if (showDebugRays)
        {
            Color rayColor = direction.x < 0 ? Color.blue : Color.red;
            if (playerHit.collider != null)
                Debug.DrawRay(rayOrigin, direction * playerHit.distance, Color.green);
            else
                Debug.DrawRay(rayOrigin, direction * effectiveDistance, rayColor);
        }
        
        return playerHit.collider != null;
    }

    // ==========================================
    // PATROL SYSTEM
    // ==========================================

    private void InitializePatrolPoints()
    {
        // Get room bounds from the RoomBounds component (not GridGenerator - its collider is smaller)
        RoomBounds roomBounds = enemy.ParentRoom;
        
        if (roomBounds != null)
        {
            // Get the BoxCollider2D from the RoomBounds
            BoxCollider2D roomCollider = roomBounds.GetComponent<BoxCollider2D>();
            
            if (roomCollider != null)
            {
                Bounds bounds = roomCollider.bounds;
                
                // Calculate patrol points at the 4 corners with padding
                float minX = bounds.min.x + roomEdgePadding;
                float maxX = bounds.max.x - roomEdgePadding;
                float minY = bounds.min.y + roomEdgePadding;
                float maxY = bounds.max.y - roomEdgePadding;
                
                // 4 corners: Top-Left → Top-Right → Bottom-Right → Bottom-Left
                patrolPoints[0] = new Vector3(minX, maxY, 0); // Top-Left
                patrolPoints[1] = new Vector3(maxX, maxY, 0); // Top-Right
                patrolPoints[2] = new Vector3(maxX, minY, 0); // Bottom-Right
                patrolPoints[3] = new Vector3(minX, minY, 0); // Bottom-Left
                
                // Store patrol origin as the center of the room
                patrolOrigin = bounds.center;
                
                Debug.Log($"[{enemy.name}] Patrol points initialized from RoomBounds: {bounds.size}");
                return;
            }
        }
        
        // Fallback: Use enemy spawn position as center with a default size
        Debug.LogWarning($"[{enemy.name}] Could not get room bounds from RoomBounds, using fallback patrol area around spawn position.");
        
        float fallbackWidth = 5f;
        float fallbackHeight = 3f;
        float halfWidth = fallbackWidth / 2f;
        float halfHeight = fallbackHeight / 2f;
        
        patrolPoints[0] = patrolOrigin + new Vector3(-halfWidth, halfHeight, 0);
        patrolPoints[1] = patrolOrigin + new Vector3(halfWidth, halfHeight, 0);
        patrolPoints[2] = patrolOrigin + new Vector3(halfWidth, -halfHeight, 0);
        patrolPoints[3] = patrolOrigin + new Vector3(-halfWidth, -halfHeight, 0);

        
    }

    private void UpdatePatrol()
    {
        if (!patrolInitialized) return;
        
        if (isWaitingAtPoint)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                isWaitingAtPoint = false;
                waitTimer = 0f;
                currentPatrolIndex = (currentPatrolIndex + 1) % 4;
                MoveToCurrentPatrolPoint();
            }
        }
        else
        {
            // Check if we've reached the patrol point OR if the path has completed
            // Path completion means we've reached as close as pathfinding can get us
            bool pathCompleted = hasRequestedPath && enemy.agentMover != null && !enemy.agentMover.isFollowingPath;
            
            if (HasReachedCurrentPatrolPoint() || pathCompleted)
            {
                // Arrived at patrol point (or as close as we can get)
                isWaitingAtPoint = true;
                waitTimer = 0f;
                hasRequestedPath = false;
                
                if (enemy.agentMover != null)
                    enemy.agentMover.canMove = false;
                enemy.StopPathfinding();
                
                Debug.Log($"[{enemy.name}] Reached patrol point {currentPatrolIndex}, waiting...");
            }
            else if (!hasRequestedPath)
            {
                // Haven't requested a path yet, do it now
                MoveToCurrentPatrolPoint();
            }
        }
    }

    private void MoveToCurrentPatrolPoint()
    {
        if (enemy.agentMover != null)
            enemy.agentMover.canMove = true;
        
        hasRequestedPath = true;
        enemy.RequestPath(patrolPoints[currentPatrolIndex]);
        Debug.Log($"[{enemy.name}] Moving to patrol point {currentPatrolIndex}");
    }

    private bool HasReachedCurrentPatrolPoint()
    {
        float distSqr = (transform.position - patrolPoints[currentPatrolIndex]).sqrMagnitude;
        return distSqr <= patrolPointReachThreshold * patrolPointReachThreshold;
    }

    private int GetClosestPatrolPointIndex()
    {
        int closest = 0;
        float closestDist = float.MaxValue;
        
        for (int i = 0; i < 4; i++)
        {
            float dist = (transform.position - patrolPoints[i]).sqrMagnitude;
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = i;
            }
        }
        return closest;
    }

    public override void ResetValues()
    {
        base.ResetValues();
        isWaitingAtPoint = false;
        waitTimer = 0f;
        hasRequestedPath = false;
    }
}
