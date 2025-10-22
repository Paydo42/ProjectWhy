using UnityEngine;

public class Angel : Enemy
{
    
         // For example, you might want to play a special animation or sound effect
           public override void OnDeathAnimationComplete()
    {

        base.OnDeathAnimationComplete();
         Debug.Log("Angel-specific death animation complete logic.");
    }
}
     /* public override void Die()
    {   
           base.Die();
    
        animator.SetTrigger("IsDeathAngel");
        // Additional logic specific to Angel's death can be added here
    }
        public override void OnDeathAnimationComplete()
    {
        // Melek ölümüne özel ek işlemler
        Debug.Log("Angel death animation complete");
        
        // Temel yok etme işlemi
        base.OnDeathAnimationComplete();
    }
    */ 

