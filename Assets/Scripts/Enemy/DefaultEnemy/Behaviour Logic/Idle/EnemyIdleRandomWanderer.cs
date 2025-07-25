using UnityEngine;

[CreateAssetMenu(fileName = "EnemyIdleRandomWanderer", menuName = "Enemy Logic/Idle Logic/Random Wanderer")]
public class EnemyIdleRandomWanderer : EnemyIdleSOBase
{
    [SerializeField] private float RandomMovementRange = 3f;
    [SerializeField] private float RandomMovementSpeed = 2f;

    private Vector3 _targetPosition;
    private Vector3 _direction;
 
    public override void Initialize(GameObject gameObject, Enemy enemy)
    {
        base.Initialize(gameObject, enemy);

    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
     _targetPosition = GetRandomPointInCircle();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();
        _direction = (_targetPosition - enemy.transform.position).normalized;
        enemy.MoveEnemy(RandomMovementSpeed * _direction);

        if ((enemy.transform.position - _targetPosition).sqrMagnitude < 0.1f)
        {
            _targetPosition = GetRandomPointInCircle();
        }
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
    }
     private Vector3 GetRandomPointInCircle()
    {
        return enemy.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * RandomMovementRange;
    }

}
