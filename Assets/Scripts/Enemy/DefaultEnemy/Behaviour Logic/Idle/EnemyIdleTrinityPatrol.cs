// Full Path: Assets/Scripts/Enemy/DefaultEnemy/Behaviour Logic/Idle/EnemyIdleTrinityPatrol.cs
using UnityEngine;

/// <summary>
/// Trinity-specific Idle behavior: 4-point rectangular patrol with left/right/up raycast detection.
/// 
/// Unlike Twins, Trinity:
/// - Can see LEFT, RIGHT, and UP (three directions)
/// - Transitions directly to ATTACK when player detected (skips Chase)
/// - Patrols 4 corner points at the edges of the current room
/// </summary>
[CreateAssetMenu(fileName = "Idle_TrinityPatrol", menuName = "Enemy Logic/Idle Logic/Trinity Patrol")]
public class EnemyIdleTrinityPatrol : EnemyIdleSOBase
{
    [Header("=== TRINITY VISION SETTINGS ===")]
    [Tooltip("How far Trinity can see on each side and up")]
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
    private bool hasRequestedPath = false;
    
    // References to eye transforms (from Trinity class)
    private Transform leftEye;
    private Transform rightEye;
    private Transform thirdEye;
    private Trinity trinityEnemy;

    // === PUBLIC GETTERS FOR GIZMO DRAWING ===
    public Vector3[] PatrolPoints => patrolPoints;
    public int CurrentPatrolIndex => currentPatrolIndex;
    public bool IsPatrolInitialized => patrolInitialized;
    public bool IsWaiting => isWaitingAtPoint;

    public override void Initialize(GameObject gameObject, Enemy enemy)
    {
        base.Initialize(gameObject, enemy);
        
        // Try to get Trinity-specific references
        trinityEnemy = enemy as Trinity;
        if (trinityEnemy != null)
        {
            leftEye = trinityEnemy.LeftEye;
            rightEye = trinityEnemy.RightEye;
            thirdEye = trinityEnemy.ThirdEye;
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
        // Don't call MoveToCurrentPatrolPoint here - let UpdatePatrol handle it
        
        Debug.Log($"[{enemy.name}] Trinity Patrol started at point {currentPatrolIndex}");
    }

    public override void DoFrameUpdateLogic()
    {
        // DON'T call base - we override the aggro check completely
        
        // === TRINITY SPECIAL VISION: Check LEFT, RIGHT, and UP ===
        if (CanSeePlayer(out Vector2 detectedDirection, out int eyeIndex))
        {
            string eyeName = eyeIndex == 0 ? "LEFT" : (eyeIndex == 1 ? "RIGHT" : "TOP");
            Debug.Log($"[{enemy.name}] Detected player with {eyeName} eye!");
            
            // Store info for attack state (if Trinity class)
            if (trinityEnemy != null)
            {
                trinityEnemy.LastKnownTargetPosition = playerTransform.position;
                trinityEnemy.LastDetectedDirection = detectedDirection;
                trinityEnemy.LastDetectedEyeIndex = eyeIndex;
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
    // VISION SYSTEM (LEFT, RIGHT & UP RAYCASTS)
    // ==========================================

    /// <summary>
    /// Check if player is visible from left, right, or up
    /// </summary>
    private bool CanSeePlayer(out Vector2 detectedDirection, out int eyeIndex)
    {
        detectedDirection = Vector2.zero;
        eyeIndex = -1;
        
        // Check LEFT (eye index 0)
        if (RaycastForPlayer(Vector2.left, leftEye))
        {
            detectedDirection = Vector2.left;
            eyeIndex = 0;
            return true;
        }
        
        // Check RIGHT (eye index 1)
        if (RaycastForPlayer(Vector2.right, rightEye))
        {
            detectedDirection = Vector2.right;
            eyeIndex = 1;
            return true;
        }
        
        // Check UP (eye index 2 - Third Eye)
        if (RaycastForPlayer(Vector2.up, thirdEye))
        {
            detectedDirection = Vector2.up;
            eyeIndex = 2;
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
            Color rayColor;
            if (direction == Vector2.left) rayColor = Color.blue;
            else if (direction == Vector2.right) rayColor = Color.red;
            else rayColor = Color.magenta; // Up
            
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
        RoomBounds roomBounds = enemy.ParentRoom;
        
        if (roomBounds != null)
        {
            BoxCollider2D roomCollider = roomBounds.GetComponent<BoxCollider2D>();
            
            if (roomCollider != null)
            {
                Bounds bounds = roomCollider.bounds;
                
                float minX = bounds.min.x + roomEdgePadding;
                float maxX = bounds.max.x - roomEdgePadding;
                float minY = bounds.min.y + roomEdgePadding;
                float maxY = bounds.max.y - roomEdgePadding;
                
                // Define 4 corners
                patrolPoints[0] = new Vector3(minX, minY, 0); // Bottom-left
                patrolPoints[1] = new Vector3(maxX, minY, 0); // Bottom-right
                patrolPoints[2] = new Vector3(maxX, maxY, 0); // Top-right
                patrolPoints[3] = new Vector3(minX, maxY, 0); // Top-left
                
                patrolOrigin = bounds.center;

                Debug.Log($"[{enemy.name}] Trinity patrol points initialized from RoomBounds");
                return;
            }
        }
        
        // Fallback: use local area around spawn
        Debug.LogWarning($"[{enemy.name}] No RoomBounds found, using local patrol area");
        float size = 3f;
        patrolPoints[0] = patrolOrigin + new Vector3(-size, -size, 0);
        patrolPoints[1] = patrolOrigin + new Vector3(size, -size, 0);
        patrolPoints[2] = patrolOrigin + new Vector3(size, size, 0);
        patrolPoints[3] = patrolOrigin + new Vector3(-size, size, 0);
    }

    private int GetClosestPatrolPointIndex()
    {
        float closestDist = float.MaxValue;
        int closestIndex = 0;
        
        for (int i = 0; i < 4; i++)
        {
            float dist = Vector3.Distance(transform.position, patrolPoints[i]);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestIndex = i;
            }
        }
        
        return closestIndex;
    }

    private void MoveToCurrentPatrolPoint()
    {
        if (enemy.agentMover != null)
            enemy.agentMover.canMove = true;
        
        hasRequestedPath = true;
        enemy.RequestPath(patrolPoints[currentPatrolIndex]);
        
        Debug.Log($"[{enemy.name}] Moving to patrol point {currentPatrolIndex}");
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
                
                // Move to next point
                currentPatrolIndex = (currentPatrolIndex + 1) % 4;
                MoveToCurrentPatrolPoint();
            }
        }
        else
        {
            // Check if we've reached the patrol point OR if the path has completed
            // Path completion means we've reached as close as pathfinding can get us
            bool pathCompleted = hasRequestedPath && enemy.agentMover != null && !enemy.agentMover.isFollowingPath;
            
            float distToTarget = Vector3.Distance(transform.position, patrolPoints[currentPatrolIndex]);
            bool reachedPoint = distToTarget <= patrolPointReachThreshold;
            
            if (reachedPoint || pathCompleted)
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

    public override void ResetValues()
    {
        base.ResetValues();
        patrolInitialized = false;
        currentPatrolIndex = 0;
        isWaitingAtPoint = false;
        waitTimer = 0f;
        hasRequestedPath = false;
    }
}
