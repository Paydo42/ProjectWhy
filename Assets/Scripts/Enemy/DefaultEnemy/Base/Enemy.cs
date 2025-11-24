// Full Path: Assets/Scripts/Enemy/DefaultEnemy/Base/Enemy.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;


public enum EnemyStartState
{
    StartIdle,  // The default behavior
    StartChase // For enemies like the Devil that chase immediately
    // You could add more later, like StartPatrol, StartFlee, etc.
}

public class Enemy : MonoBehaviour, IDamageable, IEnemyMoveable, ITriggerCheckAble
{
    [Header("Animation")]
    [Tooltip("The exact name of the Idle animation STATE in the Animator Controller.")]
    public string idleAnimationStateName = "Idle";
    [Tooltip("The exact name of the Trigger parameter for the death animation.")]
    public string deathTriggerName = "Die";
    [Tooltip("The exact name of the Trigger parameter for the take damage animation.")]
    public string takeDamageTriggerName = "TakeDamage";
    // [Tooltip("The exact name of the Trigger parameter for the attack animation.")] // Uncomment if you add an attack trigger
    // public string attackTriggerName = "Attack";

    [Header("State Machine Config")]
    [SerializeField, Tooltip("Determines the initial state the enemy enters upon activation.")]
    private EnemyStartState startingState = EnemyStartState.StartIdle;

    [SerializeField, Tooltip("If true, this enemy will never return to Idle once it starts chasing.")]
    private bool neverReturnsToIdle = false;
    // Public property to safely access the private neverReturnsToIdle field from other scripts (like states)
    public bool NeverReturnsToIdle => neverReturnsToIdle;

    [Header("Pathfinding & Movement")]
    [SerializeField, Tooltip("How often the enemy recalculates its path when actively pathfinding (seconds). Lower values are more responsive but cost more performance.")]  
     private float pathUpdateInterval = 0.5f;
     private float pathUpdateTimer = 0f;
     private bool isPathfindingActive = false; // Flag to control periodic updates
     public Vector3 currentPathfindingTarget; // Where are we trying to go?
   // --- MODIFIED: Public getter, private setter. Set via Activate ---
    public GridGenerator currentRoomGridGenerator { get; private set; }
    // --- END MODIFICATION ---

    [Header("Combat Config")] // Header for combat-related settings
    [SerializeField, Tooltip("Layers that block this enemy's line of sight for shooting.")]
    private LayerMask lineOfSightMask = 1; // Default to Layer 1 ('Default'). Assign Obstacle layer in Inspector!
    // Public property to allow states to read the mask
    public LayerMask LineOfSightMask => lineOfSightMask;

    [Header("Colliders")]
    [Tooltip("Assign a child collider here used for specific checks if needed. If null, the main collider might be used as fallback elsewhere.")]
    public BoxCollider2D avoidanceCollider;

    // Nested enum for Animation Triggers (consider moving outside if used by Animator directly)
    public enum AnimationTriggerType
    {
        // Define specific trigger types if needed by DoAnimationTriggerEventLogic
    }

    [Header("Scriptable Object References")]
    [SerializeField] public EnemyIdleSOBase EnemyIdleBaseInstance;   // Primarily for non-movement Idle data now
    [SerializeField] public EnemyChaseSOBase EnemyChaseBaseInstance; // Primarily for non-movement Chase data now
    [SerializeField] public EnemyAttackSOBase EnemyAttackBaseInstance; // Holds specific attack data (bullet, timings, range, etc.)

    // --- Core Properties & State ---
    public GameObject OriginalPrefab { get; set; } // Used by PoolManager
    private RoomBounds parentRoom; // Reference to the room it's in (if applicable)
    private bool isActivated = false; // Controls if Update/FixedUpdate run
    public EnemyStateMachine stateMachine { get; set; }
    public Enemy_Scriptable_Object enemyData; // Holds base stats like MaxHealth

    // --- Health ---
    public float MaxHealth { get; set; }
    public float CurrentHealth { get; set; }
    public event EnemyDeathDelegate OnEnemyDeath; // Event triggered on death

    // --- Core Components ---
    public Animator animator { get; set; }
    public Rigidbody2D RB { get; set; }
    public BoxCollider2D MainCollider { get; private set; }
    private SpriteRenderer spriteRenderer;

