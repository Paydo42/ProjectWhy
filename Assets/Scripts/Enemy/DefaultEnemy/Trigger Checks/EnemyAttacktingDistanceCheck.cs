using UnityEngine;

public class EnemyAttacktingDistanceCheck : MonoBehaviour
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
            _enemy.SetAttackDistanceStatus(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collison)
    {
       if (collison.gameObject == PlayerTarget)
        {
            _enemy.SetAttackDistanceStatus(false);
        }
    }
}
