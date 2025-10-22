using UnityEngine;

public class EnemyAttackState : EnemyState
{
    public EnemyAttackState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        // The specific behavior (like CircleAndShoot) handles its own entry logic
        enemy.EnemyAttackBaseInstance.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();
        // The specific behavior handles its own exit logic
        enemy.EnemyAttackBaseInstance.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        // --- THIS IS THE FIX ---
        // We only call the behavior's update logic.
        // The behavior script itself is now responsible for deciding when to change state.
        // We have REMOVED the line: if (!enemy.IsWithInAttackDistance) { stateMachine.ChangeState(enemy.ChaseState); }
        enemy.EnemyAttackBaseInstance.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.EnemyAttackBaseInstance.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.EnemyAttackBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }
}