    // --- AI Components (Context Steering) ---
    public AIData aiData { get; private set; }
    public AgentMover agentMover { get; private set; }
  /*  public SeekBehaviour seekBehaviour { get; private set; }
    public CircleTargetBehaviour circleTargetBehaviour { get; private set; }
    public ObstacleAvoidanceBehaviour obstacleAvoidanceBehaviour { get; private set; }
    public WallFollowingBehaviour wallFollowingBehaviour { get; private set; } // <<<< ADDED REFERENCE
    // Add other behaviour references here as needed
    */
    // --- Trigger Checks & Status Flags ---
    public Transform playerTransform { get; private set; } // Store player ref here
    public ITriggerCheckAble AggroCheck { get; set; }
    public ITriggerCheckAble AttackDistanceCheck { get; set; }
    public bool IsFacingRight { get; set; } = true;
    public bool IsAggroed { get; set; } = false;
    public bool IsWithInAttackDistance { get; set; } = false;

    // --- State Instances ---
    public EnemyIdleState IdleState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }
    public EnemyAttackState AttackState { get; private set; }


    public virtual void Awake()
    {
 
        // Get Core Components
        animator = GetComponentInChildren<Animator>();
        RB = GetComponent<Rigidbody2D>();
        MainCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Get AI Components
        aiData = GetComponent<AIData>();
        agentMover = GetComponent<AgentMover>();
        /* seekBehaviour = GetComponent<SeekBehaviour>();
        circleTargetBehaviour = GetComponent<CircleTargetBehaviour>();
        obstacleAvoidanceBehaviour = GetComponent<ObstacleAvoidanceBehaviour>();
        wallFollowingBehaviour = GetComponent<WallFollowingBehaviour>();
        */
        // Get Player Transform Reference
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
        else Debug.LogError("Enemy Awake: No GameObject with tag 'Player' found in the scene.", this);


         // Instantiate and Initialize Scriptable Object Instances (primarily for Attack Data now)
        if (EnemyIdleBaseInstance != null)
        {
            EnemyIdleBaseInstance = Instantiate(EnemyIdleBaseInstance);
            EnemyIdleBaseInstance.Initialize(gameObject, this);
        }
        if (EnemyChaseBaseInstance != null)
        {
            EnemyChaseBaseInstance = Instantiate(EnemyChaseBaseInstance);
            EnemyChaseBaseInstance.Initialize(gameObject, this);
        }
         if (EnemyAttackBaseInstance != null)
        {
            EnemyAttackBaseInstance = Instantiate(EnemyAttackBaseInstance);
            EnemyAttackBaseInstance.Initialize(gameObject, this);
        } else {
             Debug.LogError($"'{gameObject.name}': Missing required Enemy Attack Base Instance ScriptableObject assignment!", this);
        }
               // Initialize State Machine & States
        stateMachine = new EnemyStateMachine();
        IdleState = new EnemyIdleState(this, stateMachine);
        ChaseState = new EnemyChaseState(this, stateMachine);
        AttackState = new EnemyAttackState(this, stateMachine);

        // Fallback for avoidanceCollider
        if (avoidanceCollider == null)
        {
            avoidanceCollider = MainCollider;
        }

        // Setup Trigger Check Components
        AggroCheck = GetComponentInChildren<EnemyAggroCheck>();
        if (AggroCheck == null)
        {
             Debug.LogError($"'{gameObject.name}': Missing 'EnemyAggroCheck' component/script.", this);
        }

        AttackDistanceCheck = GetComponentInChildren<EnemyAttacktingDistanceCheck>();
        if (AttackDistanceCheck == null)
        {
            Debug.LogError($"'{gameObject.name}': Missing 'EnemyAttacktingDistanceCheck' component/script.", this);
        }

       
         // Validate essential AI components
       //  if(aiData == null) Debug.LogError($"'{gameObject.name}': Missing AIData component!", this);
         if(agentMover == null) Debug.LogError($"'{gameObject.name}': Missing AgentMover component!", this);
    }

    public virtual void Update()
    {
        if (!isActivated) return;

        // Delegate state logic to the current state
        stateMachine.CurrentEnemyState?.FrameUpdate();
        if (isPathfindingActive)
        {
            pathUpdateTimer += Time.deltaTime;
            if (pathUpdateTimer >= pathUpdateInterval)
            {
                pathUpdateTimer = 0f;
                // Recalculate path to current target
                RequestPath(currentPathfindingTarget);
            }
        }
        // Basic animation update based on movement (can be expanded)
        UpdateAnimation();
    }

    public virtual void FixedUpdate()
    {
        if (!isActivated) return;

        // Delegate physics logic (if any) to the current state
        stateMachine.CurrentEnemyState?.PhysicsUpdate();

        /*/ Update facing direction based on current linearVelocity
        if (RB.linearVelocity.sqrMagnitude > 0.01f)
        {
            CheckForLeftOrRightFacing(RB.linearVelocity);
        }
        */
    }

    public void Activate(RoomBounds room, GameObject prefab , GridGenerator gridGenerator)
    {
        parentRoom = room;
        OriginalPrefab = prefab;

        // --- Store the passed GridGenerator ---
        this.currentRoomGridGenerator = gridGenerator;
        if (this.currentRoomGridGenerator == null)
        {
            Debug.LogError($"Enemy '{name}' was activated without a valid GridGenerator!", this);
            // Decide how to handle this - disable movement? Deactivate? Throw error?
            // For now, it will just fail pathfinding requests.
        }
        // --- End Store GridGenerator ---
        //Get grid generator for the room
       

        // Set health from enemyData SO, with a fallback
        MaxHealth = enemyData != null ? enemyData.maxHealth : 10f; // Use 10 as default if SO missing
        CurrentHealth = MaxHealth;

        // Reset physics state before activating
        RB.linearVelocity = Vector2.zero;

        GetComponent<Collider2D>().enabled = true;

        gameObject.SetActive(true);
        isActivated = true;
          StopPathfinding(); // Ensure pathfinding is off initially
        Debug.Log($" Pathfinding stopped on activation for '{name}'."); 

        // Reset Animator
        if (animator != null)
        {
            animator.ResetTrigger(deathTriggerName);
            animator.ResetTrigger(takeDamageTriggerName);
            // animator.ResetTrigger(attackTriggerName); // Uncomment if using
            animator.Play(idleAnimationStateName, 0, 0f); // Ensure starting in idle anim
        }

        // Initialize State Machine based on Inspector setting
        switch (startingState)
        {
            case EnemyStartState.StartChase:
                stateMachine.Initialize(ChaseState);
                Debug.Log($"'{name}' activated directly into Chase State.");
                // Ensure Aggro is set if starting in Chase, otherwise Chase might immediately exit
                SetAggroStatus(true);
                break;

            case EnemyStartState.StartIdle:
            default:
                stateMachine.Initialize(IdleState);
                Debug.Log($"'{name}' activated into Idle State.");
                // Ensure Aggro is reset if starting in Idle
                SetAggroStatus(false);
                break;
        }
        // Reset attack distance flag on activation
        SetAttackDistanceStatus(false);
      
    }

    public void StartPathfinding(Vector3 targetPosition)
    {
        if (!isActivated || agentMover == null) return;

        currentPathfindingTarget = targetPosition;
        isPathfindingActive = true;
        pathUpdateTimer = 0f; // Reset timer to trigger immediate path request
        RequestPath(targetPosition);
        agentMover.canMove = true; // Ensure movement is enabled
    }
    public void StopPathfinding()
    {
        if (agentMover == null) return; // Can happen during Awake/Deactivate
        isPathfindingActive = false;
        pathUpdateTimer = 0f;
        agentMover.StopMovement(); // Safely stop movement if agentMover exists
        agentMover.canMove = false;
        Debug.Log($"'{name}' stopped pathfinding.");
    }
    public void RequestPath(Vector3 targetPosition)
    {
        if (isActivated && AStarManager.Instance != null && currentRoomGridGenerator != null && agentMover != null)
        {
            List<Node> newPath = AStarManager.Instance.FindPath(currentRoomGridGenerator, transform.position, targetPosition);
            agentMover.SetPath(newPath); // Update the agentMover with the new path
        }
        else if (isActivated)
        {
            if (AStarManager.Instance == null) Debug.LogWarning($"[{name}] Cannot request path: AStarManager instance missing.");
            else if (currentRoomGridGenerator == null) Debug.LogWarning($"[{name}] Cannot request path: GridGenerator missing.");
            // else if(playerTransform == null) Debug.LogWarning($"[{name}] Cannot request path: Player Transform missing."); // Target isn't always player
            else if (agentMover == null) Debug.LogWarning($"[{name}] Cannot request path: AgentMover missing.");

            if (agentMover != null) agentMover.StopMovement();
        }
    }

    
    public void Deactivate()
    {
        if(!isActivated) return; // Prevent double deactivation
        StopPathfinding();
        Debug.Log($"'{name}' deactivated.");
        isActivated = false;
        RB.linearVelocity = Vector2.zero; // Stop movement immediately
        gameObject.SetActive(false);
        // Reset state machine? Optional, depends if Initialize handles it on Activate.
        // stateMachine.ChangeState(null); // Or stateMachine.Initialize(null);
    }

    public virtual void Die()
    {
        if (!isActivated) return; // Already dying or deactivated
        StopPathfinding();
        Debug.Log($"'{name}' died.");
        isActivated = false;
        RB.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false; // Disable collider

        OnEnemyDeath?.Invoke(this); // Notify listeners

        if (animator != null && !string.IsNullOrEmpty(deathTriggerName))
        {
            animator.SetTrigger(deathTriggerName);
            // Pooling/destruction will happen via OnDeathAnimationComplete
        }
        else
        {
            // No animator or death animation, pool/destroy immediately
            ReturnToPoolOrDestroy();
        }
    }

    // Called via Animation Event at the end of the death animation clip
    public virtual void OnDeathAnimationComplete()
    {
        Debug.Log($"'{name}' death animation complete.");
        ReturnToPoolOrDestroy();
    }

     // Handles returning the object to the pool or destroying it
    private void ReturnToPoolOrDestroy() {
        // Reset kinematic/collider state before pooling/destroying if needed for reuse
        GetComponent<Collider2D>().enabled = true; // Usually want this reset

        if (PoolManager.Instance != null && OriginalPrefab != null)
        {
            Debug.Log($"Returning '{name}' to pool.");
            PoolManager.Instance.ReturnToPool(gameObject, OriginalPrefab);
        }
        else
        {
            Debug.Log($"Destroying '{name}'.");
            Destroy(gameObject);
        }
    }


    public void AnimationTrigger(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }

    public virtual void TakeDamage(float damageAmount)
    {
        if (!isActivated || CurrentHealth <= 0) return; // Don't take damage if deactivated or already dead

        CurrentHealth -= damageAmount;
        Debug.Log($"'{name}' took {damageAmount} damage. Health: {CurrentHealth}/{MaxHealth}");
        AnimationTrigger(takeDamageTriggerName);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    // Obsolete for direct movement, potentially useful for knockback via AddForce
    public void MoveEnemy(Vector2 forceOrVelocity)
    {
        // Example for knockback:
        // if (isActivated) { RB.AddForce(forceOrVelocity, ForceMode2D.Impulse); }
    }

    // Called by trigger checks to update status
    public void SetAggroStatus(bool isAggroed)
    {
        // Update only on change, unless force update needed?
        //if (this.IsAggroed != isAggroed)
        //{
             this.IsAggroed = isAggroed;
             //Debug.Log($"'{name}' Aggro Status changed to: {isAggroed}");
        //}
    }

    public void SetAttackDistanceStatus(bool isWithinStrikingDistance)
    {
        // Update only on change?
        //if (this.IsWithInAttackDistance != isWithinStrikingDistance)
        //{
             this.IsWithInAttackDistance = isWithinStrikingDistance;
             //Debug.Log($"'{name}' Within Attack Distance changed to: {isWithinStrikingDistance}");
        //}
    }

    // Flips the enemy sprite based on horizontal movement direction
    public void CheckForLeftOrRightFacing(Vector2 linearVelocity)
    {
        // Only update facing direction if there is significant horizontal movement
        if (Mathf.Abs(linearVelocity.x) > 0.1f) // Use a small threshold
        {
            bool shouldFaceRight = linearVelocity.x > 0;
           if (spriteRenderer != null)
        {
            // flipX is true when facing LEFT, false when facing RIGHT
            spriteRenderer.flipX = !shouldFaceRight;
        }
        // --- END MODIFICATION ---

        // Update the IsFacingRight flag (used potentially by other logic like fire points)
        // Note: We might not even need IsFacingRight if only used for flipping.
        // But keep it for now if other systems rely on it.
        IsFacingRight = shouldFaceRight;
        }
    }

    // Example animation update logic (can be expanded)
    private void UpdateAnimation()
    {
         if (animator == null || RB == null) return;

         // Determine if moving based on AgentMover's state and Rigidbody linearVelocity
         bool isMoving = agentMover != null && agentMover.canMove && RB.linearVelocity.sqrMagnitude > 0.1f;
      //   animator.SetBool("IsWalking", isMoving);

         // Example: Set blend tree parameters based on last move direction (if using blend tree)
         // Assuming you have parameters like "LastMoveX", "LastMoveY"
         // if (!isMoving) // Could reset blend tree to idle pose
         // {
         //    animator.SetFloat("LastMoveX", IsFacingRight ? 1 : -1); // Or based on actual last input/linearVelocity
         //    animator.SetFloat("LastMoveY", 0);
         // } else {
         //    // Update based on linearVelocity or desired direction from AI
         //    // This might need more refinement depending on your animator setup
         //    Vector2 currentVelNorm = RB.linearVelocity.normalized;
         //    animator.SetFloat("LastMoveX", currentVelNorm.x);
         //    animator.SetFloat("LastMoveY", currentVelNorm.y);
         // }
    }
}