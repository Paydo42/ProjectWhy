using UnityEngine;

public delegate void EnemyDeathDelegate(Enemy deadEnemy);

public interface IDamageable
{
    float MaxHealth { get; set; }
    float CurrentHealth { get; set; }
    void TakeDamage(float amount);
    void Die();
    event EnemyDeathDelegate OnEnemyDeath;
    

}
