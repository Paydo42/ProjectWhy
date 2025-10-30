// Full Path: Assets/Scripts/Enemy/DefaultEnemy/State Machine/ConcreteStates/EnemyAttackState.cs
using UnityEngine;

public class EnemyAttackState : EnemyState
{
    // --- REMOVED shooting timer ---
    private EnemyAttackSOBase attackDataSO; // Keep reference to base type
    private float exitAttackRangeBuffer = 1.0f;
    private Transform playerTransform; // Keep for transition check

    public EnemyAttackState(Enemy enemy, EnemyStateMachine enemyStateMachine) : base(enemy, enemyStateMachine)
    {
        attackDataSO = enemy.EnemyAttackBaseInstance;
        if (attackDataSO == null) {
            Debug.LogError($"EnemyAttackState on {enemy.name} requires an EnemyAttackSOBase assigned!", enemy);
        }
    }

    public override void EnterState()
    {
        base.EnterState();
        // Debug.Log($"==== {enemy.name} entering Attack State ({attackDataSO?.name}) ====");

        playerTransform = enemy.playerTransform;
         if (playerTransform == null) Debug.LogError($"EnemyAttackState: Player Transform not found for {enemy.name}!", enemy);

        // Tell the SO to initialize its logic (movement AND shooting timers)
        attackDataSO?.DoEnterLogic();
        Debug.Log($"{enemy.name} entered Attack State using {attackDataSO?.name}.");
    }

    public override void ExitState()
    {
        base.ExitState();
        // Debug.Log($"==== {enemy.name} exiting Attack State ({attackDataSO?.name}) ====");

        // Tell the SO to clean up its logic
        attackDataSO?.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        if (attackDataSO == null || playerTransform == null) return;

        // --- Delegate ALL frame logic (movement AND shooting) to the SO ---
        attackDataSO.DoFrameUpdateLogic();
        // --- End Delegation ---


        // --- REMOVED Shooting Logic (now fully in SO) ---


        // --- Check for State Transitions (remains in state) ---
        float distanceToPlayerSqr = (playerTransform.position - enemy.transform.position).sqrMagnitude;
        // Still need preferredShootingRange from the SO to check the exit condition
        float preferredRange = attackDataSO.preferredShootingRange;
        float bufferedRangeSqr = Mathf.Pow(preferredRange + exitAttackRangeBuffer, 2);

        if (distanceToPlayerSqr > bufferedRangeSqr)
        {
            // Debug.Log($"AttackState ({enemy.name}): Player out of buffered range (Dist^2 {distanceToPlayerSqr:F2} > Buffered^2 {bufferedRangeSqr:F2}), changing to Chase State.");
            enemy.stateMachine.ChangeState(enemy.ChaseState);
            return; // Exit early if transitioning
        }
        // --- End Transitions ---
    }


    // --- REMOVED Helper Methods (now fully in SO) ---
    // private bool HasLineOfSight() { ... }
    // private void PerformShoot() { ... }
    // --- END REMOVED ---

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        // Delegate physics logic to SO if it has any
        attackDataSO?.DoPhysicsUpdateLogic(); // Use correct base method name
    }
}