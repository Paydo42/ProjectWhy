// Full Path: Assets/Scripts/Enemy/DefaultEnemy/Behaviour Logic/Attack/EnemyAttackSOBase.cs
using UnityEngine;

// Base class for ScriptableObjects that hold configuration data and potentially
// simple setup/cleanup logic for different enemy attack patterns.
// Movement and complex actions are handled by the State Machine and Steering Behaviours.
public class EnemyAttackSOBase : ScriptableObject
{
    // References set during initialization by the Enemy script
    protected Enemy enemy;
    protected Transform transform;
    protected GameObject gameObject;
    protected Transform playerTransform; // Still useful for non-movement logic or data retrieval

    // Called once by Enemy.Awake() after this SO is instantiated.
    // Used to pass references from the enemy instance to the SO instance.
    public virtual void Initialize(GameObject ownerGameObject, Enemy ownerEnemy)
    {
        this.gameObject = ownerGameObject;
        this.transform = ownerGameObject.transform;
        this.enemy = ownerEnemy;

        // Find the player transform safely
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        } else {
            Debug.LogError($"EnemyAttackSOBase on {ownerEnemy.name} could not find GameObject with tag 'Player' during Initialize.", ownerEnemy);
        }
    }

    // Called once by the corresponding EnemyState (e.g., EnemyAttackState) when entering the state.
    // Useful for simple setup actions defined in the child SO (e.g., picking a random direction).
    public virtual void DoEnterLogic() { }

    // Called once by the corresponding EnemyState when exiting the state.
    // Useful for cleanup actions defined in the child SO.
    // Default implementation calls ResetValues.
    public virtual void DoExitLogic() { ResetValues(); }

    // Called every frame by the corresponding EnemyState's FrameUpdate.
    // **AVOID** putting movement logic here. Use for non-physics actions
    // specific to the attack pattern that *must* run every frame (rare).
    public virtual void DoFrameUpdateLogic() { }

    // Called every fixed frame by the corresponding EnemyState's PhysicsUpdate.
    // **AVOID** putting movement logic here. Use for physics-based actions
    // specific to the attack pattern (e.g., applying a special force, also rare).
    // Renamed from DoPhysicsLogic to avoid confusion with non-physics FrameUpdate.
    public virtual void DoPhysicsUpdateLogic() { }

    // Called by the Enemy script when specific animation events occur (if needed).
    // Kept for flexibility, but might not be used often with this architecture.
    public virtual void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType) { }

    // Called by DoExitLogic by default, or manually if needed.
    // Resets any internal state within the child SO.
    public virtual void ResetValues() { }
}