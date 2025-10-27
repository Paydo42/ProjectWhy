// Full Path: Assets/Scripts/Enemy/DefaultEnemy/State Machine/ConcreteStates/EnemyIdleState.cs
using UnityEngine;

public class EnemyIdleState : EnemyState
{
    public EnemyIdleState(Enemy enemy, EnemyStateMachine enemyStateMachine) : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
        // Debug.Log($"{enemy.name} entering Idle State."); // Less spammy

        if (enemy.agentMover != null) enemy.agentMover.canMove = true;

        // --- Configure Steering Behaviours ---
        if (enemy.seekBehaviour != null) enemy.seekBehaviour.enabled = true;
        if (enemy.circleTargetBehaviour != null) enemy.circleTargetBehaviour.enabled = false;
        if (enemy.obstacleAvoidanceBehaviour != null) enemy.obstacleAvoidanceBehaviour.enabled = true;
        if (enemy.wallFollowingBehaviour != null) enemy.wallFollowingBehaviour.enabled = true; // <<<< ENABLED
        // Disable any other specific movement behaviours here
    }

    public override void ExitState() { base.ExitState(); }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        if (enemy.IsAggroed) {
            enemy.stateMachine.ChangeState(enemy.ChaseState);
        }
    }

    public override void PhysicsUpdate() { base.PhysicsUpdate(); }
}