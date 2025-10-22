using UnityEngine;

// --- FIX: Add ", ITriggerCheckAble" to fulfill the interface contract ---
public class EnemyAggroCheck : MonoBehaviour, ITriggerCheckAble
{
    public GameObject PlayerTarget { get; set; }
    private Enemy _enemy;
    
    private void Awake()
    {
        PlayerTarget = GameObject.FindGameObjectWithTag("Player");
        _enemy = GetComponentInParent<Enemy>();
    }

    private void OnTriggerEnter2D(Collider2D collison)
    {
        if (collison.gameObject == PlayerTarget)
        {
            _enemy.SetAggroStatus(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collison)
    {
        if (collison.gameObject == PlayerTarget)
        {
            _enemy.SetAggroStatus(false);
        }
    }

    // --- FIX: Implement the required methods from the interface ---
    // This script doesn't handle attack distance, so this method is empty.
    public void SetAttackDistanceStatus(bool isWithinStrikingDistance) { }

    // These properties are required by the interface but are managed by the main Enemy script.
    public bool IsAggroed { get => _enemy.IsAggroed; set => _enemy.IsAggroed = value; }
    public bool IsWithInAttackDistance { get => _enemy.IsWithInAttackDistance; set => _enemy.IsWithInAttackDistance = value; }
    
    // This is also required by the interface.
    public void SetAggroStatus(bool isAggroed)
    {
        _enemy.SetAggroStatus(isAggroed);
    }
}

