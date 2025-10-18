using System.Numerics;
using UnityEngine;

public class EnemyAttackState : EnemyState
{
   
    public EnemyAttackState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("Entering Attack State");
        enemy.EnemyAttackBaseInstance.DoEnterLogic();
        // Additional logic for entering attack state can be added here
    }

    public override void ExitState()
    {
        base.ExitState();
        Debug.Log("Exiting Attack State");
        enemy.EnemyAttackBaseInstance.DoExitLogic();
        // Additional logic for exiting attack state can be added here
    }

    public override void FrameUpdate()
    {

        base.FrameUpdate();
        enemy.EnemyAttackBaseInstance.DoFrameUpdateLogic();
           if (!enemy.IsWithInAttackDistance)
        {
            stateMachine.ChangeState(enemy.ChaseState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.EnemyAttackBaseInstance.DoPhysicsLogic();
        // Logic for physics updates during attack state can be added here
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.EnemyAttackBaseInstance.DoAnimationTriggerEventLogic(triggerType);
        // Handle animation trigger events specific to attack state
    }
}
