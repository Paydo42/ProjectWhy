using UnityEngine;
[CreateAssetMenu(fileName = "EnemyAttackSingleProjectile", menuName = "Enemy Logic/Attack Logic/Single Projectile")]
public class EnemyAttackSingleProjectile : EnemyAttackSOBase
{
    [SerializeField] private Rigidbody2D BulletPrefab;
    [SerializeField] private float _TimeBetweenAttacks = 1f;
    [SerializeField] private float _TimeTillExit = 2f;
    [SerializeField] private float _DistanceToCountExit = 3f;
    [SerializeField] private float _BulletSpeed = 10f;

    private float _Timer;
    private float _ExitTimer;
   public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        // Additional logic for entering attack state can be added here
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();
        enemy.MoveEnemy(UnityEngine.Vector2.zero); // Stop movement during attack
        if (enemy.IsWithInAttackDistance)
        {
            if (_Timer >= _TimeBetweenAttacks)
            {
                _Timer = 0f;
                UnityEngine.Vector2 direction = (playerTransform.position - enemy.transform.position).normalized;
                Rigidbody2D bullet = GameObject.Instantiate(BulletPrefab, enemy.transform.position, UnityEngine.Quaternion.identity);
                bullet.linearVelocity = direction * _BulletSpeed;
            }
        }
        if (playerTransform != null)
        {
            // Check if the player is far enough to start exiting the attack state
            if (UnityEngine.Vector2.Distance(playerTransform.position, enemy.transform.position) > _DistanceToCountExit)
            {
                _ExitTimer += Time.deltaTime;
                if (_ExitTimer >= _TimeTillExit)
                {
                    enemy.stateMachine.ChangeState(enemy.ChaseState);
                }
            }
            else
            {
                _ExitTimer = 0f; // Reset exit timer if within distance
            }
            _Timer += Time.deltaTime;
            // Logic for frame updates during attack state can be added here
        }
    }
    public override void DoExitLogic()
    {
        base.DoExitLogic();
        // Additional logic for exiting attack state can be added here
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
        // Logic for physics updates during attack can be added here
    }

    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
        // Handle animation triggers specific to attack logic
    }

    public override void ResetValues()
    {
        base.ResetValues();
        // Reset any specific values related to attack logic
    }
}
