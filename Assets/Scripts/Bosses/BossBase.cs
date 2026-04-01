using System;
using UnityEngine;
using UnityEngine.Events;

public enum BossPhase
{
    Dormant = 0,
    Phase1 = 1,
    Phase2 = 2,
    Phase3 = 3,
    Dead = 99
}

public abstract class BossBase : MonoBehaviour, IDamageable
{
    [Header("Boss Health")]
    [SerializeField] private float maxHealth = 500f;
    [SerializeField, Range(0.01f, 0.99f)] private float phase2HealthThreshold = 0.70f;
    [SerializeField, Range(0.01f, 0.99f)] private float phase3HealthThreshold = 0.35f;

    [Header("Boss Events")]
    [SerializeField] private UnityEvent onEncounterStarted;
    [SerializeField] private UnityEvent onBossDefeated;

    private bool encounterStarted;
    private bool isDead;
    private bool invulnerable;

    public float MaxHealth { get; set; }
    public float CurrentHealth { get; set; }
    public BossPhase CurrentPhase { get; private set; } = BossPhase.Dormant;

    // Compatibility event for existing IDamageable usage in the project.
    public event EnemyDeathDelegate OnEnemyDeath;

    public event Action<BossBase> OnBossEncounterStarted;
    public event Action<BossBase, BossPhase> OnBossPhaseChanged;
    public event Action<BossBase> OnBossDefeated;

    protected virtual void Awake()
    {
        MaxHealth = Mathf.Max(1f, maxHealth);
        CurrentHealth = MaxHealth;
        CurrentPhase = BossPhase.Dormant;
    }

    public virtual void StartEncounter()
    {
        Debug.Log($"[BossBase] StartEncounter called — encounterStarted:{encounterStarted} isDead:{isDead} on {gameObject.name} instanceID:{GetInstanceID()}");
        if (encounterStarted || isDead)
        {
            return;
        }

        encounterStarted = true;
        CurrentHealth = MaxHealth;

        onEncounterStarted?.Invoke();
        OnBossEncounterStarted?.Invoke(this);

        EnterPhase(BossPhase.Phase1);
        OnEncounterStarted();
        Debug.Log("Boss Encounter Started: Phase 1");
    }

    public virtual void TakeDamage(float amount)
    {
        if (!encounterStarted || isDead || invulnerable || amount <= 0f)
        {
            Debug.Log($"[BossBase] TakeDamage blocked — encounterStarted:{encounterStarted} isDead:{isDead} invulnerable:{invulnerable} amount:{amount} name:{gameObject.name} instanceID:{GetInstanceID()}");
            return;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        Debug.Log($"[BossBase] HP:{CurrentHealth}/{MaxHealth} phase:{CurrentPhase}");

        OnDamaged(amount);

        if (CurrentHealth <= 0f)
        {
            Die();
            return;
        }

        EvaluatePhaseTransitions();
    }

    public virtual void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        invulnerable = true;
        CurrentHealth = 0f;
        CurrentPhase = BossPhase.Dead;

        onBossDefeated?.Invoke();
        OnBossDefeated?.Invoke(this);
        OnEnemyDeath?.Invoke(null);

        OnBossDeath();
    }

    protected void SetInvulnerable(bool value)
    {
        invulnerable = value;
    }

    protected void EnterPhase(BossPhase phase)
    {
        if (CurrentPhase == phase || isDead)
        {
            return;
        }

        CurrentPhase = phase;
        OnBossPhaseChanged?.Invoke(this, phase);
        OnPhaseChanged(phase);
    }

    private void EvaluatePhaseTransitions()
    {
        if (CurrentPhase == BossPhase.Phase1 && CurrentHealth <= MaxHealth * phase2HealthThreshold)
        {
            EnterPhase(BossPhase.Phase2);
            return;
        }

        if (CurrentPhase == BossPhase.Phase2 && CurrentHealth <= MaxHealth * phase3HealthThreshold)
        {
            EnterPhase(BossPhase.Phase3);
        }
    }

    protected virtual void OnEncounterStarted() { }
    protected virtual void OnDamaged(float amount) { }
    protected virtual void OnPhaseChanged(BossPhase newPhase) { }
    protected virtual void OnBossDeath() { }
}
