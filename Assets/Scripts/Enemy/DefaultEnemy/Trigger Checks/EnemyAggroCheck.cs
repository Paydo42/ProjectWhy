using UnityEngine;

public class EnemyAggroCheck : MonoBehaviour
{
    public GameObject PlayerTarget { get; set; }
    private Enemy _enemy;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
}
