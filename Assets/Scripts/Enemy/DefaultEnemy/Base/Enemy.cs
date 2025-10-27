// Full Path: Assets/Scripts/Enemy/DefaultEnemy/Base/Enemy.cs
using UnityEngine;
using System;
using System.Collections.Generic; // Required for List if you use it later

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
    public SeekBehaviour seekBehaviour { get; private set; }
    public CircleTargetBehaviour circleTargetBehaviour { get; private set; }
    public ObstacleAvoidanceBehaviour obstacleAvoidanceBehaviour { get; private set; }
    public WallFollowingBehaviour wallFollowingBehaviour { get; private set; } // <<<< ADDED REFERENCE
    // Add other behaviour references here as needed

    // --- Trigger Checks & Status Flags ---
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
        // Initialize State Machine & States
        stateMachine = new EnemyStateMachine();
        IdleState = new EnemyIdleState(this, stateMachine);
        ChaseState = new EnemyChaseState(this, stateMachine);
        AttackState = new EnemyAttackState(this, stateMachine);

        // Get Core Components
        animator = GetComponentInChildren<Animator>();
        RB = GetComponent<Rigidbody2D>();
        MainCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Get AI Components
        aiData = GetComponent<AIData>();
        agentMover = GetComponent<AgentMover>();
        seekBehaviour = GetComponent<SeekBehaviour>();
        circleTargetBehaviour = GetComponent<CircleTargetBehaviour>();
        obstacleAvoidanceBehaviour = GetComponent<ObstacleAvoidanceBehaviour>();
        wallFollowingBehaviour = GetComponent<WallFollowingBehaviour>();

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

         // Validate essential AI components
         if(aiData == null) Debug.LogError($"'{gameObject.name}': Missing AIData component!", this);
         if(agentMover == null) Debug.LogError($"'{gameObject.name}': Missing AgentMover component!", this);
         // SeekBehaviour is generally required if using Idle/Chase states
         if(seekBehaviour == null && (startingState == EnemyStartState.StartIdle || startingState == EnemyStartState.StartChase))
             Debug.LogWarning($"'{gameObject.name}': Missing SeekBehaviour, may not move correctly in Idle/Chase.", this);

    }

    public virtual void Update()
    {
        if (!isActivated) return;

        // Delegate state logic to the current state
        stateMachine.CurrentEnemyState?.FrameUpdate();

        // Basic animation update based on movement (can be expanded)
        UpdateAnimation();
    }

    public virtual void FixedUpdate()
    {
        if (!isActivated) return;

        // Delegate physics logic (if any) to the current state
        stateMachine.CurrentEnemyState?.PhysicsUpdate();

        // Update facing direction based on current linearVelocity
        if (RB.linearVelocity.sqrMagnitude > 0.01f)
        {
            CheckForLeftOrRightFacing(RB.linearVelocity);
        }
    }

    public void Activate(RoomBounds room, GameObject prefab)
    {
        parentRoom = room;
        OriginalPrefab = prefab;

        // Set health from enemyData SO, with a fallback
        MaxHealth = enemyData != null ? enemyData.maxHealth : 10f; // Use 10 as default if SO missing
        CurrentHealth = MaxHealth;

        // Reset physics state before activating
        RB.linearVelocity = Vector2.zero;
       
        GetComponent<Collider2D>().enabled = true;

        gameObject.SetActive(true);
        isActivated = true;


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

    public void Deactivate()
    {
        if(!isActivated) return; // Prevent double deactivation

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