using UnityEngine;

[CreateAssetMenu(fileName = "EnemyChaseRunAway", menuName = "Enemy Logic/Chase Logic/EnemyChaseRunAway")]
public class EnemyChaseRunAway : EnemyChaseSOBase
{
    [SerializeField] private float _RunAwaySpeed = 2f;
    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        // Additional logic for entering the chase run away state can be added here
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        // Additional logic for exiting the chase run away state can be added here
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();
        Vector2 moveDirection = (enemy.transform.position - playerTransform.position).normalized;
        enemy.MoveEnemy(moveDirection * _RunAwaySpeed);
        // Logic for frame updates during chase run away state can be added here
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
        // Logic for physics updates during chase run away state can be added here
    }
    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
        // Handle animation triggers specific to chase run away logic
    }
}

