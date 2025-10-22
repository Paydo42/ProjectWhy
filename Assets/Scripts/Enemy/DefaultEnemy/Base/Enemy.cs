using UnityEngine;
using System;



public class Enemy : MonoBehaviour, IDamageable, IEnemyMoveable, ITriggerCheckAble
{
    [Header("Animation")]
    [Tooltip("The exact name of the Idle animation STATE in the Animator Controller.")]
    public string idleAnimationStateName = "Idle";
    [Tooltip("The exact name of the Trigger parameter for the death animation.")]
    public string deathTriggerName = "Die";
    [Tooltip("The exact name of the Trigger parameter for the take damage animation.")]
    public string takeDamageTriggerName = "TakeDamage";
    [Tooltip("The exact name of the Trigger parameter for the attack animation.")]
    //public string attackTriggerName = "Attack";

    [Header("Colliders")]
    //"Assign a child collider here used for obstacle avoidance. If null, the main collider will be used.")]
    public BoxCollider2D avoidanceCollider; // --- NEW: Collider for AI navigation ---


    public enum AnimationTriggerType
    {
    }

    [SerializeField] public EnemyIdleSOBase EnemyIdleBaseInstance;
    [SerializeField] public EnemyChaseSOBase EnemyChaseBaseInstance;
    [SerializeField] public EnemyAttackSOBase EnemyAttackBaseInstance;

    public GameObject OriginalPrefab { get; set; }
    private RoomBounds parentRoom;
    private bool isActivated = false;

    public EnemyStateMachine stateMachine { get; set; }
    public Animator animator { get; set; }
    public Enemy_Scriptable_Object enemyData;

    public Rigidbody2D RB { get; set; }
    public BoxCollider2D MainCollider { get; private set; } // Renamed for clarity

    public float MaxHealth { get; set; }
    public float CurrentHealth { get; set; }

    public event EnemyDeathDelegate OnEnemyDeath;

    public ITriggerCheckAble AggroCheck { get; set; }
    public ITriggerCheckAble AttackDistanceCheck { get; set; }

    public EnemyIdleState IdleState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }
    public EnemyAttackState AttackState { get; private set; }


    public virtual void Awake()
    {
        stateMachine = new EnemyStateMachine();
       

        IdleState = new EnemyIdleState(this, stateMachine);
        ChaseState = new EnemyChaseState(this, stateMachine);
        AttackState = new EnemyAttackState(this, stateMachine);

        animator = GetComponentInChildren<Animator>();
        RB = GetComponent<Rigidbody2D>();
        MainCollider = GetComponent<BoxCollider2D>();
        
          if (avoidanceCollider == null)
        {
            avoidanceCollider = MainCollider;
        }

        // SETUP FOR TRIGGER CHECKS
        AggroCheck = GetComponentInChildren<EnemyAggroCheck>();
        if (AggroCheck == null)
        {
            Debug.LogError($"Error ({gameObject.name}): 'EnemyAggroCheck' script'i ya eksik ya da ITriggerCheckAble arayüzünü uygulamıyor. Lütfen script'i düzeltin.", this);
        }

        AttackDistanceCheck = GetComponentInChildren<EnemyAttacktingDistanceCheck>();
        if (AttackDistanceCheck == null)
        {
            Debug.LogError($"KRİTİK HATA ({gameObject.name}): 'EnemyAttacktingDistanceCheck' script'i ya eksik ya da ITriggerCheckAble arayüzünü uygulamıyor. Lütfen script'i düzeltin.", this);
        }
        // --- DÜZELTME SONU ---

        if (EnemyIdleBaseInstance != null)
            EnemyIdleBaseInstance = Instantiate(EnemyIdleBaseInstance);
        if (EnemyChaseBaseInstance != null)
            EnemyChaseBaseInstance = Instantiate(EnemyChaseBaseInstance);
        if (EnemyAttackBaseInstance != null)
            EnemyAttackBaseInstance = Instantiate(EnemyAttackBaseInstance);

        if (EnemyIdleBaseInstance != null)
            EnemyIdleBaseInstance.Initialize(gameObject, this);
        if (EnemyChaseBaseInstance != null)
            EnemyChaseBaseInstance.Initialize(gameObject, this);
        if (EnemyAttackBaseInstance != null)
            EnemyAttackBaseInstance.Initialize(gameObject, this);
    }

    public virtual void Update()
    {
        if (!isActivated) return;

        if (stateMachine.CurrentEnemyState != null)
        {
            stateMachine.CurrentEnemyState.FrameUpdate();
        }
    }

    public virtual void FixedUpdate()
    {
        if (!isActivated) return;

        if (stateMachine.CurrentEnemyState != null)
        {
            stateMachine.CurrentEnemyState.PhysicsUpdate();
        }
    }

    public void Activate(RoomBounds room, GameObject prefab)
    {
        parentRoom = room;
        OriginalPrefab = prefab;

        MaxHealth = enemyData.maxHealth;
        CurrentHealth = MaxHealth;

        gameObject.SetActive(true);
        isActivated = true;

        if (animator != null)
        {
            animator.ResetTrigger(deathTriggerName);
            animator.ResetTrigger(takeDamageTriggerName);
           // animator.ResetTrigger(attackTriggerName);

            animator.Play(idleAnimationStateName, 0, 0f);
        }

        stateMachine.Initialize(IdleState);
        Debug.Log($"{name} has been activated!");
    }

    public void Deactivate()
    {
        isActivated = false;
        gameObject.SetActive(false);
    }

    public virtual void Die()
    {
        isActivated = false;

        OnEnemyDeath?.Invoke(this);

        if (animator != null)
        {
            animator.SetTrigger(deathTriggerName);
        }
    }

    public virtual void OnDeathAnimationComplete()
    {
        Debug.Log($"{name} death animation complete. Returning to pool.");
        if (PoolManager.Instance != null && OriginalPrefab != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject, OriginalPrefab);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AnimationTrigger(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }

    public virtual void TakeDamage(float damageAmount)
    {
        if (!isActivated) return;

        CurrentHealth -= damageAmount;
        AnimationTrigger(takeDamageTriggerName);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    public void MoveEnemy(Vector2 velocity)
    {
        if (!isActivated) return;
        RB.linearVelocity = velocity;
        CheckForLeftOrRightFacing(velocity);
    }

    public bool IsFacingRight { get; set; }
    public bool IsAggroed { get; set; }
    public bool IsWithInAttackDistance { get; set; }

    public void SetAggroStatus(bool isAggroed)
    {
        IsAggroed = isAggroed;
    }

    public void SetAttackDistanceStatus(bool isWithinStrikingDistance)
    {
        IsWithInAttackDistance = isWithinStrikingDistance;
    }

    public void CheckForLeftOrRightFacing(Vector2 velocity)
    {
        if (velocity.x > 0.01f)
        {
            IsFacingRight = true;
        }
        else if (velocity.x < -0.01f)
        {
            IsFacingRight = false;
        }
    }
}

