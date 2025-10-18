using UnityEngine;

public class EnemyChaseState : EnemyState
{
   
    public EnemyChaseState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
      
    }

    public override void EnterState()
    {
        base.EnterState();
       
        enemy.EnemyChaseBaseInstance.DoEnterLogic();
        // Additional logic for entering chase state can be added here
    }

    public override void ExitState()
    {
        base.ExitState();
       
        enemy.EnemyChaseBaseInstance.DoExitLogic();
        // Additional logic for exiting chase state can be added here
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        enemy.EnemyChaseBaseInstance.DoFrameUpdateLogic();
         if (!enemy.IsAggroed)
        {
            stateMachine.ChangeState(enemy.IdleState);
        }
        else if (enemy.IsWithInAttackDistance)
        {
            stateMachine.ChangeState(enemy.AttackState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.EnemyChaseBaseInstance.DoPhysicsLogic();
        // Logic for physics updates during chase can be added here
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.EnemyChaseBaseInstance.DoAnimationTriggerEventLogic(triggerType);
        // Handle animation triggers specific to chase state
    }
}
