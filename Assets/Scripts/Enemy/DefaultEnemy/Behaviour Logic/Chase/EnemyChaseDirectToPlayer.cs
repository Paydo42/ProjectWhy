using UnityEngine;

[CreateAssetMenu(fileName = "EnemyChaseDirectToPlayer", menuName = "Enemy Logic/Chase Logic/Direct To Player")]
public class EnemyChaseDirectToPlayer : EnemyChaseSOBase
{
   
    [SerializeField] private float _ChaseSpeed = 3f;
    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();
           Vector2 moveDirection = (playerTransform.position - enemy.transform.position).normalized;
        enemy.MoveEnemy(moveDirection * _ChaseSpeed);
        // Logic for frame updates during chase can be added here
        if (enemy.IsWithInAttackDistance)
        {
            enemy.stateMachine.ChangeState(enemy.AttackState);
        }
        else if (!enemy.IsAggroed)
        {
            enemy.stateMachine.ChangeState(enemy.IdleState);
        }

    }
    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

    }
    public override void DoExitLogic()
    {
        base.DoExitLogic();

    }
    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
    }
    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
        // Handle animation triggers specific to direct chase logic
    }
    public override void ResetValues()
    {
        base.ResetValues();
        // Reset any specific values related to direct chase logic
    }
}
