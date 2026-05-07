using UnityEngine;

public class Sorcerer : Enemy
{
    // Animation event hook — bind on the begin-teleport clip's final frame.
    public void OnTpBeginAnimationEnd()
    {
        if (EnemyAttackBaseInstance is EnemyAttackTp tp)
            tp.OnTpBeginAnimationEnd();
    }

    // Animation event hook — bind on the appear clip's final frame.
    public void OnTpAppearAnimationEnd()
    {
        if (EnemyAttackBaseInstance is EnemyAttackTp tp)
            tp.OnTpAppearAnimationEnd();
    }
}
