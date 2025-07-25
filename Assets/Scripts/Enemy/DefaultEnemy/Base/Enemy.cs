using NUnit.Framework;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable, IEnemyMoveables, ITriggerCheckAble
{
    // Health
    
    [field: SerializeField] public float MaxHealth { get; set; } = 10f;
    [SerializeField] protected Animator animator;
    protected bool isDead = false;

    public float CurrentHealth { get; set; }

    // Movement
    public Rigidbody2D RB { get; set; }
    public bool IsFacingRight { get; set; } = true;

    // AI State
    public bool IsAggroed { get; set; }
    public bool IsWithInAttackDistance { get; set; }

    #region State Machine Variables
    public EnemyStateMachine StateMachine { get; private set; }
    public EnemyIdleState IdleState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }
    public EnemyAttackState AttackState { get; private set; }
    #endregion

    #region Scriptable Object Variables
    [SerializeField] private EnemyIdleSOBase EnemyIdleSOBase;
    [SerializeField] private EnemyChaseSOBase EnemyChaseSOBase;
    [SerializeField] private EnemyAttackSOBase EnemyAttackSOBase;

    public EnemyIdleSOBase EnemyIdleBaseInstance { get; set; }
    public EnemyChaseSOBase EnemyChaseBaseInstance { get; set; }
    public EnemyAttackSOBase EnemyAttackBaseInstance { get; set; }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        EnemyIdleBaseInstance = Instantiate(EnemyIdleSOBase);
        EnemyChaseBaseInstance = Instantiate(EnemyChaseSOBase);
        EnemyAttackBaseInstance = Instantiate(EnemyAttackSOBase);

        StateMachine = new EnemyStateMachine();

        IdleState = new EnemyIdleState(this, StateMachine);
        ChaseState = new EnemyChaseState(this, StateMachine);
        AttackState = new EnemyAttackState(this, StateMachine);
    }

    private void Start()
    {
        CurrentHealth = MaxHealth;
        RB = GetComponent<Rigidbody2D>();

        EnemyIdleBaseInstance.Initialize(gameObject, this);
        EnemyChaseBaseInstance.Initialize(gameObject, this);
        EnemyAttackBaseInstance.Initialize(gameObject, this);

        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        StateMachine.CurrentEnemyState.FrameUpdate();
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentEnemyState.PhysicsUpdate();
    }
    #endregion

    #region Health & Damage
    public void TakeDamage(float amount)
    {
     if (isDead) return;
        CurrentHealth -= amount;
        Debug.Log($"Enemy damaged. Current Health: {CurrentHealth}");
        AnimationTriggerEvent(AnimationTriggerType.EnemyDamaged);
        animator.SetTrigger("IsTakingDamage");

        if (CurrentHealth <= 0)
        {
            AnimationTriggerEvent(AnimationTriggerType.Death);

        }
    }

    public virtual void Die()
    {
        if (isDead) return;
       isDead = true;
        GetComponent<Rigidbody2D>().simulated = false;
        GetComponent<Collider2D>().enabled = false;
        Debug.Log("Enemy has died.");
        
    }
      public virtual void OnDeathAnimationComplete()
    {
        
        Destroy(gameObject);
    }
    #endregion

    #region Movement
    public void MoveEnemy(Vector2 velocity)
    {
        RB.linearVelocity = velocity;
        CheckForLeftOrRightFacing(velocity);
    }

    public void CheckForLeftOrRightFacing(Vector2 velocity)
    {
        if (IsFacingRight && velocity.x < 0f)
        {
            Vector3 rotator = new(transform.rotation.x, 180f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotator);
            IsFacingRight = false;
            //IsFacingRight = !IsFacingRight; // Toggle facing direction
        }
        else if (!IsFacingRight && velocity.x > 0f)
        {
            Vector3 rotator = new(transform.rotation.x, 0f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotator);
            IsFacingRight = true;
            //IsFacingRight = !IsFacingRight; // Toggle facing direction
        }
    }
    #endregion

    #region Animation Trigger Event
    public void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        StateMachine.CurrentEnemyState.AnimationTriggerEvent(triggerType);
        switch (triggerType)
        {
            case AnimationTriggerType.EnemyDamaged:
                // Logic for when the enemy is damaged
                break;
            case AnimationTriggerType.PlayFootstepSound:
                // Logic for playing footstep sound
                break;
            case AnimationTriggerType.Death:
                // Logic for death animation
                Die();
                Debug.Log("Enemy death animation triggered.");
                break;
        }
    }

    public enum AnimationTriggerType
    {
        EnemyDamaged,
        PlayFootstepSound,
        Death
    }
    #endregion

    #region Distance Check
    public void SetAggroStatus(bool isAggroed)
    {
        IsAggroed = isAggroed;
    }

    public void SetAttackDistanceStatus(bool isWithInAttackDistance)
    {
        IsWithInAttackDistance = isWithInAttackDistance;
    }
    #endregion
}