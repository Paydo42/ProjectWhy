using UnityEngine;

public class Charger : Enemy
{
    // Animation event hook — bind on the windup clip's final frame.
    public void OnWindupAnimationEnd()
    {
        if (EnemyAttackBaseInstance is ChargerAttackBehavior charger)
            charger.OnWindupAnimationEnd();
    }

    // Animation event hook — bind on the wall-stun clip's final frame.
    public void OnWallStunAnimationEnd()
    {
        if (EnemyAttackBaseInstance is ChargerAttackBehavior charger)
            charger.OnWallStunAnimationEnd();
    }
}
