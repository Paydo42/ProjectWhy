// Full Path: Assets/Scripts/Enemy/DefaultEnemy/Behaviour Logic/Attack/EnemyAttackCircleAndShoot.cs
using UnityEngine;

// This ScriptableObject now primarily holds configuration data for the Circle and Shoot attack pattern.
// The actual movement logic is in CircleTargetBehaviour, and shooting logic is in EnemyAttackState.
[CreateAssetMenu(fileName = "Attack_CircleAndShoot_Data", menuName = "Enemy Logic/Attack Logic Data/Circle and Shoot Data")]
public class EnemyAttackCircleAndShoot : EnemyAttackSOBase
{
    [Header("Shooting Config")]
    public GameObject bulletPrefab;
    public float timeBetweenAttacks = 1f;
    public float bulletSpeed = 10f;

    [Header("Circling Config (Read by State & Behaviour)")]
    public float circleSpeed = 4f; // Note: AgentMover speed might override this unless CircleTargetBehaviour uses it
    public float preferredShootingRange = 5f; // Read by CircleTargetBehaviour
    public float minimumDistance = 4f;       // Read by CircleTargetBehaviour (renamed from rangeDeadZone)
    public float circleSwitchInterval = 5.0f; // Read by EnemyAttackState

    // Example of simple setup logic remaining in the SO
    private int _internalCircleDirection = 1; // Keep internal state private if possible

    // Called by EnemyAttackState.EnterState
    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        // Example: Randomize direction when attack starts
        _internalCircleDirection = (Random.value > 0.5f) ? 1 : -1;
        // NOTE: EnemyAttackState should read this value AFTER calling DoEnterLogic
        // and pass it to CircleTargetBehaviour.clockwise
        Debug.Log("CircleAndShoot SO: Set internal circle direction to " + _internalCircleDirection);
    }

    // Public getter for the state to retrieve the chosen direction
    public int GetCircleDirection()
    {
        return _internalCircleDirection;
    }

    // --- REMOVED / EMPTY LOGIC ---
    public override void DoFrameUpdateLogic() { /* Intentionally empty */ }
    public override void DoPhysicsUpdateLogic() { /* Intentionally empty */ }
    public override void ResetValues() { /* Reset internal SO state if needed */ }

    // --- BASE CLASS METHODS (Ensure they are virtual in EnemyAttackSOBase) ---
    // public override void Initialize(GameObject gameObject, Enemy enemy) { base.Initialize(gameObject, enemy); }
    // public override void DoExitLogic() { base.DoExitLogic(); }
    // public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType) { base.DoAnimationTriggerEventLogic(triggerType); }
}