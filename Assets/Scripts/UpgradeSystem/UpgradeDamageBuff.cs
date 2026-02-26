using UnityEngine;
[CreateAssetMenu(fileName = "UpgradeDamageBuff", menuName = "Upgrades/Damage Buff Upgrade")]
public class UpgradeDamageBuff : UpgradeSOBase
{
    [Tooltip("Amount of bonus damage to add to projectiles")]
    [SerializeField] private float damageIncrease = 1f;
    
    public override void ApplyUpgrade(GameObject player)
    {
        PlayerShooting playerShooting = player.GetComponent<PlayerShooting>();
        if (playerShooting != null)
        {
            playerShooting.AddBonusDamage(damageIncrease);
            Debug.Log($"Damage upgraded! Projectiles now deal +{playerShooting.GetBonusDamage()} bonus damage.");
        }
        else
        {
            Debug.LogWarning("PlayerShooting component not found on the player GameObject.");
        }
    }
}
