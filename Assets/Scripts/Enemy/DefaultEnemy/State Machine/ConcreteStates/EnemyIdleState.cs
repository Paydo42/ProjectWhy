using UnityEngine;

public class EnemyIdleState : EnemyState
{
    public EnemyIdleState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }
    public override void EnterState()
    {
        base.EnterState();
        enemy.EnemyIdleBaseInstance.DoEnterLogic();
    }
    public override void ExitState()
    {
        base.ExitState();
        enemy.EnemyIdleBaseInstance.DoExitLogic();
    }
    public override void FrameUpdate()
    {
        base.FrameUpdate();
        // --- THIS IS THE FIX ---
        // We only call the behavior's update logic.
        // The specific behavior script (like EnemyIdleSOBase or EnemyIdleApproachPlayer)
        // is now responsible for deciding when to change state.
        enemy.EnemyIdleBaseInstance.DoFrameUpdateLogic();
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.EnemyIdleBaseInstance.DoPhysicsLogic();
    }
    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.EnemyIdleBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }
}

