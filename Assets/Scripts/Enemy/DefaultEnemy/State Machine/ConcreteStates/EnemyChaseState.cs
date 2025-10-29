// Full Path: Assets/Scripts/Enemy/DefaultEnemy/State Machine/ConcreteStates/EnemyChaseState.cs
using UnityEngine;

public class EnemyChaseState : EnemyState
{
    private Transform playerTransform;
    public EnemyChaseState(Enemy enemy, EnemyStateMachine enemyStateMachine) : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
        // Debug.Log($"{enemy.name} entering Chase State."); // Less spammy
        playerTransform = enemy.playerTransform;
        if (playerTransform == null) Debug.LogError($"EnemyChaseState: Player Transform is null on entering state for {enemy.name}!");

        //  if (enemy.agentMover != null) enemy.agentMover.canMove = true;
        if (playerTransform != null)
        {
            enemy.StartPathfinding(playerTransform.position); // Start periodic path updates towards player
        }
        else
        {
            enemy.StopPathfinding(); // Stop if we can't find player
        }
        /*/ --- Configure Steering Behaviours ---
        if (enemy.seekBehaviour != null) enemy.seekBehaviour.enabled = true;
        if (enemy.circleTargetBehaviour != null) enemy.circleTargetBehaviour.enabled = false;
        if (enemy.obstacleAvoidanceBehaviour != null) enemy.obstacleAvoidanceBehaviour.enabled = true;
        if (enemy.wallFollowingBehaviour != null) enemy.wallFollowingBehaviour.enabled = true; // <<<< ENABLED
         Disable any other specific movement behaviours here */ 
    }

    public override void ExitState()
    {
        base.ExitState(); 
        enemy.StopPathfinding(); // Stop pathfinding when exiting chase state
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        if (playerTransform == null)
        {
            playerTransform = enemy.playerTransform; // Try to re-acquire if lost
            if (playerTransform == null)
            {
                Debug.LogWarning($"[{enemy.name}] Lost player target in ChaseState.");
                enemy.StopPathfinding(); // Stop if player is gone
                                         // Decide what state to go to - maybe Idle?
                if (!enemy.NeverReturnsToIdle) enemy.stateMachine.ChangeState(enemy.IdleState);
                return;
            }
        }
        enemy.currentPathfindingTarget = playerTransform.position; // Keep target updated
        // Check for transitions
        if (!enemy.NeverReturnsToIdle && !enemy.IsAggroed) 
        {
            enemy.stateMachine.ChangeState(enemy.IdleState);
            return;
        }
        if (enemy.IsWithInAttackDistance)
         {
            enemy.stateMachine.ChangeState(enemy.AttackState);
            return;
        }
    }

    public override void PhysicsUpdate() 
    {
        base.PhysicsUpdate();
    }
}