using UnityEngine;
using System;
public class Enemy : MonoBehaviour, IDamageable, IEnemyMoveable, ITriggerCheckAble
{
    public enum AnimationTriggerType
    {
        Attack,
        TakeDamage,
        Die
    }

    [SerializeField] public EnemyIdleSOBase EnemyIdleBaseInstance;
    [SerializeField] public EnemyChaseSOBase EnemyChaseBaseInstance;
    [SerializeField] public EnemyAttackSOBase EnemyAttackBaseInstance;

    // --- FIX: Create properties to hold instances of each state ---
    public EnemyIdleState IdleState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }
    public EnemyAttackState AttackState { get; private set; }

    // Pooling and Activation Logic
    public GameObject OriginalPrefab { get; set; }
    private RoomBounds parentRoom;
    private bool isActivated = false;

    // Core Components and Stats
    public EnemyStateMachine stateMachine { get; set; }
    public Animator animator { get; set; }
    public Enemy_Scriptable_Object enemyData;
    
    public Rigidbody2D RB { get; set; }
    
    public float MaxHealth { get; set; }
    public float CurrentHealth { get; set; }
    
    public event EnemyDeathDelegate OnEnemyDeath;

    // State Machine Triggers
    public ITriggerCheckAble AggroCheck { get; set; }
    public ITriggerCheckAble AttackDistanceCheck { get; set; }

    public virtual void Awake()
    {
        stateMachine = new EnemyStateMachine();

        // --- FIX: Create the state instances here ---
        IdleState = new EnemyIdleState(this, stateMachine);
        ChaseState = new EnemyChaseState(this, stateMachine);
        AttackState = new EnemyAttackState(this, stateMachine);

        animator = GetComponentInChildren<Animator>();
        RB = GetComponent<Rigidbody2D>();

        AggroCheck = GetComponentInChildren<EnemyAggroCheck>() as ITriggerCheckAble;
        AttackDistanceCheck = GetComponentInChildren<EnemyAttacktingDistanceCheck>() as ITriggerCheckAble;

        EnemyIdleBaseInstance = Instantiate(EnemyIdleBaseInstance);
        EnemyChaseBaseInstance = Instantiate(EnemyChaseBaseInstance);
        EnemyAttackBaseInstance = Instantiate(EnemyAttackBaseInstance);

        EnemyIdleBaseInstance.Initialize(gameObject, this);
        EnemyChaseBaseInstance.Initialize(gameObject, this);
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

        // --- FIX: Initialize with the pre-made IdleState instance ---
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
        if(animator != null) { animator.SetTrigger("Die"); }
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

    public virtual void TakeDamage(float damageAmount)
    {
        if (!isActivated) return;
        CurrentHealth -= damageAmount;
        if(animator != null) { animator.SetTrigger("TakeDamage"); }
        if (CurrentHealth <= 0) { Die(); }
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

    public void SetAggroStatus(bool isAggroed) { IsAggroed = isAggroed; }
    public void SetAttackDistanceStatus(bool isWithinStrikingDistance) { IsWithInAttackDistance = isWithinStrikingDistance; }

    public void CheckForLeftOrRightFacing(Vector2 velocity)
    {
        if (velocity.x > 0.01f) { IsFacingRight = true; }
        else if (velocity.x < -0.01f) { IsFacingRight = false; }
    }
}

