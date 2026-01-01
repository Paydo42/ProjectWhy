using Unity.VisualScripting;
using UnityEngine;

public class Oracle : Enemy
{

    

[Header("Oracle Settings")]      

    public GameObject projectilePrefab;
    public Transform firePoint;
    public float attackCooldown = 1f;
    public float projectileSpeed = 5f;

    public LayerMask obstacleLayer;




}
