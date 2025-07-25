using UnityEngine;

public class Angel : Enemy
{
    public override void Die()
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
       
         // For example, you might want to play a special animation or sound effect
    
}
