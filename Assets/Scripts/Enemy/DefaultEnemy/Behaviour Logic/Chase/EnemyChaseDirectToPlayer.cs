using UnityEngine;

[CreateAssetMenu(fileName = "EnemyChaseDirectToPlayer", menuName = "Enemy Logic/Chase Logic/Direct To Player")]
public class EnemyChaseDirectToPlayer : EnemyChaseSOBase
{
    // REMOVED: _ChaseSpeed - AgentMover now controls speed

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        // Start periodic pathfinding towards the player
        if (playerTransform != null) {
            enemy.StartPathfinding(playerTransform.position);
             // Debug.Log($"[{enemy.name}] Starting DirectToPlayer chase (using pathfinding).");
        } else {
             Debug.LogError($"[{enemy.name}] ChaseDirectToPlayer cannot find player on Enter!", enemy);
             enemy.StopPathfinding(); // Stop if player isn't found
        }
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic(); // Contains transition checks (IsWithInAttackDistance, !IsAggroed)

        // Ensure playerTransform is still valid
        if (playerTransform == null)
        {
            // Attempt to re-acquire player transform if lost (using enemy's reference)
            playerTransform = enemy.playerTransform;

            if(playerTransform == null) {
                Debug.LogWarning($"[{enemy.name}] Lost player target during DirectToPlayer chase.");
                enemy.StopPathfinding(); // Stop if player is definitely gone
                // Transition logic in base class (DoFrameUpdateLogic) might handle changing state if !IsAggroed
                return;
            }
        }


        // Update the target for the pathfinding system continuously
        // The Enemy script handles the recalculation interval.
        enemy.currentPathfindingTarget = playerTransform.position;

        // REMOVED: Direct movement call
        // Vector2 moveDirection = (playerTransform.position - enemy.transform.position).normalized;
        // enemy.MoveEnemy(moveDirection * _ChaseSpeed);
    }

     public override void DoPhysicsLogic() // Use the name from the base class
     {
         base.DoPhysicsLogic();
         // Movement handled by AgentMover via Enemy script's pathfinding updates
     }


    public override void DoExitLogic()
    {
        base.DoExitLogic();
        // Stop pathfinding when leaving this chase behavior
        // Debug.Log($"[{enemy.name}] Exiting DirectToPlayer chase.");
        enemy.StopPathfinding();
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
