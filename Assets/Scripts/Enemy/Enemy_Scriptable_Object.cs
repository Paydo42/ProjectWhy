using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Enemy/Stats")]
public class Enemy_Scriptable_Object : ScriptableObject
{
    [Header("Combat Settings")]
    public float maxHealth = 3f;
    public int damageAmount = 1;
    public float damageCooldown = 0.5f;
    
    [Header("Shooting Settings")]
    public float fireRate = 0.5f;
    public float projectileSpeed = 10f;
    
    
    
    [Header("Visual Settings")]
    public Color damageFlashColor = Color.red;
    public float damageFlashDuration = 0.1f;
    public Color enemyColor = Color.white;
   
    
   
}