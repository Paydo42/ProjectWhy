using UnityEngine;

[CreateAssetMenu(fileName = "EnemyIdleStandStill", menuName = "Enemy Logic/Idle Logic/EnemyIdleStandStill")]
public class EnemyIdleStandStill : EnemyIdleSOBase
{
    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        // Logic for entering the stand still state can be added here
        Debug.Log("Enemy has entered Stand Still state.");
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        // Logic for exiting the stand still state can be added here
        Debug.Log("Enemy has exited Stand Still state.");
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();
        enemy.MoveEnemy(Vector2.zero);
        // Logic for frame updates while in stand still state can be added here
    }
    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
        // Logic for physics updates while in stand still state can be added here
    }
    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);


    }
}

